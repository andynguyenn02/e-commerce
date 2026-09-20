# 01 — Đăng ký, Đăng nhập, JWT, Phân quyền

**3 giờ** · Task chặn đường mọi thứ còn lại. Cart, Order, Wallet đều cần biết "ai đang đăng nhập".

## Mục tiêu

Customer đăng ký → tự có ví $1000. Đăng nhập → nhận JWT. Backend tự chặn theo role, không tin frontend.

## Các bước

### 1. Cài package (5 phút)

- `Microsoft.AspNetCore.Authentication.JwtBearer` → project Api
- `BCrypt.Net-Next` → project Infrastructure
- `System.IdentityModel.Tokens.Jwt` → project Infrastructure

Dùng BCrypt để băm mật khẩu. Không lưu mật khẩu thô, và cũng không dùng SHA256 trần — nó quá nhanh nên dễ brute-force. BCrypt cố tình chậm.

### 2. Khai báo 3 interface ở Application (15 phút)

Tạo trong `Application/Common/Interfaces/`:

| Interface | Nhiệm vụ | Hình dạng |
|---|---|---|
| `IJwtTokenGenerator` | Sinh token từ user | Nhận `UserEntity`, trả `string` |
| `IPasswordHasher` | Băm và kiểm tra mật khẩu | 2 method: băm, và đối chiếu mật khẩu với hash |
| `ICurrentUser` | Biết ai đang gọi request | 2 property chỉ đọc: `UserId`, `Role` |

> **Vì sao 3 interface này nằm ở Application?** Handler cần chúng, mà Application không được biết JWT hay BCrypt là gì. Application khai báo *cần gì*, Infrastructure quyết định *làm thế nào*. Đây chính là điểm §14 requirement muốn bạn chứng minh được.

### 3. Cài đặt ở Infrastructure và Api (30 phút)

| Class | Đặt ở | Ghi chú |
|---|---|---|
| `PasswordHasher` | `Infrastructure/Security/` | Gói lại 2 hàm tĩnh của thư viện BCrypt |
| `JwtTokenGenerator` | `Infrastructure/Security/` | Inject `IConfiguration` để đọc khoá ký |
| `CurrentUser` | `ecommerce.Api/Services/` | Inject `IHttpContextAccessor`, đọc claim |

`CurrentUser` nằm ở Api chứ không phải Infrastructure, vì nó phụ thuộc `HttpContext` — đó là chi tiết của tầng HTTP.

