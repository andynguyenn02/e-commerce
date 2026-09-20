# 09 — Xử lý nền bằng Channel + BackgroundService

**2.5 giờ** · Đáp ứng requirement §4. Phần "lạ" nhất với người mới.

## Mục tiêu

File đã lưu ở `Uploads/` được xử lý **ngoài HTTP request**: đọc nội dung, cập nhật sản phẩm, chuyển file sang `Archive/`, đổi trạng thái job.

Requirement bắt buộc dùng `IHostedService`/`BackgroundService` **cộng với** `Channel<T>`. Không được dùng Hangfire hay Quartz.

## Hiểu mô hình trước khi code

```
HTTP request (task 08)          Tiến trình nền (task 09)
─────────────────────           ────────────────────────
lưu file
tạo job STORED
đẩy jobId ──────────► Channel ──────────► worker đọc ra
đổi ACCEPTED                              ↓
trả 202 về client                    PROCESSING
                                          ↓
                                   đọc file, cập nhật DB
                                          ↓
                                   chuyển file → Archive/
                                          ↓
                                       DONE
```

`Channel<T>` là hàng đợi in-memory, thread-safe, có sẵn trong .NET — không cần package.

**Hệ quả phải chấp nhận:** app restart thì job đang chờ trong hàng đợi **mất**. Đó là giới hạn cố hữu của hàng đợi in-memory. Requirement chấp nhận điều này ở mức training. Muốn bền vững thì cần hàng đợi ngoài (RabbitMQ, Azure Service Bus) — không nằm trong phạm vi bài tập.

## Các bước

### 1. Khai báo interface hàng đợi (15 phút)

Tạo `IInventoryJobQueue` ở `Application/Common/Interfaces/`, với 2 khả năng:
- Đẩy một `Guid` (jobId) vào hàng đợi
- Lấy ra lần lượt các jobId, dạng `IAsyncEnumerable<Guid>` hoặc một method đọc chờ

Vì sao chỉ đẩy `Guid` chứ không đẩy cả entity? Vì entity gắn với `DbContext` đã bị dispose sau khi request kết thúc. Đẩy id rồi để worker tự load lại từ DB là cách an toàn duy nhất.

### 2. Cài đặt hàng đợi ở Infrastructure (20 phút)

Tạo class gói `Channel<Guid>`, đăng ký DI dạng **Singleton** — phải là một instance duy nhất, vì bên ghi (controller) và bên đọc (worker) phải dùng chung.

Hai lựa chọn khi tạo channel:

| Loại | Khi nào |
|---|---|
| **Unbounded** | Đơn giản nhất, dùng cho bài này |
| **Bounded** | Giới hạn số job chờ, chặn hoặc bỏ khi đầy |

Dùng unbounded cho bài này. Số job upload tồn kho một ngày không đáng kể.

Tra: `Channel.CreateUnbounded<T>()`, `ChannelWriter.WriteAsync`, `ChannelReader.ReadAllAsync`.

### 3. Viết worker (40 phút)

Tạo class kế thừa `BackgroundService` ở `Infrastructure/BackgroundJobs/`.

Trong `ExecuteAsync`: lặp đọc jobId từ hàng đợi cho tới khi bị huỷ. Mỗi jobId đọc được thì xử lý.

> ### Bẫy lớn nhất của BackgroundService: DI scope
>
> `BackgroundService` là **Singleton**. `IAppDbContext` là **Scoped**. Inject thẳng DbContext vào constructor của worker sẽ nổ lúc khởi động, hoặc tệ hơn — chạy được nhưng dùng chung một DbContext cho mọi job, dẫn tới rò rỉ change tracker và lỗi ngẫu nhiên.
>
> **Cách đúng:** inject `IServiceScopeFactory`. Với **mỗi job**, tạo một scope mới, lấy `IAppDbContext` từ scope đó, xử lý xong thì dispose scope.
>
> Đây là lỗi số 1 khi viết background service trong ASP.NET Core. Tra: `IServiceScopeFactory.CreateScope()`.

