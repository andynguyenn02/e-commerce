# 07 — Lịch sử đơn hàng

**1.5 giờ** · Thu hoạch thành quả của task 06. Đáp ứng requirement §9.

## Mục tiêu

Customer xem lại đơn đã mua, với **giá đã trả** chứ không phải giá hiện tại.

## Nguyên tắc

Mọi con số hiển thị phải lấy từ `OrderItemEntity`, **không** từ `ProductEntity`:

| Hiển thị | Lấy từ | Không lấy từ |
|---|---|---|
| Giá đã trả | `OrderItem.PriceAtPurchased` | ~~`Product.Price`~~ |
| Số lượng | `OrderItem.Quantity` | — |
| Tên sản phẩm | `Product.Name` | (chấp nhận được) |

Tên sản phẩm lấy từ `Product` là chấp nhận được ở mức training — đổi tên sản phẩm thì lịch sử hiển thị tên mới. Muốn chuẩn tuyệt đối thì snapshot cả tên vào `OrderItem`. **Chưa cần làm bây giờ.**

Giá thì **bắt buộc** snapshot, vì requirement nói thẳng.

## Các bước

### 1. Thiết kế DTO — lần này tách là đúng (15 phút)

Cần 3 record:

| DTO | Dùng cho | Nội dung |
|---|---|---|
| `OrderSummaryDto` | Danh sách đơn | Id đơn, ngày mua, số loại sản phẩm, tổng tiền |
| `OrderItemDto` | Một dòng trong đơn | Id + tên + mã sản phẩm, số lượng, giá đã trả, thành tiền |
| `OrderDetailDto` | Một đơn cụ thể | Id đơn, ngày mua, danh sách item, tổng tiền |

> **Summary và Detail tách nhau là tách đúng** — danh sách không cần kéo theo toàn bộ item của mọi đơn. Khác hẳn với 3 DTO trùng lặp bên Product hồi đầu, vốn giống hệt nhau nên phải gộp.
>
> Quy tắc: tách khi **hình dạng dữ liệu khác nhau**, không tách vì **endpoint khác nhau**.

### 2. Query danh sách đơn (30 phút)

Query không tham số. Lọc theo user hiện tại, sắp giảm dần theo thời gian tạo.

Với mỗi đơn cần đếm số item và tính tổng tiền. Hướng đi: viết hai subquery ngay bên trong `Select` — đếm và cộng trên bảng `OrderItems` lọc theo `OrderId`.

EF dịch được cả hai thành SQL, nên chỉ một round-trip, không bị N+1.

> **Vì sao không thêm cột `TotalAmount` vào `OrderEntity`?** Vì nó tính được từ `OrderItems`, và dữ liệu trùng lặp là nguồn của bug — hai chỗ lưu cùng một sự thật thì sớm muộn chúng lệch nhau. Chỉ lưu sẵn khi đã **đo** được là chậm.

Ngày mua dùng luôn `CreatedAt` của đơn — đơn sinh ra đúng lúc thanh toán nên hai thứ là một. Không cần thêm cột.

### 3. Query chi tiết đơn (30 phút)

Query nhận `OrderId`.

Handler theo thứ tự:
1. Tìm đơn → không có thì `404`
2. **Kiểm tra đơn có thuộc về user đang gọi không** → không thì chặn
3. Lấy các order item, projection sang DTO, giá lấy từ `PriceAtPurchased`
4. Tổng tiền cộng từ các thành tiền

> **Bước 2 là bắt buộc.** Thiếu nó thì ai cũng xem được đơn hàng người khác chỉ bằng cách đoán `orderId` — cùng lỗ hổng IDOR như task 04, nhưng nghiêm trọng hơn vì đơn hàng chứa thông tin mua sắm cá nhân.

> **Bẫy cần lường trước:** global query filter `!IsDeleted` bạn đặt ở task 00 sẽ khiến `JOIN` sang `Products` loại bỏ sản phẩm đã ngừng bán, làm item biến mất khỏi lịch sử. Lịch sử phải hiện đủ kể cả sản phẩm không còn bán. Tra `IgnoreQueryFilters()` và cân nhắc áp cho riêng query này.

