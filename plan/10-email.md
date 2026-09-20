# 10 — Gửi email qua hàng đợi

**2 giờ** · Đáp ứng requirement §10.

## Mục tiêu

Hai loại mail:
1. Xử lý tồn kho xong → báo Admin
2. Thanh toán xong → báo Customer

**Ràng buộc quan trọng:** *"Email delivery must not cause the primary business transaction to remain open while waiting for an external mail service."*

Nghĩa là: **không được** gọi SMTP bên trong transaction checkout. Mail server chậm 30 giây thì transaction giữ lock DB 30 giây.

## Cách giải

Dùng lại đúng mô hình của task 09: một `Channel` thứ hai + một `BackgroundService` thứ hai.

```
Checkout transaction          Hàng đợi mail          Worker mail
───────────────────           ─────────────          ───────────
trừ tiền, tạo đơn
COMMIT ✓
  ↓
đẩy yêu cầu mail ──────────► Channel ──────────────► gửi SMTP
  ↓                                                      ↓
trả 200 về client ngay                          cập nhật EmailSentAt
```

Điểm then chốt: **đẩy vào hàng đợi SAU khi commit**, không phải bên trong transaction.

Entity của bạn đã có sẵn chỗ để ghi nhận: `OrderEntity.EmailSentAt` và `InventoryJobEntity.EmailSentAt`.

## Các bước

### 1. Dựng SMTP server cục bộ (15 phút)

Đừng dùng Gmail — vướng app password, 2FA, rate limit, và bạn sẽ mất 45 phút cho một việc không phải trọng tâm.

Thêm **Mailpit** vào `docker-compose.yml`: một SMTP server giả có sẵn giao diện web để xem mail. Image `axllent/mailpit`, cổng SMTP `1025`, cổng web `8025`.

Mail gửi đi sẽ hiện ở http://localhost:8025, không thực sự đi đâu cả. Đúng cho môi trường dev.

### 2. Khai báo interface (15 phút)

Ở `Application/Common/Interfaces/`:

**`IEmailSender`** — một method gửi mail, nhận: địa chỉ nhận, tiêu đề, nội dung, cancellation token.

**`IEmailQueue`** — giống `IInventoryJobQueue`: đẩy vào, và đọc ra.

Kiểu dữ liệu đẩy vào hàng đợi: một record mô tả mail cần gửi. Cân nhắc hai hướng:

| Hướng | Ưu | Nhược |
|---|---|---|
| Đẩy nội dung mail đã soạn sẵn | Worker đơn giản | Soạn nội dung trong transaction |
| Đẩy loại mail + id liên quan (`OrderId`/`JobId`) | Worker tự load dữ liệu mới nhất | Worker phức tạp hơn |

Hướng 2 nhất quán với task 09 và tránh đẩy chuỗi dài vào hàng đợi. Chọn hướng 2.

### 3. Cài đặt gửi mail (25 phút)

Tạo `SmtpEmailSender` ở `Infrastructure/Email/`. Dùng **MailKit** — thư viện SMTP tiêu chuẩn của .NET, vì `SmtpClient` của BCL đã bị Microsoft đánh dấu không khuyến nghị.

Đọc host, port, địa chỉ người gửi từ cấu hình. Với Mailpit thì không cần xác thực, không cần TLS.

Thêm mục cấu hình vào `appsettings.Development.json`.

### 4. Hàng đợi và worker mail (30 phút)

Làm y hệt task 09:
- Class gói `Channel<T>`, đăng ký Singleton
- `BackgroundService` đọc hàng đợi
- **Mỗi mail một DI scope riêng** qua `IServiceScopeFactory`
- `try/catch` quanh từng mail, không quanh vòng lặp

Xử lý một yêu cầu mail:
1. Load dữ liệu liên quan từ DB (đơn hàng hoặc job) theo id
2. Soạn tiêu đề và nội dung
3. Gọi `IEmailSender`
4. Gửi thành công → ghi `EmailSentAt = DateTime.UtcNow`, lưu

> **`EmailSentAt` là cột quan trọng.** Nó cho biết mail đã thực sự gửi hay chưa. Không có nó thì mail gửi hỏng sẽ im lặng biến mất, không ai biết.
>
> Gửi hỏng thì để `EmailSentAt = null` và log lỗi. **Không** đổi trạng thái job thành `FAILED` — việc nhập tồn kho đã thành công, chỉ mail lỗi. Trộn hai loại thất bại vào nhau là sai.

### 5. Nối vào checkout (20 phút)

Trong `CheckoutCommandHandler`: sau khi `CommitAsync` thành công, đẩy yêu cầu mail vào hàng đợi.

**Vị trí dòng này quyết định đúng/sai của cả task.** Đặt trong `try` trước commit là vi phạm requirement §10 — và tệ hơn, nếu transaction rollback thì khách vẫn nhận mail xác nhận cho đơn không tồn tại.

Cần địa chỉ email của khách. `UserEntity` hiện chỉ có `UserName`. Hai lựa chọn:

| Lựa chọn | Công |
|---|---|
| Thêm cột `Email` vào `UserEntity`, bắt buộc khi đăng ký | Sửa entity + migration + command + validator |
| Coi `UserName` là email, thêm rule `EmailAddress()` vào validator đăng ký | Gần như không |