Một lỗi nữa: nếu xử lý một job ném exception mà không bắt, cả worker chết và **không có job nào được xử lý nữa** cho tới khi restart app. Phải bọc `try/catch` quanh phần xử lý **từng job**, không phải quanh vòng lặp.

### 4. Logic xử lý một job (45 phút)

Các bước cho mỗi jobId:

1. Load job từ DB. Không thấy → log cảnh báo, bỏ qua
2. Đổi trạng thái `PROCESSING`, lưu
3. Mở file qua `IFileStorage.OpenRead`
4. Đọc nội dung theo định dạng (xem bước 5)
5. Với mỗi dòng: tìm sản phẩm theo `Code`
   - Có → cập nhật giá và tồn kho
   - Không → tuỳ bạn quyết định: tạo mới, hay bỏ qua và ghi vào danh sách lỗi
6. Lưu DB
7. Chuyển file sang `Archive/`
8. Đổi trạng thái `DONE`, lưu
9. Gửi mail thông báo (task 10 — tạm bỏ)

Toàn bộ 2–8 bọc trong `try/catch`. Khi `catch`:
- Đổi trạng thái `FAILED`
- Ghi nội dung lỗi vào `ErrorMessage`
- Chuyển file sang `Failed/` — **không phải** `Archive/`
- Lưu DB

Requirement §5 nói rõ file hỏng không được coi như file đã archive thành công.

**Quyết định cần chốt:** file có 100 dòng, dòng thứ 50 sai định dạng thì sao? Hai hướng:

| Hướng | Ưu | Nhược |
|---|---|---|
| Tất-cả-hoặc-không (dùng transaction) | Dữ liệu nhất quán | 1 dòng sai làm hỏng cả file |
| Xử lý được dòng nào hay dòng đó, gom lỗi vào `ErrorMessage` | Không mất công upload lại | Cập nhật một phần |

Chọn hướng nào cũng được, nhưng **phải ghi lại lựa chọn** vào file này để còn giải thích khi review.

### 5. Đọc CSV và Excel (30 phút)

Requirement §4 yêu cầu hỗ trợ cả hai.

| Định dạng | Thư viện gợi ý | Ghi chú |
|---|---|---|
| CSV | `CsvHelper` | Map header sang class, tự xử lý dấu phẩy trong ngoặc kép |
| Excel `.xlsx` | `ClosedXML` | API đơn giản, không cần cài Excel |

Cả hai đều cài vào project **Infrastructure**, không phải Application. Application chỉ biết "có thứ gì đó đọc được file" qua interface.

Cân nhắc: khai báo thêm một interface kiểu `IInventoryFileReader` ở Application, nhận `Stream` + tên file, trả về danh sách dòng đã parse. Infrastructure chọn parser theo phần mở rộng. Như vậy Application hoàn toàn không biết CsvHelper tồn tại.

Tối thiểu file cần có: `Code`, `Type`, `Price`. Requirement cho phép bạn thêm trường — nên thêm `Quantity` vì bài này cần cập nhật tồn kho.

### 6. Đăng ký worker (10 phút)

Ở `Infrastructure/DependencyInjection.cs`:
- Đăng ký hàng đợi dạng Singleton
- Đăng ký worker bằng `AddHostedService<...>()`

Rồi quay lại task 08, bổ sung bước đẩy jobId vào hàng đợi trong `UploadInventoryFileCommandHandler`.

### 7. Chiến lược retry (10 phút, tuỳ chọn)

Requirement nói *"retry strategy must be designed by the developers"* — tức là bạn được quyền quyết định, kể cả quyết định **không** retry.

Ở mức training, hướng đơn giản nhất: **không tự động retry**, job `FAILED` có `ErrorMessage` để admin đọc và upload lại. Ghi quyết định đó vào đây kèm lý do.

Muốn làm thêm thì thêm cột `RetryCount`, đẩy lại vào hàng đợi tối đa 3 lần.

## AC

