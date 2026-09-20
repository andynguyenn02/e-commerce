# 12 — Frontend: dựng nền

**2 giờ** · Đáp ứng requirement §11. Chưa làm màn hình nào, chỉ dựng bộ khung.

## Mục tiêu

Xong task này thì: đăng nhập được, token được lưu và tự gắn vào mọi request, route bị chặn theo role, và có sẵn khuôn mẫu 4 trạng thái để mọi màn hình sau copy theo.

## Các bước

### 1. Tạo project (15 phút)

Dùng Vite với template React + TypeScript. Đặt thư mục `ecommerce.Web/` ngang hàng các project .NET.

Cài thêm:
- `@reduxjs/toolkit` và `react-redux` — requirement bắt buộc Redux
- `react-router-dom` — điều hướng
- `axios` — gọi API

> Dùng **Redux Toolkit**, không dùng Redux thuần. Redux thuần cần viết action type, action creator, reducer, switch-case — khoảng 40 dòng cho một thao tác. RTK làm cùng việc đó trong 10 dòng và vẫn là Redux thật, thoả mãn requirement.

### 2. Bật CORS ở backend (10 phút)

Frontend chạy cổng khác backend, trình duyệt sẽ chặn. Ở `Program.cs` phía .NET: đăng ký một CORS policy cho phép origin của Vite (mặc định `http://localhost:5173`), cho phép mọi header và method.

Gọi `UseCors` **trước** `UseAuthentication`.

Chỉ mở cho origin cụ thể, đừng dùng `AllowAnyOrigin` kèm credentials — cấu hình đó bị trình duyệt từ chối.

### 3. Thiết kế Redux store (25 phút)

Requirement yêu cầu bạn tự thiết kế slice. Đề xuất 4 slice, chia theo **miền dữ liệu**, không chia theo màn hình:

| Slice | Giữ gì |
|---|---|
| `auth` | token, role, username, trạng thái đăng nhập |
| `products` | danh sách sản phẩm, thông tin phân trang, bộ lọc |
| `cart` | các item trong giỏ, tổng tiền |
| `orders` | danh sách đơn, đơn đang xem |

Ví thì không cần slice riêng — số dư chỉ hiện ở vài chỗ, nhét vào `auth` hoặc gọi trực tiếp là đủ. Thêm slice cho mỗi bảng DB là thói quen xấu.

### 4. Chuẩn hoá 4 trạng thái bất đồng bộ (20 phút)

Requirement §11 đòi 4 trạng thái: `idle`, `loading`, `success`, `error`.

Thiết kế một kiểu dùng chung cho mọi slice: mỗi slice giữ một trường `status` nhận 1 trong 4 giá trị đó, cộng một trường `error` chứa thông báo.

Với `createAsyncThunk`, RTK tự sinh 3 action: `pending`, `fulfilled`, `rejected`. Map thẳng chúng sang `loading`, `success`, `error` trong `extraReducers`.

> **Dùng `status` dạng chuỗi 4 giá trị, không dùng `isLoading: boolean`.** Boolean không phân biệt được "chưa gọi lần nào" với "gọi xong không có dữ liệu" — mà giao diện hai trường hợp đó phải khác nhau: một cái hiện skeleton, một cái hiện "Không có sản phẩm".

### 5. Lớp gọi API (25 phút)

Tạo một instance axios dùng chung với `baseURL` trỏ vào backend, đọc từ biến môi trường của Vite.

Hai interceptor:

**Request interceptor** — đọc token từ store (hoặc localStorage) và gắn vào header `Authorization` dạng `Bearer <token>`.

**Response interceptor** — gặp `401` thì xoá token, đưa về trang đăng nhập. Token hết hạn 8 tiếng nên chuyện này chắc chắn xảy ra.

> Có interceptor rồi thì **không component nào phải tự gắn token**. Quên gắn ở một chỗ là sinh bug lẻ tẻ rất khó tìm.

Đặt các hàm gọi API theo miền: `api/auth.ts`, `api/products.ts`, `api/cart.ts`, `api/orders.ts`, `api/inventory.ts`. Component không gọi axios trực tiếp.

### 6. Slice auth và luồng đăng nhập (25 phút)

