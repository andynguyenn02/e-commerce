# 00 — Dọn nợ kỹ thuật

**30 phút** · Dọn sạch trước khi xây tiếp. Làm nhanh, đừng nghĩ nhiều.

## Mục tiêu

Xoá code chết, sửa 4 lỗi nhỏ đang tồn, tự động hoá timestamp để 10 task sau không phải gán tay.

## Các bước

### 1. Gỡ mật khẩu khỏi git (5 phút)

Mở `ecommerce.Api/appsettings.Development.json`, xoá toàn bộ khối `ConnectionStrings`.

Connection string đã nằm trong user secrets rồi — kiểm tra bằng `dotnet user-secrets list -p ecommerce.Api`. Bản trong appsettings là thừa và đang bị commit lên git.

### 2. Sửa 3 lỗi nhỏ (5 phút)

| File | Vấn đề | Hướng sửa |
|---|---|---|
| `GetProductByCategoryQueryHandler` | `ToListAsync()` thiếu cancellation token | Truyền token vào |
| `UpdateProductCommandHandler` | Gán cả `Category` lẫn `CategoryId` | Bỏ dòng gán navigation, chỉ giữ khoá ngoại |
| `GetAllProductsQueryHandler` | Load toàn bộ entity rồi map trong bộ nhớ | Chuyển sang projection để EF sinh SQL chỉ lấy cột cần |

Sau khi chuyển sang projection, `AsNoTracking()` thành thừa — projection vốn không bao giờ bị tracking. Bỏ luôn.

### 3. Bật lại soft delete (5 phút)

`ProductEntity.IsDeleted` đang là code chết. Sản phẩm đã nằm trong đơn hàng thì không được xoá cứng — lịch sử đơn sẽ vỡ.

Ba việc cần làm:

1. **Handler xoá** đổi từ gỡ entity khỏi `DbSet` sang bật cờ `IsDeleted`.
2. **Global query filter** trên `ProductEntity` để mọi truy vấn tự loại sản phẩm đã xoá. Khai báo trong `OnModelCreating`.
3. **Unique index trên `Code` phải thành filtered index** — chỉ áp cho hàng chưa xoá.

Bỏ qua việc 3 thì mã sản phẩm đã xoá bị khoá vĩnh viễn, không dùng lại được. Tra cứu: `HasQueryFilter`, `HasIndex(...).HasFilter(...)`.

### 4. Tự động hoá timestamp (10 phút)

Đang gán `CreatedAt`/`UpdatedAt` bằng tay trong từng handler. Làm một lần ở tầng DbContext.

Hướng đi: override `SaveChangesAsync` trong `ApplicationDbContext`, duyệt `ChangeTracker.Entries<CommonEntity>()`, gán theo `EntityState`:

- `Added` → gán cả `CreatedAt` và `UpdatedAt`
- `Modified` → chỉ gán `UpdatedAt`

Rồi gọi `base.SaveChangesAsync`.

Làm xong phải:
- Bỏ `required` ở 2 field thời gian trong `CommonEntity` (giờ DbContext lo, không bắt caller gán nữa)
- Xoá mọi dòng gán `CreatedAt`/`UpdatedAt` trong các handler

> **Bẫy cần biết:** `ExecuteUpdateAsync`/`ExecuteDeleteAsync` không đi qua `SaveChangesAsync` nên không được đóng dấu thời gian. Nếu sau này dùng chúng, phải set tay.

### 5. Xoá interface repository không dùng (2 phút)

Xoá cả thư mục `ecommerce.Domain/Interfaces/` — 8 file repository + `IUnitOfWork`. Không có class nào cài đặt, không có nơi nào đăng ký DI, không handler nào inject.

Rồi dọn `using` còn sót trong 6 handler ở `Application/Products/`.

### 6. Migration (3 phút)

```bash
dotnet ef migrations add Cleanup -p ecommerce.Infrastructure -s ecommerce.Api
dotnet ef database update -p ecommerce.Infrastructure -s ecommerce.Api
```

## AC

- [ ] `dotnet build` — 0 error, 0 warning
- [ ] `grep -r "Password=" ecommerce.Api/appsettings*.json` không ra kết quả
- [ ] Thư mục `ecommerce.Domain/Interfaces/` không còn tồn tại
- [ ] Không còn dòng gán `CreatedAt` nào trong `Application/`
- [ ] Xoá sản phẩm rồi `GET` danh sách → biến mất, nhưng trong DB row vẫn còn với `IsDeleted = 1`
- [ ] Tạo sản phẩm mới trùng `code` với sản phẩm vừa xoá mềm → thành công

## Test API

```http
### 1. Tạo sản phẩm
POST http://localhost:5118/api/products
Content-Type: application/json

{
  "name": "Bàn phím cơ",
  "price": 1200000,
  "code": "KB-001",
  "availableQuantity": 50,
  "categoryId": "0199c1a0-0000-7000-8000-000000000001"
}
```

→ `201`, copy lại `id`.

```http
### 2. Xoá mềm
DELETE http://localhost:5118/api/products/{id}
```
→ `204`

```http
### 3. Danh sách không còn nó
GET http://localhost:5118/api/products
```
→ `200`, mảng không chứa `KB-001`

```http
### 4. Tái sử dụng mã cũ
POST http://localhost:5118/api/products
Content-Type: application/json

{
  "name": "Bàn phím cơ v2",
  "price": 1500000,
  "code": "KB-001",
  "availableQuantity": 30,
  "categoryId": "0199c1a0-0000-7000-8000-000000000001"
}
```
→ `201`. Nếu `500` kèm lỗi unique index thì bước 3 thiếu filtered index.

```bash
### 5. Đối chiếu DB
docker exec -it sql-dev /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Local_Dev_Pass123!' \
  -Q "SELECT Code, IsDeleted, CreatedAt FROM MyAppDb.dbo.Products"
```

Phải thấy 2 dòng `KB-001` (một `IsDeleted=1`, một `IsDeleted=0`) và `CreatedAt` có giá trị thật.

---

**Tiếp theo:** [01-auth-jwt.md](01-auth-jwt.md)
