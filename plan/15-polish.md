# 15 — Hoàn thiện và chuẩn bị bảo vệ

**2 giờ** · Việc cuối. Biến "code chạy được" thành "dự án nộp được".

## Mục tiêu

Người khác clone repo về chạy được trong 5 phút. Và bạn trả lời được mọi câu hỏi review.

## Các bước

### 1. Viết README (40 phút)

Đặt ở gốc repo. Cần đủ 6 phần:

| Phần | Nội dung |
|---|---|
| Giới thiệu | 3 câu: đây là gì, làm được gì |
| Kiến trúc | Sơ đồ 4 tầng + chiều phụ thuộc, 5 dòng giải thích |
| Công nghệ | Liệt kê có version |
| Chạy thế nào | Từng lệnh, từ `git clone` tới mở được trang |
| Tài khoản mẫu | admin/Admin@123, customer1/Customer@123 |
| Quyết định thiết kế | Xem bước 2 |

Phần "chạy thế nào" phải **thực sự chạy được**. Cách kiểm tra: clone repo vào thư mục khác, làm theo đúng README, không dùng kiến thức trong đầu. Thiếu bước nào sẽ lộ ra ngay.

### 2. Ghi lại các quyết định thiết kế (30 phút)

Đây là phần người review quan tâm nhất, và là phần requirement nhấn mạnh: *"Developers should be able to explain..."*

Mỗi mục viết 2–4 câu: **quyết định gì, vì sao, đánh đổi gì.**

Danh sách cần có:

1. **Vì sao dùng `IAppDbContext` thay vì Repository pattern** — `DbContext` đã là Unit of Work, `DbSet` đã là Repository. Thêm một lớp nữa là bọc cái đã bọc.
2. **Giá lịch sử giải thế nào** — `OrderItem.PriceAtPurchased` chụp giá lúc mua. Giỏ hàng cố ý **không** lưu giá để luôn theo giá mới.
3. **Vì sao xoá mềm sản phẩm** — sản phẩm nằm trong đơn cũ không được biến mất.
4. **Vì sao `DeleteBehavior.Restrict` toàn bộ** — có dữ liệu tiền, xoá dây chuyền làm mất sổ sách. Cộng thêm việc SQL Server chặn đa đường cascade.
5. **Vì sao hàng đợi in-memory, và giới hạn của nó** — requirement bắt buộc `Channel<T>`. App restart thì job chờ bị mất. Muốn bền vững cần message broker ngoài.
6. **Vì sao mail nằm ngoài transaction** — không giữ lock DB trong lúc chờ dịch vụ ngoài.
7. **Token để localStorage — đánh đổi gì** — yếu trước XSS, đổi lại đơn giản, không cần xử lý CSRF.
8. **Vì sao bỏ qua đua tranh khi mua đồng thời** — requirement cho phép. Muốn xử lý thì thêm `RowVersion` để khoá lạc quan.

Viết được 8 mục này là bạn đã chứng minh hiểu bài, không chỉ làm theo hướng dẫn.

### 3. Dọn code lần cuối (20 phút)

Rà soát:

- [ ] Không còn `Console.WriteLine` debug sót lại
- [ ] Không còn endpoint tạm (nhất là cái dùng để sinh hash mật khẩu admin ở task 01)
- [ ] Không còn `TODO` chưa xử lý — hoặc xử lý, hoặc chuyển thành issue
- [ ] Không còn `using` thừa
- [ ] `dotnet build` — 0 warning
- [ ] Không có bí mật nào trong git: `git log -p | grep -i password` không ra kết quả đáng ngại

### 4. Đối chiếu requirement (20 phút)

Mở file requirement gốc, đi từng mục, đánh dấu chỗ code đáp ứng:

| Mục | Yêu cầu | Đáp ứng ở |
|---|---|---|
| §2 | Phân quyền 2 role, backend enforce | `[Authorize(Roles=...)]` mọi controller |
| §3 | Sửa giá, đơn cũ giữ giá cũ | `PATCH .../price` + `PriceAtPurchased` |
| §4 | Xử lý nền bằng BackgroundService + Channel | Task 09 |
| §5 | Uploads/Archive, tên file không đè nhau | `LocalFileStorage`, tên GUID |
| §6 | Giỏ theo giá mới nhất, backend tính tiền | Task 04 |
| §7 | Ví $1000, truy vết được tiền trừ vì đơn nào | `WalletTransaction.OrderId` |
| §8 | Checkout nguyên tử bằng EF transaction | Task 06 |
| §9 | Lịch sử đơn bất biến | Task 07 |
| §10 | Mail không giữ transaction | Task 10 |
| §11 | React + Redux, 4 trạng thái | Task 12–14 |
| §12 | Clean Architecture 4 tầng | Toàn bộ solution |
| §13 | REST API đúng quy ước | Toàn bộ controller |
| §14 | Nghiệp vụ không phụ thuộc hạ tầng | Các interface ở Application |

