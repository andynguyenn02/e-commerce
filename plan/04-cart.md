# 04 — Giỏ hàng

**3.5 giờ** · Task lớn nhất ngày 1. Đáp ứng requirement §6.

## Mục tiêu

Customer thêm/sửa/xoá sản phẩm trong giỏ. Giỏ **luôn tính theo giá hiện tại**. Admin bị chặn hoàn toàn.

## Nguyên tắc cốt lõi

**Giỏ hàng không bao giờ lưu giá.**

`CartItemEntity` chỉ có `ProductId` và `Quantity`. Mỗi lần xem giỏ, giá đọc mới từ `ProductEntity.Price`. Admin đổi giá → giỏ chưa thanh toán tự đổi theo. Requirement §6 được giải bằng việc **không làm gì cả** — chỉ cần không thêm cột giá.

Đây là chỗ nhiều người làm sai: thấy "cart item" thì phản xạ thêm cột `Price`. Thêm vào là hỏng §6.

## Các bước

### 1. Dựng cấu trúc (5 phút)

```
Application/Carts/
├── Commands/
│   ├── AddItemToCart/
│   ├── UpdateCartItemQuantity/
│   ├── RemoveCartItem/
│   └── ClearCart/
└── Queries/
    └── GetMyCart/
```

### 2. Thiết kế DTO (10 phút)

Cần 2 record:

**`CartItemDto`** — mỗi dòng trong giỏ cần đủ thông tin để frontend vẽ:
- Id của cart item (để gọi sửa/xoá)
- Id, tên, mã sản phẩm
- Đơn giá **hiện tại**
- Số lượng đang đặt
- Tồn kho còn lại (để frontend cảnh báo khi vượt)
- Thành tiền của dòng

**`CartDto`** — id giỏ, danh sách item, tổng tiền.

Tính thành tiền và tổng tiền ở **backend**, không để frontend tự nhân. Requirement §6 nói rõ frontend không phải nguồn tin cậy về giá.

### 3. Query xem giỏ (30 phút)

Query không có tham số — user lấy từ `ICurrentUser`.

Handler:
1. Tìm giỏ theo `UserId`
2. **Chưa có giỏ → trả giỏ rỗng, không ném 404.** Người dùng mới chưa từng thêm gì là chuyện bình thường.
3. Lấy các cart item, projection sang DTO, trong đó đơn giá đọc từ navigation sang `Product`
4. Tổng tiền = tổng các thành tiền

> **Mẹo EF:** truy cập `ci.Product!.Name` ngay bên trong `Select` thì EF tự sinh `JOIN`. Không cần `Include`. `Include` chỉ dùng khi cần load entity thật để sửa.

### 4. Command thêm sản phẩm (45 phút)

Command nhận `ProductId` + `Quantity`.

Handler theo thứ tự:

1. Tìm sản phẩm, không có thì `404`
2. Tìm giỏ của user — **chưa có thì tạo mới** ngay trong handler này
3. Kiểm tra sản phẩm đã có trong giỏ chưa
4. Tính số lượng mới = số lượng cũ (nếu có) + số lượng thêm
5. So với tồn kho, vượt thì ném lỗi xung đột kèm số còn lại
6. Đã có thì cập nhật số lượng, chưa có thì thêm dòng mới
7. Lưu

Ba quyết định thiết kế cần hiểu:

| Quyết định | Lý do |
|---|---|
| **Giỏ tạo lazy** — chỉ sinh khi thêm món đầu tiên | Không cần tạo giỏ rỗng cho mọi user lúc đăng ký |
| **Cộng dồn thay vì tạo dòng mới** | Thêm cùng sản phẩm 2 lần thì `Quantity` tăng, giỏ vẫn 1 dòng |
| **Kiểm tra tồn kho ngay khi thêm** | Báo sớm cho người dùng — nhưng checkout **vẫn phải kiểm tra lại** |

Validator: `ProductId` không rỗng, `Quantity` > 0.

### 5. Sửa số lượng (25 phút)

Command nhận `CartItemId` + `Quantity` mới.

Handler cần load cart item **kèm cả** `Product` và `Cart` (chỗ này cần `Include` thật, vì phải đọc `Cart.UserId` và `Product.AvailableQuantity`).

Rồi 3 kiểm tra theo thứ tự:
1. Cart item có tồn tại không → `404`
2. **Cart đó có thuộc về user đang gọi không** → nếu không thì chặn
3. Số lượng mới có vượt tồn kho không → `409`

> **Bước 2 là bắt buộc.** Không có nó, bất kỳ customer nào biết `cartItemId` của người khác đều sửa được giỏ người ta. Đây là lỗ hổng **IDOR** — lỗi bảo mật phổ biến nhất trong API CRUD.
>
> Quy tắc chung: với mọi tài nguyên thuộc sở hữu cá nhân, "đã đăng nhập" **không đủ**. Phải kiểm tra thêm "có phải của bạn không".

### 6. Xoá item và xoá sạch giỏ (20 phút)

**`RemoveCartItemCommand`** — kiểm tra quyền sở hữu y hệt bước 5, rồi gỡ khỏi `DbSet`.

**`ClearCartCommand`** — không tham số. Tìm giỏ của user, lấy hết item, gỡ hàng loạt. Giỏ không tồn tại thì return im lặng, không lỗi.

