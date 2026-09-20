# 11 — Unit test với xUnit

**3 giờ** · Requirement mục 1 có ghi "Unit testing with xUnit".

## Mục tiêu

Test những chỗ **sai thì mất tiền**, không test những chỗ sai thì chỉ xấu giao diện.

Không đặt mục tiêu phủ 100%. Đặt mục tiêu: nếu ai đó sửa hỏng logic checkout, test phải đỏ.

## Chọn thứ để test

Xếp theo mức độ đáng test:

| Ưu tiên | Đối tượng | Vì sao |
|---|---|---|
| 1 | `CheckoutCommandHandler` | Động tới tiền và tồn kho, 7 bước phải nguyên tử |
| 2 | `AddItemToCartCommand` — cộng dồn, chặn vượt kho | Logic có nhánh, dễ sai |
| 3 | Kiểm tra quyền sở hữu (IDOR) ở Cart và Order | Lỗ hổng bảo mật, không được hồi quy |
| 4 | Các Validator | Rẻ, nhanh, nhiều nhánh |
| 5 | Các Query đơn giản | Ít giá trị, chỉ là projection |

Làm đủ 1–4 trong 3 tiếng là tốt. Bỏ 5.

## Các bước

### 1. Tạo project test (15 phút)

```bash
dotnet new xunit -n ecommerce.Application.Tests
dotnet sln add ecommerce.Application.Tests
dotnet add ecommerce.Application.Tests reference ecommerce.Application ecommerce.Domain
```

Cài thêm vào project test:
- `Microsoft.EntityFrameworkCore.Sqlite` — xem bước 2
- `FluentAssertions` — assert đọc như tiếng Anh, thông báo lỗi rõ hơn nhiều
- `NSubstitute` hoặc `Moq` — giả lập `ICurrentUser`, `IEmailQueue`

### 2. Chọn database cho test — điểm quyết định (20 phút)

Hai lựa chọn, và lựa chọn sai sẽ làm bạn mất một tiếng để nhận ra:

| | EF InMemory | **SQLite in-memory** |
|---|---|---|
| Transaction | **Không hỗ trợ** — lặng lẽ bỏ qua | Có |
| Ràng buộc FK, unique | Không áp | Có |
| Giống SQL Server | Ít | Vừa |
| Cài đặt | Dễ hơn chút | Phải giữ connection mở |

> **Bắt buộc dùng SQLite in-memory** cho bài này. Test quan trọng nhất là checkout, mà checkout dựa vào transaction. EF InMemory sẽ khiến test rollback **luôn xanh dù code hoàn toàn sai** — tệ hơn là không có test.

Điểm cần biết về SQLite in-memory: database tồn tại chừng nào connection còn mở. Phải tự tạo và giữ `SqliteConnection` trong suốt vòng đời test, dispose ở cuối. Tra: `new SqliteConnection("Filename=:memory:")`, `EnsureCreated()`.

### 3. Viết lớp helper dựng DbContext (25 phút)

Cần một chỗ dùng chung để mỗi test lấy được một `ApplicationDbContext` sạch, đã tạo sẵn schema, và có thể seed dữ liệu.

Gợi ý hình dạng: một class implement `IDisposable`, constructor mở connection và tạo schema, có method trả về context, `Dispose` đóng connection.

Mỗi test **phải có DB riêng**. Dùng chung DB giữa các test thì thứ tự chạy ảnh hưởng kết quả — loại bug khó chịu nhất trong test.

Lưu ý: `ApplicationDbContext` nằm ở Infrastructure, nên project test cần reference cả Infrastructure. Đó là chấp nhận được — test được phép biết chi tiết cài đặt.

### 4. Giả lập ICurrentUser (10 phút)

Handler nào cũng inject `ICurrentUser`. Trong test, dùng NSubstitute/Moq tạo một bản giả trả về `Guid` cố định của user đã seed.

Đây chính là phần thưởng cho việc bạn đã tách `ICurrentUser` ra interface ở task 01. Nếu handler đọc thẳng `HttpContext` thì không test được nếu không dựng cả web server.

### 5. Test checkout — phần quan trọng nhất (60 phút)

Tối thiểu 5 test:

