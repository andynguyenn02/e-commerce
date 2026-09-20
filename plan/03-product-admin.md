# 03 — Product: Phân quyền, Sửa giá, Phân trang

**2 giờ** · Hoàn thiện phần Product cho đúng requirement §3.

## Mục tiêu

Admin sửa được giá riêng lẻ. Danh sách có phân trang + lọc + tìm kiếm. Rà lại phân quyền.

## Các bước

### 1. Endpoint sửa giá riêng (25 phút)

Requirement §3: *"Admins must be able to update the selling price of a product."* Dùng `PUT` cả object chỉ để đổi giá là thừa và dễ ghi đè nhầm field khác.

Tạo `UpdateProductPriceCommand` nhận `ProductId` + `Price`, không trả về gì.

Handler: tìm sản phẩm, gán giá mới, lưu. Ngắn gọn thế thôi.

Validator: giá phải lớn hơn 0.

**Route:** `PATCH /api/products/{id}/price`, chỉ ADMIN. Dùng `PATCH` vì đúng ngữ nghĩa — sửa **một phần** tài nguyên. Body chỉ chứa `price`, id lấy từ route.

> **Không cần lưu lịch sử giá.** Giá lịch sử được bảo toàn nhờ `OrderItemEntity.PriceAtPurchased` — chụp lại giá tại thời điểm mua. Sửa `Product.Price` không đụng gì tới đơn cũ. Requirement §3 và §9 được thoả mãn chỉ bằng thiết kế này, không cần bảng `PriceHistory`.

### 2. Phân trang (35 phút)

`GET /api/products` đang trả toàn bộ bảng. Sửa thành phân trang + lọc.

**Tạo một generic record `PagedResult<T>`** ở `Application/Common/Models/`, chứa: danh sách item, số trang hiện tại, kích thước trang, tổng số bản ghi. Thêm property tính `TotalPages` từ tổng chia kích thước trang (nhớ làm tròn lên).

**Sửa `GetAllProductsQuery`** thành nhận 4 tham số có giá trị mặc định: `Page = 1`, `PageSize = 20`, `CategoryId` (nullable), `Search` (nullable). Trả về `PagedResult<ProductDto>`.

**Handler dựng query theo từng bước:**

1. Bắt đầu từ `DbSet` dạng `IQueryable`
2. Nếu có `CategoryId` → nối thêm điều kiện lọc
3. Nếu có `Search` → nối thêm điều kiện tìm trong tên hoặc mã
4. Đếm tổng số bản ghi **trên query đã lọc, trước khi phân trang**
5. Sắp xếp → bỏ qua → lấy → projection → thực thi

Ba điểm dễ sai:

- **Nối `Where` dần không chạy query nào.** EF chỉ dựng cây biểu thức. SQL chỉ bắn đi khi gặp `CountAsync` hoặc `ToListAsync`.
- **`OrderBy` là bắt buộc khi có `Skip`/`Take`.** Không sắp xếp thì SQL Server trả thứ tự tuỳ ý, trang 2 có thể lặp item của trang 1.
- **Đếm tổng phải trước khi phân trang**, nếu không bạn đếm được đúng số item của trang hiện tại.

Validator: `Page` > 0, `PageSize` trong khoảng 1–100. Giới hạn trên để client không xin 1 triệu dòng một lần.

**Controller:** dùng `[FromQuery]` để bind thẳng query string vào record.

### 3. Gộp DTO trùng lặp (15 phút)

Xoá `GetProductByIdDto` và `GetProductByCategoryDto` — cả hai giống hệt `ProductDto`.

Chuyển `ProductDto` lên một cấp, thành `Application/Products/Queries/ProductDto.cs`, sửa namespace, rồi cho 3 handler dùng chung.

Giảm 3 type xuống 1, không mất gì.

### 4. Xoá query GetProductByCategory (10 phút)

`GetAllProductsQuery` giờ đã có tham số `CategoryId`. Query riêng thành thừa.

Xoá cả thư mục `Queries/GetProductByCategoryId/` và endpoint tương ứng. Thay bằng `GET /api/products?categoryId=...`.

> Nên làm việc này thường xuyên: mỗi lần thêm tính năng, kiểm tra xem nó có làm code cũ thành thừa không. Xoá được code là thắng.

### 5. Rà lại phân quyền (10 phút)

| Endpoint | Quyền |
|---|---|
| `GET /api/products` | Công khai |
| `GET /api/products/{id}` | Công khai |
| `POST /api/products` | ADMIN |
| `PUT /api/products/{id}` | ADMIN |
| `PATCH /api/products/{id}/price` | ADMIN |
| `DELETE /api/products/{id}` | ADMIN |

### 6. Độ chính xác decimal (5 phút)

Nếu chưa làm ở task 00: khai báo `HasPrecision(18, 2)` cho mọi cột tiền — `Product.Price`, `OrderItem.PriceAtPurchased`, `Wallet.Balance`, `WalletTransaction.Amount`.

Không khai báo thì EF cảnh báo và tự chọn mặc định, dễ mất số lẻ ở các bảng sau.

Rồi tạo migration, cập nhật DB.

## AC

- [ ] `?page=1&pageSize=5` → tối đa 5 item + `totalCount` + `totalPages`
- [ ] `?page=2&pageSize=5` → 5 item khác hẳn trang 1, không trùng
- [ ] `?search=chuot` → chỉ ra sản phẩm khớp tên hoặc mã
- [ ] `?categoryId=...` → chỉ ra sản phẩm thuộc danh mục đó
- [ ] `?pageSize=1000` → `400`
- [ ] `PATCH .../price` với ADMIN → `204`, `GET` lại thấy giá mới
- [ ] `PATCH .../price` với CUSTOMER → `403`
- [ ] `PATCH .../price` với giá âm → `400`
- [ ] Không còn `GetProductByIdDto.cs`, `GetProductByCategoryDto.cs`

## Test API

```http
### 1. Trang 1, 5 item
GET http://localhost:5118/api/products?page=1&pageSize=5
```

Dạng kết quả mong đợi:
```json
{
  "items": [],
  "page": 1,
  "pageSize": 5,
  "totalCount": 12,
  "totalPages": 3
}
```

```http
### 2. Trang 2 — phải khác hẳn trang 1
GET http://localhost:5118/api/products?page=2&pageSize=5
```

```http
### 3. Tìm kiếm
GET http://localhost:5118/api/products?search=chuot
```

```http
### 4. Lọc theo danh mục
GET http://localhost:5118/api/products?categoryId=0199c1a0-0000-7000-8000-000000000001
```

```http
### 5. pageSize quá lớn → phải 400
GET http://localhost:5118/api/products?pageSize=1000
```

```http
### 6. Admin đổi giá
PATCH http://localhost:5118/api/products/{productId}/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "price": 520000
}
```
→ `204`

```http
### 7. Xác nhận giá mới
GET http://localhost:5118/api/products/{productId}
```
→ `price: 520000`

```http
### 8. Customer đổi giá → phải 403
PATCH http://localhost:5118/api/products/{productId}/price
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "price": 1
}
```

```http
### 9. Giá âm → phải 400
PATCH http://localhost:5118/api/products/{productId}/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "price": -5
}
```

---

**Tiếp theo:** [04-cart.md](04-cart.md)