- [ ] Upload file → sau vài giây, `GET /api/inventory/jobs` thấy `status: "DONE"`
- [ ] File biến mất khỏi `Uploads/`, xuất hiện trong `Archive/`
- [ ] Giá và tồn kho của sản phẩm trong file đã được cập nhật đúng
- [ ] Upload file sai định dạng nội dung → `status: "FAILED"`, `errorMessage` có nội dung
- [ ] File hỏng nằm trong `Failed/`, **không** phải `Archive/`
- [ ] `POST /api/inventory/upload` vẫn trả về trong **dưới 1 giây** dù file to
- [ ] Upload 3 file liên tiếp → cả 3 đều `DONE`, worker không chết giữa chừng
- [ ] Log của app không có exception chưa bắt

## Test API

### 1. Kịch bản thành công

```bash
### 1a. Xem giá và tồn kho hiện tại
curl http://localhost:5118/api/products?search=MS-001
```
→ ghi lại `price` và `availableQuantity`

```bash
### 1b. Tạo file với giá và số lượng KHÁC hẳn
cat > /tmp/capnhat.csv <<'EOF'
Code,Type,Price,Quantity
MS-001,Phụ kiện máy tính,777000,333
EOF
```

```bash
### 1c. Upload
curl -i -X POST http://localhost:5118/api/inventory/upload \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -F "file=@/tmp/capnhat.csv"
```
→ `202` — và phải trả về **ngay**, không đợi xử lý

```http
### 1d. Đợi 2-3 giây rồi xem job
GET http://localhost:5118/api/inventory/jobs
Authorization: Bearer <TOKEN_ADMIN>
```
→ `status: "DONE"`

```bash
### 1e. Sản phẩm đã đổi
curl http://localhost:5118/api/products?search=MS-001
```
→ `price: 777000`, `availableQuantity: 333`

```bash
### 1f. File đã chuyển chỗ
ls ecommerce.Api/Storage/Uploads/    # không còn file đó
ls ecommerce.Api/Storage/Archive/    # có file đó
```

### 2. Kịch bản thất bại

```bash
### 2a. File sai cấu trúc (thiếu cột, giá không phải số)
cat > /tmp/hong.csv <<'EOF'
Code,Type,Price
MS-001,Phụ kiện,khong-phai-so
EOF

curl -i -X POST http://localhost:5118/api/inventory/upload \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -F "file=@/tmp/hong.csv"
```

```http
### 2b. Kiểm tra job
GET http://localhost:5118/api/inventory/jobs
Authorization: Bearer <TOKEN_ADMIN>
```
→ `status: "FAILED"`, `errorMessage` có nội dung mô tả lỗi

```bash
### 2c. File nằm ở Failed/, KHÔNG ở Archive/
ls ecommerce.Api/Storage/Failed/
ls ecommerce.Api/Storage/Archive/
```

### 3. Test worker không chết

```bash
### Upload liên tiếp 1 file hỏng rồi 2 file tốt
curl -X POST http://localhost:5118/api/inventory/upload -H "Authorization: Bearer <TOKEN_ADMIN>" -F "file=@/tmp/hong.csv"
curl -X POST http://localhost:5118/api/inventory/upload -H "Authorization: Bearer <TOKEN_ADMIN>" -F "file=@/tmp/capnhat.csv"
curl -X POST http://localhost:5118/api/inventory/upload -H "Authorization: Bearer <TOKEN_ADMIN>" -F "file=@/tmp/capnhat.csv"
```

Đợi vài giây rồi xem `GET /api/inventory/jobs`: phải có **1 FAILED và 2 DONE**.

Nếu 2 job sau vẫn đứng ở `ACCEPTED` → worker đã chết vì exception từ job đầu. Kiểm tra lại vị trí `try/catch`: phải bọc **từng job**, không bọc cả vòng lặp.

### 4. Test DI scope

Upload 5 file liên tiếp. Nếu gặp lỗi kiểu *"A second operation was started on this context instance"* hoặc dữ liệu lẫn lộn giữa các job → bạn đang dùng chung một `DbContext`. Sửa lại theo `IServiceScopeFactory`.

---

**Xong ngày 2.** Commit lại.

**Tiếp theo:** [10-email.md](10-email.md)
