# 06 — Thanh toán (Checkout)

**3 giờ** · **Task khó nhất của backend.** Đáp ứng requirement §8.

Đọc hết file trước khi gõ dòng đầu tiên.

## Mục tiêu

Một thao tác thanh toán làm **7 việc, hoặc thành công tất cả, hoặc không việc nào**:

1. Tạo đơn hàng
2. Trừ tiền ví
3. Giảm tồn kho
4. Ghi các sản phẩm đã mua
5. Ghi **giá tại thời điểm mua**
6. Ghi biến động ví
7. Dọn sạch giỏ hàng

Nếu bước 5 lỗi mà bước 2 đã trừ tiền → khách mất tiền không có hàng. Đó là lý do cần transaction.

## Chuẩn bị: mở cửa transaction qua interface

`IAppDbContext` hiện chỉ có các `DbSet` và `SaveChangesAsync`. Cần thêm một method mở transaction — nhận cancellation token, trả về `IDbContextTransaction` (namespace `Microsoft.EntityFrameworkCore.Storage`).

Bên `ApplicationDbContext`, cài đặt bằng cách uỷ quyền cho `Database.BeginTransactionAsync`.

> **Vì sao không phơi thẳng property `Database` ra interface?** Vì `DatabaseFacade` mở toang toàn bộ EF cho tầng Application — `ExecuteSqlRaw`, `Migrate`, `EnsureDeleted`. Chỉ mở đúng thứ cần dùng. Đây là nguyên tắc interface tối thiểu.

## Các bước

### 1. Command và DTO (15 phút)

`CheckoutCommand` **không có tham số nào**.

Trả về một record chứa: `OrderId`, `TotalAmount`, `RemainingBalance`.

> Command rỗng là có chủ ý. Mọi thứ cần thiết đều lấy từ phía server: user từ token, sản phẩm từ giỏ trong DB, giá từ bảng Product.
>
> Requirement §6: *"The frontend must not be considered the trusted source of product prices."* Không nhận bất cứ con số nào từ client là cách triệt để nhất để thoả mãn điều đó.

### 2. Khung transaction (20 phút)

Cấu trúc handler:

1. Mở transaction (dùng `await using` để tự dispose kể cả khi ném exception)
2. Khối `try` — chứa toàn bộ logic bước 3–8
3. Cuối `try`: gọi `SaveChangesAsync` **một lần**, rồi `CommitAsync`
4. Khối `catch`: gọi `RollbackAsync`, rồi ném lại exception

Hai điểm dễ sai:

- Trong `catch` phải dùng `throw;` trần, **không** `throw ex;`. `throw ex;` xoá sạch stack trace gốc, mất luôn manh mối debug.
- Exception phải được ném tiếp lên để `UseExceptionHandler` trả status code đúng. Nuốt exception ở đây thì client nhận `200` cho một giao dịch thất bại.

### 3. Lấy giỏ và kiểm tra rỗng (15 phút)

Tìm giỏ theo `UserId`. Không có giỏ, hoặc giỏ không có item nào → ném lỗi xung đột "Giỏ hàng trống".

Lấy các cart item **kèm `Include` sang `Product`** — chỗ này cần entity thật, không dùng projection.

> Đây là ngoại lệ hợp lý cho quy tắc "query thì projection". Lát nữa phải **sửa** `Product.AvailableQuantity`, nên cần entity được change tracker theo dõi. Projection tạo ra object rời, sửa không có tác dụng.

### 4. Kiểm tra sản phẩm và tồn kho (20 phút)

Duyệt từng cart item, với mỗi sản phẩm kiểm tra:
- Còn được bán không (cờ `IsDeleted`)
- Tồn kho có đủ cho số lượng đặt không

Không đủ thì ném lỗi xung đột, thông báo nêu rõ tên sản phẩm và số còn lại.

> **Phải kiểm tra lại dù task 04 đã kiểm tra khi thêm giỏ.** Giữa lúc thêm và lúc thanh toán có thể cách nhau vài ngày. Kho đã đổi, sản phẩm có thể đã ngừng bán.
>
> Nguyên tắc chung: **validate tại thời điểm ghi mới là validate thật.** Kiểm tra lúc thêm vào giỏ chỉ để trải nghiệm người dùng tốt hơn, không phải để đảm bảo đúng đắn.