Cần một async thunk gọi `POST /api/auth/login`, lưu `token` + `role` vào state, đồng thời ghi vào `localStorage` để F5 không bị đăng xuất.

Lúc khởi động app, đọc lại `localStorage` để khôi phục phiên.

Cần thêm một action `logout` xoá cả state lẫn `localStorage`.

> **Về bảo mật:** để token trong `localStorage` là điểm yếu trước XSS. Cách an toàn hơn là httpOnly cookie, nhưng đòi backend đổi sang cấp cookie và xử lý CSRF. Với bài training, `localStorage` chấp nhận được — nhưng **phải biết** là mình đang đánh đổi cái gì. Ghi lại vào đây.

### 7. Route và chặn theo role (20 phút)

Cấu trúc route đề xuất:

| Route | Ai vào được |
|---|---|
| `/login` | Mọi người |
| `/products` | Mọi người |
| `/cart` | CUSTOMER |
| `/orders`, `/orders/:id` | CUSTOMER |
| `/admin/products` | ADMIN |
| `/admin/inventory` | ADMIN |

Viết một component bọc route, nhận vào role được phép. Không có token → chuyển về `/login`. Sai role → chuyển về `/products`.

> **Requirement §2 nói rõ:** *"Frontend route restrictions alone are not considered sufficient security."*
>
> Chặn route ở frontend chỉ để **trải nghiệm** — tránh hiện menu vô nghĩa. Bảo mật thật nằm ở `[Authorize]` phía backend, đã làm ở task 01. Ai đó sửa state trong DevTools vẫn vào được trang admin, nhưng mọi API gọi đi đều `403`. Đó mới là phòng tuyến thật.

## AC

- [ ] `npm run dev` chạy, mở được trang
- [ ] Gọi API từ frontend không bị lỗi CORS
- [ ] Đăng nhập → token lưu vào localStorage, chuyển sang `/products`
- [ ] F5 → vẫn đăng nhập, không bị đá ra
- [ ] Đăng xuất → localStorage sạch, về `/login`
- [ ] Mọi request có header `Authorization` (kiểm tra tab Network)
- [ ] Chưa đăng nhập vào `/cart` → bị đẩy về `/login`
- [ ] Đăng nhập CUSTOMER vào `/admin/products` → bị đẩy về `/products`
- [ ] Redux DevTools thấy được 4 slice
- [ ] Đăng nhập sai → hiện lỗi, không trắng trang

## Cách test

### 1. CORS

Mở DevTools → Console. Gọi thử danh sách sản phẩm từ frontend. Không được có lỗi đỏ dạng *"blocked by CORS policy"*.

Nếu có: kiểm tra `UseCors` đã gọi **trước** `UseAuthentication` chưa, và origin trong policy có đúng cổng Vite không.

### 2. Đăng nhập

Dùng tài khoản:
```
customer1 / Customer@123
admin / Admin@123
```

Sau khi đăng nhập, mở DevTools → Application → Local Storage: phải thấy token.

### 3. Interceptor

DevTools → Network → chọn một request bất kỳ tới API → tab Headers. Phải có:
```
Authorization: Bearer eyJhbGciOi...
```

### 4. Xử lý 401

Sửa tay token trong localStorage thành chuỗi rác, rồi F5 và gọi một API cần auth. Phải tự động về `/login`, không được trắng trang hay treo vòng xoay mãi.

### 5. Chặn route

| Làm gì | Kỳ vọng |
|---|---|
| Chưa đăng nhập, gõ `/cart` | Về `/login` |
| Đăng nhập CUSTOMER, gõ `/admin/products` | Về `/products` |
| Đăng nhập ADMIN, gõ `/cart` | Về `/products` |

### 6. Kiểm chứng "frontend không phải bảo mật"

Đăng nhập bằng CUSTOMER. Mở Redux DevTools, sửa `auth.role` thành `"ADMIN"`. Trang admin sẽ hiện ra.

Nhưng bấm nút tạo sản phẩm → API trả `403`.

Đúng như thiết kế: giao diện lừa được, backend thì không.

---

**Tiếp theo:** [13-frontend-customer.md](13-frontend-customer.md)
