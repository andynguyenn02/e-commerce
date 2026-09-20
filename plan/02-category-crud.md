# 02 — CRUD Danh mục

**1 giờ** · Task dễ nhất. Mục đích là **lặp lại pattern** cho thành phản xạ, và thoát khỏi category seed cứng.

## Mục tiêu

CRUD đầy đủ cho `CategoryEntity`. Ai cũng xem được, chỉ ADMIN được sửa.

## Các bước

### 1. Dựng cấu trúc thư mục (5 phút)

Copy y hệt bố cục `Application/Products/`:

```
Application/Categories/
├── Commands/
│   ├── CreateCategory/
│   ├── UpdateCategory/
│   └── DeleteCategory/
└── Queries/
    ├── GetAllCategories/
    └── GetCategoryById/
```

Mỗi thư mục Command có 3 file: Command, Handler, Validator.

### 2. Một DTO dùng chung (2 phút)

Tạo **một** `CategoryDto` chứa `Id` và `Name`, đặt ở `Queries/`, dùng cho cả hai query.

> Bên Products bạn đã lỡ tạo 3 DTO giống hệt nhau cho 3 query. Đừng lặp lại. Chỉ tách DTO khi hình dạng dữ liệu thực sự khác nhau — task 07 sẽ cho bạn ví dụ tách đúng.

### 3. Các Command (20 phút)

| Command | Tham số | Trả về |
|---|---|---|
| `CreateCategoryCommand` | `Name` | `Guid` |
| `UpdateCategoryCommand` | `CategoryId`, `Name` | không |
| `DeleteCategoryCommand` | `CategoryId` | không |

**Handler tạo mới:** kiểm tra trùng tên trước khi thêm. Trùng thì ném lỗi xung đột để client nhận `409` thay vì `500` từ unique index.

**Handler xoá — quan trọng:** trước khi gỡ, phải kiểm tra danh mục còn sản phẩm nào không. Còn thì ném lỗi xung đột kèm thông báo rõ ràng.

> Nhớ ở task trước bạn đã đặt `DeleteBehavior.Restrict` cho toàn bộ khoá ngoại. Không có bước kiểm tra này thì SQL Server vẫn chặn, nhưng client nhận `500` khó hiểu thay vì `409` có thông báo. Đây là khác biệt giữa "chạy được" và "dùng được".

### 4. Các Query (10 phút)

- `GetAllCategoriesQuery` → danh sách, sắp theo tên
- `GetCategoryByIdQuery` → một cái, không thấy thì ném `KeyNotFoundException`

Cả hai dùng projection, không load entity.

### 5. Validator (5 phút)

Tên: không rỗng, tối đa 100 ký tự. Id: không rỗng.

### 6. Unique index trên Name (3 phút)

Khai báo trong `OnModelCreating`, rồi tạo migration và cập nhật DB.

Bước 3 đã chặn trùng ở tầng ứng dụng rồi, nhưng index ở tầng DB là chốt chặn cuối — hai request đồng thời có thể cùng vượt qua bước kiểm tra ở tầng ứng dụng.

### 7. Controller (15 phút)

| Method | Route | Quyền |
|---|---|---|
| GET | `/api/categories` | Công khai |
| GET | `/api/categories/{id:guid}` | Công khai |
| POST | `/api/categories` | ADMIN |
| PUT | `/api/categories/{id:guid}` | ADMIN |
| DELETE | `/api/categories/{id:guid}` | ADMIN |

**Cho PUT:** tạo một record body riêng chỉ chứa `Name`, không chứa `CategoryId`. Id lấy từ route, controller ghép hai thứ lại thành command. Đây đúng là cách bạn đã chốt ở phần Product — body và command là hai hợp đồng khác nhau.

Gắn `[Authorize]` lên từng method vì controller này có cả endpoint công khai lẫn endpoint riêng ADMIN.

## AC

- [ ] `GET /api/categories` không token → `200`
- [ ] `POST` không token → `401`
- [ ] `POST` với token CUSTOMER → `403`
- [ ] `POST` với token ADMIN → `201`
- [ ] Tạo trùng tên → `409`
- [ ] `name` rỗng → `400`
- [ ] Xoá danh mục đang có sản phẩm → `409` kèm thông báo tiếng Việt rõ ràng
- [ ] Xoá danh mục rỗng → `204`
- [ ] Chỉ có **một** file DTO trong `Application/Categories/`

## Test API

```http
### 1. Xem danh sách (không cần token)
GET http://localhost:5118/api/categories
```

```http
### 2. Admin tạo danh mục
POST http://localhost:5118/api/categories
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "name": "Phụ kiện máy tính"
}
```
→ `201` + id. **Lưu id này lại** cho các task sau.

```http
### 3. Tạo trùng tên → phải 409
POST http://localhost:5118/api/categories
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "name": "Phụ kiện máy tính"
}
```

```http
### 4. Tên rỗng → phải 400
POST http://localhost:5118/api/categories
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "name": ""
}
```

```http
### 5. Customer tạo → phải 403
POST http://localhost:5118/api/categories
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "name": "Danh mục lậu"
}
```

```http
### 6. Sửa tên
PUT http://localhost:5118/api/categories/{categoryId}
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "name": "Phụ kiện máy tính & Gaming"
}
```

```http
### 7. Xoá danh mục đang có sản phẩm → phải 409
DELETE http://localhost:5118/api/categories/0199c1a0-0000-7000-8000-000000000001
Authorization: Bearer <TOKEN_ADMIN>
```

```http
### 8. Xoá danh mục rỗng → 204
DELETE http://localhost:5118/api/categories/{categoryId_vua_tao}
Authorization: Bearer <TOKEN_ADMIN>
```

---

**Tiếp theo:** [03-product-admin.md](03-product-admin.md)