### 4. Controller (15 phút)

Thêm vào `OrdersController` đã tạo ở task 06 (class đã có `[Authorize(Roles = "CUSTOMER")]`):

| Method | Route | Việc |
|---|---|---|
| GET | `/api/orders` | Danh sách đơn của tôi |
| GET | `/api/orders/{id:guid}` | Chi tiết một đơn |

## AC

- [ ] `GET /api/orders` → danh sách, mới nhất lên đầu
- [ ] `totalAmount` trong summary khớp `totalAmount` trả về lúc checkout
- [ ] `GET /api/orders/{id}` → đủ item, `priceAtPurchased` đúng giá lúc mua
- [ ] ★ Admin đổi giá → gọi lại `GET /api/orders/{id}`, giá **không đổi**
- [ ] Customer A xem `orderId` của Customer B → bị chặn
- [ ] `GET /api/orders/{guid-không-tồn-tại}` → `404`
- [ ] Customer chưa mua gì → `200` với `[]`
- [ ] Admin gọi `GET /api/orders` → `403`
- [ ] Đơn chứa sản phẩm đã xoá mềm vẫn hiện đủ item

## Test API

```http
### 1. Danh sách đơn
GET http://localhost:5118/api/orders
Authorization: Bearer <TOKEN_CUSTOMER>
```

Dạng kết quả:
```json
[
  {
    "orderId": "...",
    "purchasedAt": "2026-09-20T10:30:00Z",
    "itemCount": 1,
    "totalAmount": 200
  }
]
```

```http
### 2. Chi tiết đơn
GET http://localhost:5118/api/orders/{orderId}
Authorization: Bearer <TOKEN_CUSTOMER>
```

Dạng kết quả:
```json
{
  "orderId": "...",
  "purchasedAt": "2026-09-20T10:30:00Z",
  "items": [
    {
      "productName": "Chuột không dây",
      "quantity": 2,
      "priceAtPurchased": 100,
      "lineTotal": 200
    }
  ],
  "totalAmount": 200
}
```

### ★ 3. Bài test quyết định — lịch sử bất biến

```http
### 3a. Ghi lại giá trong đơn
GET http://localhost:5118/api/orders/{orderId}
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `priceAtPurchased: 100`

```http
### 3b. Admin đổi giá sản phẩm lên 500
PATCH http://localhost:5118/api/products/<ID_SP>/price
Content-Type: application/json
Authorization: Bearer <TOKEN_ADMIN>

{
  "price": 500
}
```

```http
### 3c. Xem lại ĐÚNG đơn đó
GET http://localhost:5118/api/orders/{orderId}
Authorization: Bearer <TOKEN_CUSTOMER>
```
→ `priceAtPurchased: 100`, `totalAmount: 200` — **không đổi**

```http
### 3d. Nhưng sản phẩm thì giá mới
GET http://localhost:5118/api/products/<ID_SP>
```
→ `price: 500`

Đây chính là §3 và §9. Nếu 3c ra `500` thì handler đang `JOIN` lấy giá từ `Product` — sửa lại dùng `PriceAtPurchased`.

```http
### 4. Test IDOR — dùng token customer2
GET http://localhost:5118/api/orders/{orderId_CUA_CUSTOMER1}
Authorization: Bearer <TOKEN_CUSTOMER2>
```
→ phải bị chặn, **không** được trả dữ liệu

```http
### 5. Đơn không tồn tại → 404
GET http://localhost:5118/api/orders/00000000-0000-0000-0000-000000000099
Authorization: Bearer <TOKEN_CUSTOMER>
```

```http
### 6. Admin xem đơn → 403
GET http://localhost:5118/api/orders
Authorization: Bearer <TOKEN_ADMIN>
```

### 7. Test sản phẩm đã xoá mềm

Xoá mềm sản phẩm đã nằm trong đơn, rồi gọi lại `GET /api/orders/{orderId}`. Item phải **vẫn hiện**. Nếu mất → cần `IgnoreQueryFilters()`.

---

**Tiếp theo:** [08-inventory-upload.md](08-inventory-upload.md)
