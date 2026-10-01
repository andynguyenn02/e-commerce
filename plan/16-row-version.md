# 16 — Row version / Optimistic concurrency

**1.5–2 giờ** · Chống tranh chấp dữ liệu khi admin sửa product cùng lúc hoặc worker cập nhật tồn kho xen giữa lệnh khác.

> **Lưu ý:** Checkout **đã an toàn** nhờ conditional update và transaction ở task 06. Task này không sửa `CheckoutCommandHandler` một dòng nào — nó chỉ giải quyết hai lỗ hổng khác ở phía admin và worker.

## Mục tiêu

1. **Chặn lost update ở admin:** Hai admin sửa cùng một sản phẩm → người lưu sau bị từ chối với mã lỗi 409, yêu cầu tải lại dữ liệu mới.
2. **Chặn worker ghi đè nhầm:** Worker đọc sản phẩm để cập nhật tồn kho, nhưng giữa lúc đó checkout trừ kho → worker không thể ghi đè mất số lượng vừa trừ. Nếu xung đột, worker tự động tải bản ghi mới rồi thử lại (tối đa 3 lần).
3. **Frontend nhận biết xung đột:** Form sửa sản phẩm hiện thông báo 409 và nút tải lại dữ liệu mới.

## Các bước

### 1. Thêm cột RowVersion vào Product (25 phút)

Thêm property `byte[]` tên `RowVersion` (hoặc `byte[] RowVersionBytes` nếu muốn rõ) vào `ProductEntity`.

Cấu hình ở `ApplicationDbContext.OnModelCreating` bằng Fluent API — **không dùng attribute `[Timestamp]`** trong Domain vì Domain nên sạch khỏi EF. Gọi method `IsRowVersion()` trên property của Product.

> **Cơ chế quan trọng:** Cột `rowversion` trong SQL Server **do DB engine tự động tăng** mỗi lần UPDATE. App không bao giờ gán giá trị này — EF sẽ đọc từ DB sau khi SaveChanges và cập nhật lại entity.
>
> Hệ quả: khi `ExecuteUpdateAsync` của checkout chạy (task 06), nó cũng làm RowVersion đổi → những client còn giữ bản cũ sẽ bị chặn, đúng như thiết kế.

**Phạm vi:** Chỉ ProductEntity. **KHÔNG nhồi vào CommonEntity** vì 11 entity khác không tranh chấp. Wallet dùng conditional update rồi, thêm sau nếu xuất hiện tranh chấp thật.

### 2. Migration (15 phút)

Chạy `dotnet ef migrations add AddProductRowVersion` tại thư mục `ecommerce.Infrastructure`.

Kiểm tra file migration: cột `[RowVersion]` phải có `IsRowVersion()` tương ứng, hoặc cú pháp `rowversion` của SQL Server.

Kiểm tra `ApplicationDbContextModelSnapshot.cs` được cập nhật.

> Khi migration chạy trên DB: SQL Server tự điền giá trị `rowversion` cho các dòng cũ. App không cần lo.

### 3. Sửa ProductDto để trả RowVersion (15 phút)

Thêm property `string RowVersion` (hoặc `byte[] RowVersion`) vào `ProductDto`.

Encode RowVersion thành chuỗi Base64 hoặc hex string để JSON-friendly. EF sẽ gán giá trị byte array từ DB, handler sẽ dùng extension method `ToBase64String` hoặc tương tự để chuyển thành string khi trả về API.

Cập nhật `GetProductByIdQueryHandler` và `GetAllProductsQueryHandler` để mapper tự động populate RowVersion vào DTO.

### 4. Sửa UpdateProductCommand và UpdatePriceCommand (20 phút)

Thêm property `string RowVersion` (chuỗi Base64) vào cả hai command.

Validator: `RowVersion` không được null/empty, nếu không báo lỗi "Phiên bản dữ liệu bị thiếu, hãy tải lại sản phẩm."

> Mục đích: bắt buộc client phải gửi RowVersion của phiên bản đang giữ. Nếu client cố tình bỏ qua → validator từ chối ngay, không cho phép blind update.

### 5. Sửa handler UpdateProductCommandHandler và UpdatePriceCommandHandler (25 phút)

Sau khi đọc entity từ DB, trước khi SaveChanges:

1. Decode RowVersion từ chuỗi Base64 về byte array
2. Gán vào `ChangeTracker.Entries()` của entity, property `OriginalValues["RowVersion"]` (hoặc cách tương đương: gọi method `SetOriginalValue`)
3. Gọi `SaveChangesAsync` như bình thường

EF sẽ tự động thêm `WHERE RowVersion = @oldRowVersion` vào mệnh đề UPDATE. Nếu match → cập nhật. Nếu không match (ai khác đã sửa) → `DbUpdateConcurrencyException`.

> **Thay vì** truyền RowVersion mỗi lần, bạn có thể attach entity với `DbContext.Update(entity)` rồi gán OriginalValues. Chi tiết cách làm là lựa chọn design. Cách nào cũng được, miễn là EF có RowVersion cũ để đặt vào WHERE.

### 6. Xử lý DbUpdateConcurrencyException ở handler (15 phút)

Bắt exception `DbUpdateConcurrencyException` (namespace `Microsoft.EntityFrameworkCore`).

**Lựa chọn thiết kế:** Tạo exception riêng của Application layer tên `ConcurrencyConflictException`, handler bắt EF exception rồi ném lại `ConcurrencyConflictException`.

```
try {
  await context.SaveChangesAsync();
} catch (DbUpdateConcurrencyException ex) {
  throw new ConcurrencyConflictException(...);
}
```

Hoặc bắt trực tiếp ở handler và ném `DbUpdateConcurrencyException` lên. Nếu chọn cách thứ nhất, Api không phải biết type của EF — sạch hơn, khuyến khích.

### 7. Map exception sang 409 Conflict ở GlobalExceptionHandler (10 phút)

Thêm case mới trong `GlobalExceptionHandler` (ở `ecommerce.Api`).

Nếu bắt `ConcurrencyConflictException` (hoặc `DbUpdateConcurrencyException` nếu không tạo exception riêng) → trả `StatusCode(409)` + thông điệp JSON:

```json
{
  "status": 409,
  "message": "Sản phẩm đã bị người khác thay đổi. Vui lòng tải lại dữ liệu mới và thử lại.",
  "code": "CONCURRENCY_CONFLICT"
}
```

### 8. Sửa InventoryJobWorker: retry on conflict (30 phút)

Worker đọc sản phẩm theo `Code`, gọi `SetStock` + `SetPrice`, rồi `SaveChangesAsync`.

Nếu `DbUpdateConcurrencyException` → bắt exception:

1. Reload entity từ DB: `context.Products.Find(productId)` (hoặc query lại)
2. Áp lại giá trị mới từ row hiện tại trong file
3. Thử lại SaveChanges
4. Nếu vẫn lỗi lần thứ 2 → retry lần 3
5. Nếu lần 3 vẫn lỗi → set job status `Failed`, ghi log rõ ràng: `"Inventory job failed after 3 retries for product [Code]"`

> **Ghi chú thiết kế tốt hơn (nếu có thời gian sau):** Thay vì cập nhật tồn kho tuyệt đối (`SetStock(100)`), lưu dưới dạng *delta* bằng conditional update: `UPDATE Products SET AvailableQuantity = AvailableQuantity + deltaQty WHERE ...`. Cách này không bao giờ xung đột. Nhưng đó là thay đổi nghiệp vụ, để sau.

### 9. Frontend: form sửa sản phẩm (15 phút)

Khi load form: API trả RowVersion, form giữ lại trong state ẩn.

Khi submit:
- Gửi RowVersion lại trong body request.
- Nếu response `409` → hiện thông báo "Sản phẩm đã bị người khác cập nhật" + nút "Tải lại dữ liệu mới".
- Nút "Tải lại" → gọi API lấy sản phẩm mới, cập nhật form với dữ liệu mới nhất (bao gồm RowVersion mới), khôi phục tập trung trên form để user sửa lại.

**Nên:** modal/toast thông báo rõ, không im lặng.

### 10. Test (25 phút)

Ở `ecommerce.Tests`, tạo file test mới hoặc thêm vào class hiện có, tái dùng `CheckoutDatabaseFixture`.

**Test 1 — Concurrent admin updates (15 phút):**
- Load sản phẩm, lấy RowVersion_v1
- Spawn 2 task cùng lúc, cả hai sửa giá sản phẩm đó (với cùng RowVersion_v1)
- Mong đợi: 1 task thành công, 1 task nhận `ConcurrencyConflictException`/`ConcurrencyConflictException`
- Kiểm tra giá là giá của task thành công, không phải cả 2 cập nhật