Lựa chọn 2 gọn hơn và đủ cho bài training. Nhưng làm vậy thì phải sửa dữ liệu test đã tạo (`customer1` không phải email hợp lệ). Chọn xong thì ghi lại quyết định vào đây.

### 6. Nối vào xử lý tồn kho (15 phút)

Trong worker của task 09, sau khi đổi trạng thái `DONE`: đẩy yêu cầu mail báo Admin.

Địa chỉ nhận: email của user đã upload (job có sẵn `UserId`).

Nội dung nên có: tên file gốc, số dòng đã xử lý, thời điểm hoàn thành.

Chỉ gửi khi `DONE`. Job `FAILED` thì tuỳ bạn — requirement chỉ yêu cầu mail khi thành công. Muốn gửi cả mail báo lỗi cũng được, ghi lại quyết định.

## AC

- [ ] Mailpit chạy được, mở http://localhost:8025 thấy giao diện
- [ ] Checkout thành công → mail xuất hiện ở Mailpit trong vài giây
- [ ] `OrderEntity.EmailSentAt` được ghi giá trị sau khi gửi
- [ ] Nội dung mail có mã đơn và tổng tiền
- [ ] Upload tồn kho thành công → mail báo Admin xuất hiện ở Mailpit
- [ ] `InventoryJobEntity.EmailSentAt` được ghi
- [ ] ★ **Checkout trả về trong dưới 1 giây** — không đợi SMTP
- [ ] Checkout **thất bại** (số dư không đủ) → **không** có mail nào được gửi
- [ ] Tắt Mailpit rồi checkout → checkout vẫn thành công `200`, chỉ `EmailSentAt` là null
- [ ] Worker mail không chết sau khi gửi lỗi

## Test API

### 1. Khởi động Mailpit

```bash
docker compose up -d
```
Mở http://localhost:8025 — hộp thư rỗng.

### 2. Mail xác nhận đơn hàng

```http
### 2a. Thêm hàng vào giỏ
POST http://localhost:5118/api/cart/items
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "productId": "<ID_SP>",
  "quantity": 1
}
```

```bash
### 2b. Checkout và ĐO thời gian
time curl -X POST http://localhost:5118/api/orders/checkout \
  -H "Authorization: Bearer <TOKEN_CUSTOMER>"
```
→ phải **dưới 1 giây**. Lâu hơn nghĩa là bạn đang đợi SMTP đồng bộ.

Mở http://localhost:8025 → có mail mới, nội dung chứa mã đơn và tổng tiền.

```bash
### 2c. Kiểm tra EmailSentAt
docker exec -it sql-dev /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Local_Dev_Pass123!' \
  -Q "SELECT Id, EmailSentAt FROM MyAppDb.dbo.Orders ORDER BY CreatedAt DESC"
```
→ dòng mới nhất có `EmailSentAt` khác null

### 3. Mail báo nhập tồn kho

```bash
curl -X POST http://localhost:5118/api/inventory/upload \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -F "file=@/tmp/capnhat.csv"
```

Đợi vài giây, mở Mailpit → có mail thứ 2 gửi cho admin.

```bash
docker exec -it sql-dev /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Local_Dev_Pass123!' \
  -Q "SELECT OriginalFileName, Status, EmailSentAt FROM MyAppDb.dbo.InventoryJobs"
```

### ★ 4. Checkout thất bại KHÔNG được gửi mail

```http
### 4a. Đặt giá vượt số dư
PATCH http://localhost:5118/api/products/<ID_SP>/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "price": 9999999
}
```

```http
### 4b. Thêm vào giỏ
POST http://localhost:5118/api/cart/items
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "productId": "<ID_SP>",
  "quantity": 1
}
```

Đếm số mail trong Mailpit **trước**, rồi:

```http
### 4c. Checkout → phải 409
POST http://localhost:5118/api/orders/checkout
Authorization: Bearer <TOKEN_CUSTOMER>
```

Đếm lại số mail → phải **y nguyên**. Có mail mới nghĩa là bạn đẩy hàng đợi trước khi commit. Chuyển dòng đó xuống sau `CommitAsync`.

### ★ 5. Mail chết không được làm chết checkout

```bash
### 5a. Tắt Mailpit
docker compose stop mailpit
```

```http
### 5b. Checkout bình thường (nhớ hạ giá lại trước)
POST http://localhost:5118/api/orders/checkout
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ vẫn phải `200`, đơn vẫn được tạo, tiền vẫn bị trừ

```bash
### 5c. EmailSentAt của đơn này là null
docker exec -it sql-dev /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Local_Dev_Pass123!' \
  -Q "SELECT Id, EmailSentAt FROM MyAppDb.dbo.Orders ORDER BY CreatedAt DESC"
```

```bash
### 5d. Bật lại, checkout tiếp → worker vẫn sống
docker compose start mailpit
```
Checkout thêm lần nữa → mail mới phải xuất hiện. Không có nghĩa là worker mail đã chết ở bước 5b, cần kiểm tra lại `try/catch`.

---

**Tiếp theo:** [11-tests.md](11-tests.md)