### 5. Tính tổng tiền và kiểm tra ví (20 phút)

Tổng tiền = tổng của (giá hiện tại của sản phẩm × số lượng) cho mọi item.

Lấy ví của user. So số dư với tổng tiền. Không đủ → ném lỗi xung đột, thông báo nêu số cần và số còn.

Giá dùng để tính là `Product.Price` đọc từ DB — không phải giá client gửi lên (client không gửi gì), không phải giá lưu trong giỏ (giỏ không lưu giá).

### 6. Tạo đơn hàng và các dòng sản phẩm (30 phút)

Tạo `OrderEntity` gắn với user hiện tại.

Rồi duyệt từng cart item, mỗi cái tạo một `OrderItemEntity` gồm: id đơn, id sản phẩm, số lượng, và **`PriceAtPurchased` gán bằng giá hiện tại của sản phẩm**.

Trong cùng vòng lặp, trừ `AvailableQuantity` của sản phẩm đi đúng số lượng mua.

> ### Dòng gán `PriceAtPurchased` là dòng quan trọng nhất của cả dự án.
>
> Nó **sao chép** giá thành một giá trị độc lập. Từ giây phút đó, admin sửa `Product.Price` bao nhiêu lần cũng không đụng tới con số đã ghi.
>
> Requirement §3 (đơn cũ giữ $100 khi giá lên $120) và §9 (lịch sử bất biến) đều được giải bằng đúng một phép gán này.
>
> Nếu thay vào đó bạn chỉ lưu `ProductId` rồi lúc xem lịch sử mới `JOIN` lấy giá — lịch sử sẽ thay đổi mỗi lần admin sửa giá. Sai hoàn toàn, và là lỗi phổ biến nhất ở bài tập này.

### 7. Trừ ví và ghi biến động (15 phút)

Trừ `Balance` của ví đi đúng tổng tiền.

Tạo `WalletTransactionEntity` gồm: id ví, **id đơn hàng vừa tạo**, và số tiền (dương, theo quy ước chốt ở task 05).

`OrderId` chính là thứ nối biến động tiền với đơn hàng — đáp ứng "which purchase caused a deduction" của §7.

### 8. Dọn giỏ (5 phút)

Gỡ hàng loạt các cart item.

**Giữ lại `CartEntity`**, chỉ xoá item. Lần mua sau dùng lại giỏ cũ, không cần tạo mới.

### 9. Controller (10 phút)

`POST /api/orders/checkout`, **không có body**.

Đặt trong `OrdersController` mới, gắn `[Authorize(Roles = "CUSTOMER")]` ở cấp class — requirement §2: *"An Admin cannot perform checkout."*

### 10. Kiểm lại: chỉ một SaveChanges (10 phút)

Toàn bộ thay đổi ở bước 3–8 chỉ nằm trong change tracker. **Một** `SaveChangesAsync` duy nhất ở cuối đẩy tất cả xuống DB trong cùng transaction.

Đừng rải nhiều `SaveChangesAsync` giữa các bước. Một lần là đủ, gọn hơn, và ít chỗ sai hơn.

## AC

- [ ] Checkout giỏ hợp lệ → `200` + `orderId`, `totalAmount`, `remainingBalance`
- [ ] Số dư ví giảm **đúng** bằng `totalAmount`
- [ ] `AvailableQuantity` giảm đúng số lượng đã mua
- [ ] Bảng `OrderItems` có `PriceAtPurchased` bằng giá tại thời điểm mua
- [ ] Bảng `WalletTransactions` có 1 dòng mới, `OrderId` trỏ đúng đơn vừa tạo
- [ ] Giỏ rỗng sau khi thanh toán
- [ ] Checkout giỏ rỗng → `409`
- [ ] Số dư không đủ → `409`, **và ví không bị trừ, tồn kho không đổi, không có đơn nào được tạo**
- [ ] Tồn kho không đủ → `409`, không có gì thay đổi
- [ ] Admin gọi checkout → `403`
- [ ] ★ Admin đổi giá sau khi mua → đơn cũ giữ nguyên giá cũ

