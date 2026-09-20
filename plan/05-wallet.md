# 05 — Ví điện tử

**1 giờ** · Task dễ. Khởi động ngày 2.

## Mục tiêu

Customer xem được số dư và lịch sử biến động tiền. Đáp ứng requirement §7.

## Ghi chú thiết kế

Model đã đúng sẵn từ đầu:

| Requirement §7 | Giải bằng |
|---|---|
| "Current wallet balance" | `WalletEntity.Balance` |
| "Money deducted during purchases" | `WalletTransactionEntity.Amount` |
| "Which purchase caused a deduction" | `WalletTransactionEntity.OrderId` |

Task này **chỉ đọc**. Ghi vào ví là việc của [06-checkout.md](06-checkout.md).

## Các bước

### 1. DTO (10 phút)

Hai record:
- **`WalletDto`** — id ví, số dư
- **`WalletTransactionDto`** — id giao dịch, id đơn hàng liên quan, số tiền, thời điểm

### 2. Query số dư (15 phút)

Query không tham số, lấy user từ `ICurrentUser`.

Handler: lọc ví theo `UserId`, projection, lấy một cái. Không tìm thấy thì ném `KeyNotFoundException`.

> **Ném 404 ở đây là đúng**, khác với giỏ hàng. Customer nào cũng phải có ví (tạo lúc đăng ký ở task 01). Không có ví nghĩa là dữ liệu hỏng — cần biết ngay chứ không nên im lặng trả về 0.
>
> Giỏ rỗng là trạng thái bình thường. Ví không tồn tại là trạng thái bất thường. Phân biệt được hai thứ này là điểm phân biệt code cẩu thả với code cẩn thận.

### 3. Query lịch sử giao dịch (20 phút)

Handler: lọc giao dịch mà ví của nó thuộc về user hiện tại, sắp giảm dần theo thời gian tạo, projection.

> **Mẹo EF:** lọc xuyên navigation (`t.Wallet!.UserId == ...`) thì EF tự sinh `JOIN` sang bảng Wallets. Không cần load ví trước rồi lọc theo `WalletId` — một query là đủ.

Sắp giảm dần theo thời gian vì người dùng đọc sao kê từ mới nhất.

### 4. Controller (10 phút)

| Method | Route | Việc |
|---|---|---|
| GET | `/api/wallet` | Số dư |
| GET | `/api/wallet/transactions` | Lịch sử |

`[Authorize(Roles = "CUSTOMER")]` ở cấp class — Admin không có ví.

Không có `{userId}` trong URL. Ví lấy theo token.

### 5. Chốt quy ước dấu của Amount (5 phút)

Quyết định **ngay bây giờ** và ghi lại vào đây, vì task 06 sẽ ghi vào bảng này:

> `Amount` là **số dương** = số tiền bị trừ khỏi ví cho đơn hàng đó.

Dự án này chỉ có một chiều tiền (mua hàng), nên không cần dấu âm/dương để phân biệt.

**Đừng thêm cột `TransactionType` bây giờ.** Chưa có nghiệp vụ nào (nạp tiền, hoàn tiền) dùng tới nó. Thêm khi nào thực sự cần.

## AC

- [ ] `GET /api/wallet` với token CUSTOMER → `200`, `balance: 1000` (tài khoản mới)
- [ ] `GET /api/wallet` không token → `401`
- [ ] `GET /api/wallet` với token ADMIN → `403`
- [ ] `GET /api/wallet/transactions` tài khoản mới → `200` với `[]`
- [ ] Customer A và Customer B có `walletId` khác nhau

## Test API

```http
### 1. Xem số dư
GET http://localhost:5118/api/wallet
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `{ "walletId": "...", "balance": 1000 }`

```http
### 2. Lịch sử giao dịch (chưa mua gì)
GET http://localhost:5118/api/wallet/transactions
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `[]`

```http
### 3. Admin xem ví → phải 403
GET http://localhost:5118/api/wallet
Authorization: Bearer <TOKEN_ADMIN>
```

```http
### 4. Không token → phải 401
GET http://localhost:5118/api/wallet
```

```http
### 5. Cách ly giữa 2 customer — đăng nhập customer2 rồi gọi
GET http://localhost:5118/api/wallet
Authorization: Bearer <TOKEN_CUSTOMER2>
```
→ `walletId` **khác** customer1, `balance` cũng `1000`

```bash
### 6. Đối chiếu DB
docker exec -it sql-dev /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Local_Dev_Pass123!' \
  -Q "SELECT u.UserName, w.Id, w.Balance FROM MyAppDb.dbo.Wallets w JOIN MyAppDb.dbo.Users u ON u.Id = w.UserId"
```

---

**Tiếp theo:** [06-checkout.md](06-checkout.md) — task khó nhất của backend