### 7. Chặn Admin (15 phút)

Requirement §2: *"An Admin cannot add products to a shopping cart."*

Gắn `[Authorize(Roles = "CUSTOMER")]` ở **cấp class** của controller, không phải từng method.

Đặt ở cấp class an toàn hơn: thêm action mới sau này tự động được bảo vệ, không sợ quên.

### 8. Controller (25 phút)

| Method | Route | Việc |
|---|---|---|
| GET | `/api/cart` | Xem giỏ |
| POST | `/api/cart/items` | Thêm sản phẩm |
| PUT | `/api/cart/items/{cartItemId:guid}` | Sửa số lượng |
| DELETE | `/api/cart/items/{cartItemId:guid}` | Xoá 1 item |
| DELETE | `/api/cart` | Xoá sạch giỏ |

**Không có `{userId}` trong bất kỳ URL nào.** User lấy từ token. Cho userId vào URL là mời gọi IDOR.

Cho PUT: body chỉ chứa `quantity`, id lấy từ route.

### 9. Exception cho lỗi quyền sở hữu (5 phút)

Lỗi "không phải giỏ của bạn" về ngữ nghĩa là `403 Forbidden` (biết bạn là ai, nhưng không cho), khác với `401 Unauthorized` (không biết bạn là ai).

Muốn chuẩn thì tạo một exception riêng và map sang `403`. Không muốn thì tái dùng `UnauthorizedAccessException` → `401`, chấp nhận được ở mức training.

## AC

- [ ] Customer thêm sản phẩm → `204`
- [ ] `GET /api/cart` → `unitPrice` khớp giá hiện tại, `lineTotal = unitPrice × quantity`, `total` đúng tổng
- [ ] Thêm **cùng một** sản phẩm lần 2 → `quantity` cộng dồn, giỏ vẫn 1 dòng
- [ ] Thêm vượt tồn kho → `409` kèm số lượng còn lại
- [ ] ★ **Admin đổi giá → `GET /api/cart` thấy `total` mới ngay** — AC quan trọng nhất
- [ ] Sửa `quantity` → total đổi theo
- [ ] Xoá item → item biến mất
- [ ] Customer A sửa `cartItemId` của Customer B → bị chặn
- [ ] Admin gọi bất kỳ endpoint cart nào → `403`
- [ ] Customer chưa thêm gì gọi `GET /api/cart` → `200` với `items: []`, không phải `404`

## Test API

```http
### 1. Giỏ rỗng ban đầu
GET http://localhost:5118/api/cart
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `{ "cartId": "000...", "items": [], "total": 0 }`

```http
### 2. Thêm 2 chuột
POST http://localhost:5118/api/cart/items
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "productId": "<ID_CHUOT_MS-001>",
  "quantity": 2
}
```

```http
### 3. Xem giỏ
GET http://localhost:5118/api/cart
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `unitPrice: 520000`, `quantity: 2`, `lineTotal: 1040000`, `total: 1040000`

```http
### 4. Thêm cùng sản phẩm lần nữa → phải cộng dồn
POST http://localhost:5118/api/cart/items
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "productId": "<ID_CHUOT_MS-001>",
  "quantity": 3
}
```
Rồi `GET /api/cart` → **1 dòng**, `quantity: 5`

```http
### 5. Vượt tồn kho → phải 409
POST http://localhost:5118/api/cart/items
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "productId": "<ID_CHUOT_MS-001>",
  "quantity": 99999
}
```

### ★ 6. Bài test quan trọng nhất — giá mới áp vào giỏ

```http
### 6a. Admin đổi giá lên 600000
PATCH http://localhost:5118/api/products/<ID_CHUOT_MS-001>/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "price": 600000
}
```

```http
### 6b. Customer xem lại giỏ — KHÔNG thêm gì, KHÔNG sửa gì
GET http://localhost:5118/api/cart
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `unitPrice: 600000`, `total: 3000000` (5 × 600000)

Vẫn thấy giá cũ nghĩa là bạn đã lỡ lưu giá vào `CartItemEntity`. Gỡ cột đó ra.

```http
### 7. Sửa số lượng
PUT http://localhost:5118/api/cart/items/{cartItemId}
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER>

{
  "quantity": 1
}
```

```http
### 8. Admin gọi cart → phải 403
GET http://localhost:5118/api/cart
Authorization: Bearer <TOKEN_ADMIN>
```

```http
### 9. Xoá item
DELETE http://localhost:5118/api/cart/items/{cartItemId}
Authorization: Bearer <TOKEN_CUSTOMER>
```

### 10. Test IDOR

Đăng ký thêm `customer2`, đăng nhập lấy token, rồi:

```http
PUT http://localhost:5118/api/cart/items/{cartItemId_CUA_CUSTOMER1}
Content-Type: application/json
Authorization: Bearer <TOKEN_CUSTOMER2>

{
  "quantity": 999
}
```
→ phải bị chặn. Nếu `204` thì bạn thiếu bước kiểm tra quyền sở hữu.

Body đăng ký customer2:
```json
{
  "userName": "customer2",
  "password": "Customer@123"
}
```

---

**Xong ngày 1.** Commit lại.

**Tiếp theo:** [05-wallet.md](05-wallet.md)