| Tên test | Sắp xếp | Kỳ vọng |
|---|---|---|
| Checkout thành công | Ví 1000, sp giá 100 tồn 10, giỏ 2 cái | Đơn được tạo, ví còn 800, tồn còn 8, giỏ rỗng |
| Chụp đúng giá tại thời điểm mua | Như trên, sau checkout đổi giá sp thành 999 | `OrderItem.PriceAtPurchased` vẫn là 100 |
| Số dư không đủ thì rollback | Ví 50, giỏ trị giá 200 | Ném exception, ví vẫn 50, **không có đơn nào**, giỏ **vẫn còn hàng** |
| Tồn kho không đủ thì rollback | Tồn 1, giỏ đặt 5 | Ném exception, không có đơn, tồn vẫn 1 |
| Giỏ rỗng | Không có cart item | Ném exception |

Ba test rollback là lý do phải dùng SQLite. Chúng kiểm tra rằng **không có thay đổi nào sót lại** sau khi thất bại — chính là điều requirement §8 đòi hỏi.

Viết theo cấu trúc Arrange–Act–Assert, đặt tên test theo dạng `Phương thức_Tình huống_KỳVọng` để đọc kết quả test là hiểu ngay.

### 6. Test giỏ hàng (35 phút)

| Tên test | Kỳ vọng |
|---|---|
| Thêm sản phẩm mới | Giỏ có 1 item, đúng số lượng |
| Thêm lại cùng sản phẩm | Vẫn 1 item, số lượng **cộng dồn** |
| Thêm vượt tồn kho | Ném exception, giỏ không đổi |
| Giỏ chưa tồn tại | Tự tạo giỏ mới, không ném lỗi |
| Sửa item của người khác | Ném exception (test IDOR) |
| Xem giỏ sau khi đổi giá sản phẩm | Tổng tiền phản ánh **giá mới** |

Test cuối cùng là bản dịch trực tiếp của requirement §6 thành code. Đáng viết nhất trong nhóm này.

### 7. Test validator (20 phút)

Validator test rất rẻ — khởi tạo, gọi `Validate`, kiểm tra kết quả. Không cần DB, không cần mock.

Dùng `[Theory]` + `[InlineData]` của xUnit để chạy một test với nhiều bộ dữ liệu. Ví dụ với `CreateProductCommandValidator`: giá `0`, `-1`, `-0.01` đều phải trượt; `0.01`, `100` phải qua.

Một `[Theory]` thay được 6 `[Fact]`.

### 8. Chạy và đọc kết quả (15 phút)

```bash
dotnet test
```

Muốn xem độ phủ: cài `coverlet.collector` rồi `dotnet test --collect:"XPlat Code Coverage"`.

Đừng đuổi theo con số phủ. 40% tập trung vào checkout và cart giá trị hơn 90% dàn đều lên các getter.

## AC

- [ ] `dotnet test` chạy được, tất cả xanh
- [ ] Có ít nhất 5 test cho `CheckoutCommandHandler`
- [ ] Có test chứng minh rollback: thất bại xong thì ví, tồn kho, giỏ đều **không đổi**
- [ ] Có test chứng minh `PriceAtPurchased` không đổi khi giá sản phẩm đổi
- [ ] Có test cộng dồn số lượng khi thêm trùng sản phẩm
- [ ] Có ít nhất 1 test IDOR (sửa/xem tài nguyên của người khác)
- [ ] Có test validator dùng `[Theory]`
- [ ] Mỗi test có DB riêng — chạy `dotnet test` nhiều lần đều ra cùng kết quả
- [ ] Đổi thứ tự test không làm test nào đỏ

## Cách tự kiểm chứng test có thật sự bảo vệ

Test xanh không chứng minh test tốt. Làm bài kiểm tra ngược — **cố tình phá code rồi xem test có đỏ không**:

| Phá gì | Test nào phải đỏ |
|---|---|
| Xoá dòng trừ `wallet.Balance` trong checkout | Test checkout thành công |
| Đổi `PriceAtPurchased` thành lấy từ `Product.Price` lúc đọc | Test giá lịch sử |
| Xoá khối `catch`/`RollbackAsync` | Test rollback |
| Xoá kiểm tra quyền sở hữu trong cart | Test IDOR |
| Đổi `GreaterThan(0)` thành `GreaterThanOrEqualTo(0)` | Test validator |

Phá một chỗ mà **không** test nào đỏ → bạn thiếu test ở chỗ đó. Nhớ hoàn tác sau khi kiểm tra xong.

Đây là cách duy nhất để biết bộ test có giá trị thật, thay vì chỉ làm đẹp con số.

---

**Tiếp theo:** [12-frontend-setup.md](12-frontend-setup.md)