## Test API

### Kịch bản chính — chạy tuần tự

```http
### 1. Xác nhận số dư ban đầu
GET http://localhost:5118/api/wallet
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ ghi lại `balance`, giả sử `1000`

```http
### 2. Admin đặt giá sản phẩm = 100
PATCH http://localhost:5118/api/products/<ID_SP>/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "price": 100
}
```

```http
### 3. Customer thêm 2 cái vào giỏ
POST http://localhost:5118/api/cart/items
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "productId": "<ID_SP>",
  "quantity": 2
}
```

```http
### 4. Xác nhận giỏ = 200
GET http://localhost:5118/api/cart
Authorization: Bearer <TOKEN_CUSTOMER>
```

```http
### 5. Thanh toán
POST http://localhost:5118/api/orders/checkout
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `{ "orderId": "...", "totalAmount": 200, "remainingBalance": 800 }` — **lưu `orderId`**

```http
### 6. Ví còn 800
GET http://localhost:5118/api/wallet
Authorization: Bearer <TOKEN_CUSTOMER>
```

```http
### 7. Giỏ đã rỗng
GET http://localhost:5118/api/cart
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `items: []`, `total: 0`

```http
### 8. Có 1 biến động ví
GET http://localhost:5118/api/wallet/transactions
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ 1 dòng, `amount: 200`, `orderId` khớp bước 5

### ★ 9. Test giá lịch sử

```http
### 9a. Admin tăng giá lên 120
PATCH http://localhost:5118/api/products/<ID_SP>/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "price": 120
}
```

```bash
### 9b. Giá trong đơn cũ PHẢI vẫn là 100
docker exec -it sql-dev /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Local_Dev_Pass123!' \
  -Q "SELECT oi.PriceAtPurchased, oi.Quantity, p.Price AS GiaHienTai FROM MyAppDb.dbo.OrderItems oi JOIN MyAppDb.dbo.Products p ON p.Id = oi.ProductId"
```
→ `PriceAtPurchased = 100`, `GiaHienTai = 120`. Hai số **khác nhau** là đạt.

### 10. Test rollback — số dư không đủ

```http
### 10a. Admin đặt giá cực cao
PATCH http://localhost:5118/api/products/<ID_SP>/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "price": 999999
}
```

```http
### 10b. Thêm vào giỏ
POST http://localhost:5118/api/cart/items
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "productId": "<ID_SP>",
  "quantity": 1
}
```

```http
### 10c. Thanh toán → phải 409
POST http://localhost:5118/api/orders/checkout
Authorization: Bearer <TOKEN_CUSTOMER>
```

**Rồi kiểm tra ba thứ — tất cả phải KHÔNG đổi:**

```http
GET http://localhost:5118/api/wallet
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ vẫn `800`

```http
GET http://localhost:5118/api/cart
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ giỏ **vẫn còn** item, không bị xoá

```bash
docker exec -it sql-dev /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Local_Dev_Pass123!' \
  -Q "SELECT COUNT(*) FROM MyAppDb.dbo.Orders"
```
→ vẫn là 1, **không** có đơn mới

Nếu xuất hiện đơn thứ 2 hoặc giỏ bị xoá → transaction không rollback đúng. Kiểm tra lại `await using` và khối `catch`.

```http
### 11. Giỏ rỗng → phải 409 (xoá giỏ trước rồi checkout)
DELETE http://localhost:5118/api/cart
Authorization: Bearer <TOKEN_CUSTOMER>
```
```http
POST http://localhost:5118/api/orders/checkout
Authorization: Bearer <TOKEN_CUSTOMER>
```

```http
### 12. Admin checkout → phải 403
POST http://localhost:5118/api/orders/checkout
Authorization: Bearer <TOKEN_ADMIN>
```

---

**Tiếp theo:** [07-order-history.md](07-order-history.md)