Mục nào chưa đủ thì ghi rõ vào README ở phần "Giới hạn hiện tại". **Thừa nhận thiếu sót tốt hơn để người review tự phát hiện.**

### 5. CI cơ bản (10 phút, tuỳ chọn)

Một workflow GitHub Actions chạy `dotnet build` và `dotnet test` mỗi lần push. Khoảng 20 dòng YAML.

Giá trị: chứng minh code build được trên máy sạch, không chỉ trên máy bạn.

## AC

- [ ] README có đủ 6 phần
- [ ] Làm theo README từ thư mục trống → chạy được app
- [ ] Có đủ 8 mục quyết định thiết kế
- [ ] `dotnet build` — 0 warning
- [ ] `dotnet test` — tất cả xanh
- [ ] Không còn endpoint tạm
- [ ] Bảng đối chiếu requirement đầy đủ, mục thiếu được ghi nhận thật thà

## Bài kiểm tra cuối — chạy hết một vòng

Làm từ DB trắng, không dùng dữ liệu cũ:

```bash
docker compose down -v
docker compose up -d
dotnet ef database update -p ecommerce.Infrastructure -s ecommerce.Api
dotnet run --project ecommerce.Api
```

Rồi đi hết luồng:

1. Đăng nhập `admin` → tạo danh mục → tạo sản phẩm giá 100, tồn 10
2. Upload file tồn kho → đợi `DONE` → kiểm tra giá/tồn đã đổi → kiểm tra mail ở Mailpit
3. Đăng ký `customer_test` → kiểm tra ví có 1000
4. Thêm 2 sản phẩm vào giỏ → kiểm tra tổng tiền
5. Admin đổi giá → khách xem lại giỏ → **tổng tiền đổi theo**
6. Khách thanh toán → kiểm tra: ví giảm, tồn giảm, giỏ rỗng, mail đã gửi
7. Admin đổi giá lần nữa → khách xem đơn cũ → **giá không đổi**
8. Thử checkout khi không đủ tiền → kiểm tra **không có gì thay đổi**

Đi trọn 8 bước không lỗi là dự án hoàn thành.

## Câu hỏi review hay gặp — chuẩn bị trước

| Câu hỏi | Ý chính để trả lời |
|---|---|
| "Vì sao không dùng Repository pattern?" | `DbContext` = UoW, `DbSet` = Repository. Bọc thêm không thêm giá trị, chỉ thêm code. |
| "Làm sao đảm bảo giá lịch sử không đổi?" | Chụp giá vào `OrderItem.PriceAtPurchased` lúc checkout. Chỉ ra dòng code đó. |
| "Checkout nguyên tử thế nào?" | `BeginTransactionAsync`, một `SaveChangesAsync`, commit hoặc rollback. Chỉ ra test rollback. |
| "Nếu 2 người cùng mua sản phẩm cuối?" | Bài này bỏ qua theo requirement. Nếu cần: `RowVersion` + khoá lạc quan. |
| "Vì sao Channel chứ không phải Hangfire?" | Requirement bắt buộc. Kèm theo: nêu được giới hạn mất job khi restart. |
| "Nghiệp vụ tách khỏi hạ tầng ở đâu?" | Chỉ ra `IEmailSender`, `IFileStorage`, `IJwtTokenGenerator` khai báo ở Application, cài đặt ở Infrastructure. |
| "Frontend chặn route là đủ bảo mật chưa?" | Chưa. Demo: sửa role trong Redux DevTools → vào được trang, nhưng API trả `403`. |

Câu cuối đáng chuẩn bị demo trực tiếp — nó chứng minh bạn hiểu **vì sao** requirement viết câu đó, không chỉ làm theo.

---

**Xong.** Cập nhật checklist trong [README.md](README.md).