**Test 2 — Worker không ghi đè checkout (10 phút):**
- Tạo product, tồn kho = 10, giá = 100
- Spawn task 1: worker bắt đầu cập nhật từ file, đọc product ra (RowVersion_v1), giữ trong change tracker
- Task 2: checkout trừ 5 cái (thành công, RowVersion tăng thành v2, AvailableQuantity = 5)
- Task 1: worker lưu lại giá và số lượng 10 từ file → bị reject vì RowVersion không match
- Sau retry: worker reload product (RowVersion_v2, AvailableQuantity = 5), áp lại giá từ file, SaveChanges thành công
- Kiểm tra: tồn kho là 5 (checkout không bị đè), không phải 10

**Test 3 — Checkout vẫn an toàn:**
Chạy lại toàn bộ `CheckoutConcurrencyTests` từ task 11 — phải vẫn pass, không hồi quy.

## AC

- [ ] `ProductEntity` có property `byte[] RowVersion`, cấu hình `IsRowVersion()` ở `OnModelCreating`
- [ ] Migration `AddProductRowVersion` được tạo, cột `rowversion` có mặt trong DB
- [ ] `ProductDto` trả `string RowVersion` (Base64 hoặc hex)
- [ ] `UpdateProductCommand` và `UpdatePriceCommand` có property `string RowVersion`, validator check khác null
- [ ] Handler gán OriginalValues trước SaveChanges, EF tự thêm WHERE RowVersion
- [ ] Bắt `DbUpdateConcurrencyException` (hoặc `ConcurrencyConflictException` riêng), map sang 409
- [ ] InventoryJobWorker retry tối đa 3 lần khi gặp conflict, log rõ lần thứ 3 thất bại
- [ ] Frontend hiểu 409, hiện modal "tải lại dữ liệu", nút tải lại refresh RowVersion
- [ ] Test concurrent admin update → 1 thành công, 1 lỗi 409
- [ ] Test worker vs checkout → tồn kho không bị đè nhầm
- [ ] Chạy lại `CheckoutConcurrencyTests` → vẫn pass
- [ ] `CheckoutCommandHandler` không bị sửa một dòng nào

## Test API

### 1. Hai admin sửa cùng lúc

```http
### 1a. Load sản phẩm, lấy RowVersion
GET http://localhost:5118/api/products/{productId}
Authorization: Bearer <TOKEN_ADMIN>
```
Ghi lại `rowVersion` từ response, ví dụ: `"rowVersion": "AAAAAAH7DQ=="`

```http
### 1b. Admin 1 sửa giá
PATCH http://localhost:5118/api/products/{productId}/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN_1>

{
  "price": 150,
  "rowVersion": "AAAAAAH7DQ=="
}
```
→ `204` (thành công)

```http
### 1c. Reload, RowVersion mới
GET http://localhost:5118/api/products/{productId}
Authorization: Bearer <TOKEN_ADMIN>
```
Ghi lại RowVersion mới (khác so với 1a)

```http
### 1d. Admin 2 sửa giá với RowVersion cũ → phải 409
PATCH http://localhost:5118/api/products/{productId}/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN_2>

{
  "price": 200,
  "rowVersion": "AAAAAAH7DQ=="
}
```
→ `409 Conflict` + thông điệp "Sản phẩm đã bị người khác thay đổi"

### 2. Test worker + checkout không xung đột trong đơn giản

```http
### 2a. Đặt giá sản phẩm = 100, tồn kho = 10
PATCH http://localhost:5118/api/products/{productId}/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "price": 100,
  "rowVersion": "..."
}
```

```http
### 2b. Customer checkout 5 cái
POST http://localhost:5118/api/orders/checkout
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `200`, giỏ rỗng, tồn kho = 5

```bash
### 2c. Kiểm tra tồn kho trong DB
docker exec -it sql-dev /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Local_Dev_Pass123!' \
  -Q "SELECT AvailableQuantity, RowVersion FROM MyAppDb.dbo.Products WHERE Code = 'PRODUCT_CODE'"
```
→ `AvailableQuantity = 5` (checkout thành công)

---

**Tiếp theo:** Task hoàn thành, dự án sạch.
