# 13 — Frontend: màn hình Customer

**4 giờ** (3h ngày 3 + 1h ngày 4) · Phần chính của requirement §11.

## Mục tiêu

4 màn hình: danh sách sản phẩm, giỏ hàng, thanh toán, lịch sử đơn. Mỗi màn hình xử lý đủ 4 trạng thái.

## Các bước

### 1. Bố cục chung và thanh điều hướng (30 phút)

Một layout bọc mọi trang, gồm:
- Thanh trên: logo, link Sản phẩm / Giỏ hàng / Đơn hàng
- Số dư ví hiện ở góc phải (chỉ với CUSTOMER)
- Số lượng item trong giỏ hiện trên icon giỏ
- Nút đăng xuất

Menu hiện theo role: CUSTOMER thấy Giỏ hàng/Đơn hàng, ADMIN thấy Quản lý sản phẩm/Nhập kho.

Số dư ví và số item giỏ đọc từ Redux — nhờ vậy sau khi checkout chúng tự cập nhật, không phải F5.

### 2. Danh sách sản phẩm (45 phút)

Màn hình công khai, không cần đăng nhập.

Cần có: ô tìm kiếm, bộ lọc danh mục, lưới sản phẩm, điều khiển phân trang.

Mỗi thẻ sản phẩm: tên, mã, giá, tồn kho, ô nhập số lượng, nút Thêm vào giỏ.

Bốn trạng thái phải xử lý:

| Trạng thái | Hiện gì |
|---|---|
| `idle` / `loading` | Skeleton hoặc spinner |
| `success` có dữ liệu | Lưới sản phẩm |
| `success` rỗng | "Không tìm thấy sản phẩm nào" |
| `error` | Thông báo lỗi + nút Thử lại |

Hai lưu ý:

- **Chỉ ADMIN mới thấy nút sửa giá.** CUSTOMER thấy nút Thêm vào giỏ. Người chưa đăng nhập thấy nút Thêm nhưng bấm vào thì chuyển sang `/login`.
- **Ô tìm kiếm phải debounce** khoảng 300–500ms. Không debounce thì gõ 10 ký tự bắn 10 request, và kết quả về không đúng thứ tự gõ.

Trạng thái phân trang, từ khoá, danh mục đang lọc nên để trong slice `products` chứ không trong state của component — để quay lại trang không mất bộ lọc.

### 3. Giỏ hàng (50 phút)

Bảng các item: tên sản phẩm, đơn giá, ô chỉnh số lượng, thành tiền, nút xoá. Dưới cùng là tổng tiền và nút Thanh toán.

Xử lý:
- Giỏ rỗng → thông báo + link về trang sản phẩm, **ẩn** nút Thanh toán
- Đổi số lượng → gọi API rồi nạp lại giỏ
- Số lượng vượt tồn kho → API trả `409`, hiện thông báo từ backend

> **Toàn bộ số tiền lấy từ response của API, không tính ở frontend.** Bạn đã tính `lineTotal` và `total` ở backend từ task 04. Frontend chỉ hiển thị.
>
> Tính lại ở frontend sinh ra hai nguồn sự thật, và sớm muộn chúng lệch nhau. Tệ hơn: làm lu mờ ranh giới mà requirement §6 cố tình dựng lên.

Nên có xác nhận trước khi xoá item, hoặc nút hoàn tác. Bấm nhầm nút xoá là chuyện thường.

### 4. Luồng thanh toán (60 phút)

**Đây là phần requirement §11 mô tả chi tiết nhất — làm đúng từng bước.**

Luồng yêu cầu:

```
Bấm Thanh toán
      ↓
Hiện spinner, KHOÁ nút
      ↓
Đợi API
      ↓
Thành công:                    Thất bại:
  ẩn spinner                     ẩn spinner
  toast thành công               hiện lỗi
  cập nhật số dư ví              GIỮ NGUYÊN giỏ hàng
  dọn giỏ
```

Điểm quan trọng nhất: **thất bại thì giỏ phải còn nguyên.** Requirement viết rõ *"Preserve valid cart state"*. Đừng dọn giỏ ở phía frontend trước khi API trả về thành công.

Khoá nút khi đang gửi là bắt buộc. Không khoá thì người dùng bấm 3 lần sẽ tạo 3 đơn — và bài này đã bỏ qua xử lý đua tranh, nên backend sẽ không cứu bạn.

Sau khi thành công: cập nhật số dư ví trên thanh trên (dùng `remainingBalance` API trả về, không cần gọi lại), dọn slice giỏ, chuyển sang trang chi tiết đơn hoặc danh sách đơn.

Thông báo lỗi nên lấy nội dung từ backend (`error` trong response) chứ không hiện chuỗi chung chung. Backend đã viết sẵn "Số dư không đủ. Cần X, ví còn Y" — hiện đúng câu đó hữu ích hơn nhiều so với "Đã có lỗi xảy ra".

### 5. Lịch sử đơn hàng (45 phút)

Hai màn:

**Danh sách** — bảng gồm mã đơn, ngày mua, số loại sản phẩm, tổng tiền, link xem chi tiết. Rỗng thì hiện "Bạn chưa có đơn hàng nào".

