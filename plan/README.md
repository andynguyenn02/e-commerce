# Kế hoạch hoàn thiện E-Commerce

**Tổng: ~38 giờ ≈ 4 ngày × 10 giờ**

Làm đúng theo thứ tự số file. Mỗi file là một task độc lập, xong là chạy được và test được ngay.

## Tin tốt trước khi bắt đầu

Model domain hiện tại của bạn **đã giải đúng sẵn 3 yêu cầu khó nhất**:

| Yêu cầu | Đã có trong model |
|---|---|
| §3, §9 — Giá lịch sử không đổi khi admin sửa giá | `OrderItemEntity.PriceAtPurchased` |
| §6 — Giỏ hàng luôn theo giá mới nhất | `CartItemEntity` không lưu giá → luôn đọc từ `ProductEntity.Price` |
| §7 — Truy vết tiền trừ vì đơn nào | `WalletTransactionEntity(WalletId, OrderId, Amount)` |

Không cần sửa gì ở 3 chỗ này. Chỉ cần dùng đúng.

## Lịch

### Ngày 1 — Nền tảng + Sản phẩm (10h)

| File | Task | Giờ |
|---|---|---|
| [00-cleanup.md](00-cleanup.md) | Dọn nợ kỹ thuật hiện tại | 0.5 |
| [01-auth-jwt.md](01-auth-jwt.md) | Đăng ký, đăng nhập, JWT, phân quyền | 3 |
| [02-category-crud.md](02-category-crud.md) | CRUD danh mục | 1 |
| [03-product-admin.md](03-product-admin.md) | Phân quyền product, sửa giá, phân trang | 2 |
| [04-cart.md](04-cart.md) | Giỏ hàng | 3.5 |

### Ngày 2 — Tiền và Đơn hàng (10h)

| File | Task | Giờ |
|---|---|---|
| [05-wallet.md](05-wallet.md) | Ví điện tử | 1 |
| [06-checkout.md](06-checkout.md) | Thanh toán + transaction | 3 |
| [07-order-history.md](07-order-history.md) | Lịch sử đơn hàng | 1.5 |
| [08-inventory-upload.md](08-inventory-upload.md) | Upload file tồn kho | 2 |
| [09-background-worker.md](09-background-worker.md) | Channel + BackgroundService | 2.5 |

### Ngày 3 — Email, Test, Frontend nền (10h)

| File | Task | Giờ |
|---|---|---|
| [10-email.md](10-email.md) | Gửi mail qua hàng đợi | 2 |
| [11-tests.md](11-tests.md) | xUnit | 3 |
| [12-frontend-setup.md](12-frontend-setup.md) | Vite + React + Redux + auth | 2 |
| [13-frontend-customer.md](13-frontend-customer.md) | Màn hình Customer (phần 1) | 3 |

### Ngày 4 — Frontend còn lại + Hoàn thiện (8h)

| File | Task | Giờ |
|---|---|---|
| [13-frontend-customer.md](13-frontend-customer.md) | Màn hình Customer (phần 2) | 1 |
| [14-frontend-admin.md](14-frontend-admin.md) | Màn hình Admin | 3 |
| [15-polish.md](15-polish.md) | README, CI, dọn cuối | 2 |
| — | Dự phòng | 2 |

## Cách dùng

1. Mở đúng **một** file task. Đừng đọc trước file sau.
2. Làm hết các bước đánh số trong file.
3. Chạy phần **Test API** ở cuối file — body request đã viết sẵn, copy dán thẳng vào Swagger.
4. Tick hết ô **AC** mới sang task tiếp theo.
5. Commit sau mỗi task.

Luôn để 2 cửa sổ mở: `dotnet run --project ecommerce.Api` và Swagger tại http://localhost:5118/swagger

## Tiến độ

- [ ] 00 — Dọn nợ
- [ ] 01 — Auth + JWT
- [ ] 02 — Category CRUD
- [ ] 03 — Product admin
- [ ] 04 — Cart
- [ ] 05 — Wallet
- [ ] 06 — Checkout
- [ ] 07 — Order history
- [ ] 08 — Inventory upload
- [ ] 09 — Background worker
- [ ] 10 — Email
- [ ] 11 — Tests
- [ ] 12 — Frontend setup
- [ ] 13 — Frontend customer
- [ ] 14 — Frontend admin
- [ ] 15 — Polish

## Quy ước chung cho toàn bộ plan

**Cấu trúc thư mục mỗi feature** (lặp y hệt Products):

```
ecommerce.Application/<Feature>/
├── Commands/<Tên>/
│   ├── <Tên>Command.cs
│   ├── <Tên>CommandHandler.cs
│   └── <Tên>CommandValidator.cs
└── Queries/<Tên>/
    ├── <Tên>Query.cs
    ├── <Tên>QueryHandler.cs
    └── <Tên>Dto.cs
```

**Quy tắc đặt tầng** — khi phân vân thứ gì nằm đâu:

| Thứ | Nằm ở |
|---|---|
| Entity, enum, quy tắc nghiệp vụ thuần | `Domain` |
| Command, Query, Handler, Validator, DTO, interface của dịch vụ ngoài | `Application` |
| EF Core, gửi mail thật, đọc ghi file, sinh JWT | `Infrastructure` |
| Controller, middleware, cấu hình DI của web | `Api` |

**Luật vàng**: `Application` chỉ được khai báo `interface`. Bản cài đặt luôn nằm ở `Infrastructure`.