**Về nội dung token:** cần tối thiểu 3 claim — id người dùng, tên đăng nhập, và role. Tra `ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, `ClaimTypes.Role`. Đặt hạn dùng khoảng 8 tiếng. Ký bằng `HmacSha256`.

Khi đọc role ra ở `CurrentUser`, giá trị phải khớp chính xác chuỗi mà `RoleEnum.ADMIN.ToString()` sinh ra, vì lát nữa `[Authorize(Roles = "...")]` so sánh theo chuỗi.

### 4. Cấu hình khoá bí mật (10 phút)

Khoá ký JWT phải **ít nhất 32 ký tự**, nếu ngắn hơn HmacSha256 sẽ ném lỗi lúc chạy.

- `Jwt:Key` → đặt bằng `dotnet user-secrets set`, **không** để trong appsettings
- `Jwt:Issuer`, `Jwt:Audience` → để trong `appsettings.json`, đây không phải bí mật

### 5. Đăng ký DI và pipeline (10 phút)

Ở `Infrastructure/DependencyInjection.cs`: đăng ký `IPasswordHasher` và `IJwtTokenGenerator` dạng Scoped.

Ở `Program.cs`:
- Thêm `AddHttpContextAccessor()` và đăng ký `ICurrentUser`
- Gọi `AddAuthentication(...).AddJwtBearer(...)` với `TokenValidationParameters` bật đủ 4 kiểm tra: issuer, audience, lifetime, signing key
- Gọi `AddAuthorization()`

**Thứ tự middleware bắt buộc:** `UseAuthentication` → `UseAuthorization` → `MapControllers`.

Đặt ngược (Authorization trước Authentication) thì mọi request đều 401, vì lúc phân quyền hệ thống chưa biết bạn là ai.

### 6. Command đăng ký (30 phút)

Tạo `Application/Auth/Commands/Register/` theo đúng cấu trúc 3 file như bên Products.

Command nhận `UserName` và `Password`, trả về `Guid` của user mới.

Handler làm theo thứ tự:
1. Kiểm tra username đã tồn tại chưa → nếu có, ném lỗi xung đột
2. Tạo `UserEntity` với role `CUSTOMER`, mật khẩu đã băm
3. Tạo `WalletEntity` cho user đó với `Balance = 1000`
4. Add cả hai, gọi **một** `SaveChangesAsync`

> **Ví tạo ngay lúc đăng ký** — đây là cách đáp ứng §7. Một `SaveChangesAsync` cho cả hai entity nên tự động nguyên tử, không cần transaction thủ công.

Validator: username không rỗng, tối đa 50 ký tự. Password không rỗng, tối thiểu 6 ký tự.

### 7. Command đăng nhập (20 phút)

Command nhận `UserName` + `Password`, trả về một record chứa `Token` và `Role`.

Handler: tìm user theo username, đối chiếu mật khẩu bằng `IPasswordHasher`, sinh token nếu đúng.

> **Dùng chung một thông báo lỗi** cho cả hai trường hợp "không tìm thấy user" và "sai mật khẩu". Báo riêng giúp kẻ tấn công dò ra danh sách tài khoản có thật trên hệ thống.

### 8. Bổ sung exception mapping (5 phút)

Thêm vào `switch` trong exception handler đã viết:

| Exception | Status |
|---|---|
| `UnauthorizedAccessException` | `401` |
| `InvalidOperationException` | `409` |

`409 Conflict` hợp cho các lỗi kiểu "đã tồn tại", "trạng thái không cho phép" — sau này Cart và Checkout dùng lại nhiều.

### 9. AuthController (10 phút)

Hai endpoint: `POST /api/auth/register` và `POST /api/auth/login`. Cả hai đều công khai, không gắn `[Authorize]`.

### 10. Seed tài khoản Admin (10 phút)

Không có endpoint nào tạo Admin — phải seed cứng qua `HasData` trong `OnModelCreating`.

Vướng mắc: `HasData` chỉ nhận **hằng số**, không gọi được hàm băm. Cách xử lý: chạy hàm băm một lần ở đâu đó (một endpoint tạm, LINQPad, hay dotnet-script), copy chuỗi hash ra, dán vào `HasData` dưới dạng chuỗi cố định. Nhớ xoá endpoint tạm sau đó.

Dùng `Guid` cố định để lần sau chạy migration không sinh Admin trùng.

> **Admin không có ví** — đúng yêu cầu §2, Admin không được mua hàng.

### 11. Khoá endpoint theo role (15 phút)

| Endpoint | Quyền |
|---|---|
| `GET /api/products`, `GET /api/products/{id}` | Công khai |
| `POST`, `PUT`, `DELETE /api/products` | `[Authorize(Roles = "ADMIN")]` |

Chuỗi trong `Roles` phải khớp chính xác giá trị `RoleEnum.ADMIN.ToString()`.

### 12. Nút Authorize trong Swagger (15 phút, tuỳ chọn)

OpenAPI của .NET 10 cần một document transformer để khai báo security scheme kiểu Bearer. Tra `AddDocumentTransformer` và `SecuritySchemeType.Http`.

Quá 15 phút thì bỏ qua, dùng `curl -H "Authorization: Bearer <token>"` để test cũng đủ.

### 13. Migration

```bash
dotnet ef migrations add Auth -p ecommerce.Infrastructure -s ecommerce.Api
dotnet ef database update -p ecommerce.Infrastructure -s ecommerce.Api
```

## AC

- [ ] Đăng ký tài khoản mới → `200` + userId, bảng `Wallets` có 1 dòng `Balance = 1000`
- [ ] Đăng ký trùng username → `409`
- [ ] Đăng nhập đúng → nhận token, dán vào jwt.io thấy claim `role`
- [ ] Đăng nhập sai mật khẩu → `401`
- [ ] `GET /api/products` không cần token → `200`
- [ ] `POST /api/products` không token → `401`
- [ ] `POST /api/products` với token CUSTOMER → `403`
- [ ] `POST /api/products` với token ADMIN → `201`
- [ ] Cột `PasswordHash` trong DB bắt đầu bằng `$2a$`, không phải mật khẩu thô

## Test API

```http
### 1. Đăng ký customer
POST http://localhost:5118/api/auth/register
Content-Type: application/json

{
  "userName": "customer1",
  "password": "Customer@123"
}
```

```http
### 2. Đăng nhập customer
POST http://localhost:5118/api/auth/login
Content-Type: application/json

{
  "userName": "customer1",
  "password": "Customer@123"
}
```
→ `{ "token": "eyJ...", "role": "CUSTOMER" }` — **lưu token lại**

```http
### 3. Đăng nhập admin
POST http://localhost:5118/api/auth/login
Content-Type: application/json

{
  "userName": "admin",
  "password": "Admin@123"
}
```
→ `role: "ADMIN"` — lưu token admin lại

```http
### 4. Customer tạo product → phải 403
POST http://localhost:5118/api/products
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "name": "Chuột không dây",
  "price": 450000,
  "code": "MS-001",
  "availableQuantity": 100,
  "categoryId": "0199c1a0-0000-7000-8000-000000000001"
}
```

```http
### 5. Admin tạo product → phải 201
POST http://localhost:5118/api/products
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "name": "Chuột không dây",
  "price": 450000,
  "code": "MS-001",
  "availableQuantity": 100,
  "categoryId": "0199c1a0-0000-7000-8000-000000000001"
}
```

```http
### 6. Sai mật khẩu → phải 401
POST http://localhost:5118/api/auth/login
Content-Type: application/json

{
  "userName": "customer1",
  "password": "sai-mat-khau"
}
```

```http
### 7. Trùng username → phải 409
POST http://localhost:5118/api/auth/register
Content-Type: application/json

{
  "userName": "customer1",
  "password": "Khac@123"
}
```

```bash
### 8. Đối chiếu DB
docker exec -it sql-dev /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Local_Dev_Pass123!' \
  -Q "SELECT u.UserName, u.Role, w.Balance FROM MyAppDb.dbo.Users u LEFT JOIN MyAppDb.dbo.Wallets w ON w.UserId = u.Id"
```

---

**Tiếp theo:** [02-category-crud.md](02-category-crud.md)