**Chi tiết** — thông tin đơn, bảng sản phẩm với giá đã trả, tổng tiền.

> Ở màn chi tiết, nhãn cột nên ghi rõ **"Giá đã trả"** chứ không phải "Giá". Nó có thể khác giá hiện tại của sản phẩm, và người dùng cần hiểu ngay vì sao.
>
> Đây là chỗ requirement §9 hiện ra trên giao diện. Một dòng chữ nhỏ nhưng thể hiện bạn hiểu vấn đề.

### 6. Màn hình ví (20 phút)

Có thể gộp vào trang đơn hàng hoặc tách riêng.

Hiện số dư to rõ, dưới là bảng lịch sử giao dịch: ngày, mã đơn liên quan, số tiền trừ.

Mã đơn nên là link sang chi tiết đơn.

### 7. Thông báo toast (20 phút)

Cần một cơ chế toast dùng chung cho: thêm giỏ thành công, thanh toán thành công, các lỗi.

Dùng thư viện có sẵn (`react-hot-toast`, `sonner`) — tự viết tốn 30 phút cho thứ không phải trọng tâm bài tập.

## AC

- [ ] Danh sách sản phẩm hiện đúng, phân trang bấm được
- [ ] Tìm kiếm có debounce (gõ nhanh không bắn nhiều request — kiểm tra tab Network)
- [ ] Lọc theo danh mục hoạt động
- [ ] Đang tải → hiện spinner/skeleton
- [ ] Không có kết quả → hiện thông báo rỗng, không phải trang trắng
- [ ] API lỗi → hiện lỗi + nút thử lại
- [ ] Thêm vào giỏ → toast + số trên icon giỏ tăng
- [ ] Giỏ hiển thị đúng đơn giá, thành tiền, tổng tiền — **khớp với API**
- [ ] Sửa số lượng → tổng cập nhật
- [ ] Giỏ rỗng → ẩn nút Thanh toán
- [ ] ★ Bấm Thanh toán → spinner hiện, nút bị khoá
- [ ] ★ Thanh toán thành công → toast, số dư ví trên thanh trên đổi, giỏ trống
- [ ] ★ Thanh toán thất bại → **giỏ vẫn còn nguyên**, hiện đúng thông báo lỗi từ backend
- [ ] Bấm Thanh toán nhiều lần liên tiếp → chỉ tạo **1** đơn
- [ ] Lịch sử đơn hiện đúng, mới nhất trước
- [ ] Chi tiết đơn hiện "Giá đã trả"
- [ ] ★ Admin đổi giá → xem lại đơn cũ, giá **không đổi**

## Cách test

### 1. Debounce

Mở tab Network, lọc XHR. Gõ nhanh "chuot khong day" vào ô tìm kiếm.

→ chỉ được có **1–2** request, không phải 15.

### 2. Bốn trạng thái

| Cách tạo tình huống | Kỳ vọng |
|---|---|
| Network → Throttling → Slow 3G, rồi tải trang | Thấy rõ spinner |
| Tìm từ khoá vô nghĩa `zzzzz` | "Không tìm thấy sản phẩm nào" |
| Tắt backend rồi tải trang | Thông báo lỗi + nút thử lại |
| Bật lại backend, bấm thử lại | Dữ liệu hiện ra |

### ★ 3. Luồng thanh toán thành công

1. Ghi lại số dư ví trên thanh trên
2. Thêm vài sản phẩm vào giỏ
3. Bật Network Throttling → Slow 3G (để kịp nhìn spinner)
4. Bấm Thanh toán

Phải quan sát được: spinner hiện → nút bị khoá không bấm lại được → toast thành công → số dư giảm đúng → giỏ trống.

### ★ 4. Luồng thanh toán thất bại — bài test quan trọng nhất

1. Đăng nhập ADMIN ở cửa sổ ẩn danh, đặt giá sản phẩm lên `9999999`
2. Quay lại cửa sổ CUSTOMER, thêm sản phẩm đó vào giỏ
3. Bấm Thanh toán

Kỳ vọng:
- Hiện lỗi "Số dư không đủ. Cần ..., ví còn ..."
- **Giỏ hàng vẫn còn đủ item** — F5 kiểm tra lại, vẫn còn
- Số dư ví không đổi
- Nút Thanh toán mở khoá lại, bấm được lần nữa

Giỏ bị dọn ở đây là lỗi nặng: người dùng mất công chọn hàng mà không mua được gì.

### 5. Chống bấm nhiều lần

Bật Slow 3G, bấm Thanh toán rồi bấm liên tiếp 5 lần thật nhanh.

Vào `/orders` → phải chỉ có **1** đơn mới.

### ★ 6. Giá lịch sử trên giao diện

1. Mua một sản phẩm giá 100
2. Đăng nhập ADMIN đổi giá sản phẩm đó thành 500
3. Quay lại `/orders/:id` của đơn cũ

→ phải hiện `100`. Hiện `500` nghĩa là frontend đang lấy giá từ API sản phẩm thay vì từ dữ liệu đơn hàng.

---

**Tiếp theo:** [14-frontend-admin.md](14-frontend-admin.md)
