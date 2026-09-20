# 14 — Frontend: màn hình Admin

**3 giờ** · Phần Admin của requirement §11.

## Mục tiêu

3 màn hình: quản lý sản phẩm, sửa giá, nhập kho từ file. Đủ 4 trạng thái như bên Customer.

## Các bước

### 1. Quản lý sản phẩm (60 phút)

Bảng sản phẩm với các cột: mã, tên, danh mục, giá, tồn kho, và các nút thao tác.

Cần có: nút Thêm mới, nút Sửa từng dòng, nút Xoá từng dòng, phân trang, tìm kiếm.

Form thêm/sửa nên là modal hoặc drawer, tránh chuyển trang qua lại. Các trường: tên, mã, giá, số lượng, danh mục (dropdown lấy từ `GET /api/categories`).

Validate ngay ở form trước khi gửi: tên không rỗng, giá > 0, số lượng ≥ 0.

> **Validate ở frontend không thay thế validate ở backend.** Nó chỉ để phản hồi nhanh cho người dùng. Backend đã có FluentValidation từ task 03, và đó mới là chốt chặn thật.
>
> Kiểm chứng: dùng Postman gửi thẳng `price: -5` lên API → vẫn phải `400`.

Nút Xoá cần hộp xác nhận. Nhắc người dùng đây là xoá mềm — sản phẩm sẽ ẩn khỏi cửa hàng nhưng lịch sử đơn vẫn giữ.

### 2. Sửa giá nhanh (30 phút)

Requirement §2 tách riêng *"Update product prices"* khỏi *"Manage product information"* — nên làm một lối đi riêng cho nó.

Gợi ý: ô giá trong bảng bấm vào sửa được tại chỗ, Enter để lưu, Escape để huỷ. Gọi `PATCH /api/products/{id}/price`.

Nhanh hơn nhiều so với mở cả form sửa chỉ để đổi một con số — và đó chính là lý do bạn tạo endpoint `PATCH` riêng ở task 03.

Sau khi lưu xong nên có dấu hiệu thị giác ngắn (dòng nhấp nháy xanh) để xác nhận đã lưu.

### 3. Trang nhập kho (60 phút)

Đây là màn hình thể hiện rõ nhất giá trị của xử lý nền.

Hai phần:

**Phần upload** — vùng chọn file (hoặc kéo thả), chỉ nhận `.csv` và `.xlsx`, nút Tải lên.

**Phần danh sách job** — bảng: tên file gốc, trạng thái, thời gian tạo, thời gian gửi mail, thông báo lỗi.

Trạng thái nên hiện dạng nhãn màu:

| Trạng thái | Màu | Ý nghĩa |
|---|---|---|
| `STORED` / `ACCEPTED` | Xám | Đã nhận, đang chờ |
| `PROCESSING` | Xanh dương, có hiệu ứng động | Đang xử lý |
| `DONE` | Xanh lá | Xong |
| `FAILED` | Đỏ | Lỗi, xem `errorMessage` |

**Điểm quan trọng — luồng bất đồng bộ trên giao diện:**

Upload xong API trả `202` **ngay**, nhưng job chưa xử lý xong. Giao diện phải thể hiện đúng điều đó:

1. Bấm Tải lên → spinner trên nút
2. Nhận `202` → toast "Đã nhận file, đang xử lý"
3. Job mới xuất hiện trong bảng với trạng thái chờ
4. **Tự động làm mới** danh sách job mỗi 2–3 giây
5. Trạng thái chuyển `PROCESSING` → `DONE` ngay trước mắt người dùng
6. Xong thì dừng tự làm mới

> **Đừng bắt người dùng F5 để xem kết quả.** Toàn bộ điểm của kiến trúc bất đồng bộ là người dùng không phải đợi — nhưng họ vẫn cần **thấy** tiến độ.
>
> Cách đơn giản nhất: `setInterval` gọi lại `GET /api/inventory/jobs`. Nhớ `clearInterval` khi rời trang, nếu không component đã unmount vẫn gọi API mãi — đây là rò rỉ bộ nhớ kinh điển trong React.
>
> Tinh tế hơn: chỉ poll khi còn job chưa kết thúc. Mọi job đều `DONE`/`FAILED` thì dừng.

### 4. Xử lý lỗi upload (20 phút)

Các trường hợp cần hiện thông báo rõ ràng:

| Tình huống | Hiện gì |
|---|---|
| Chưa chọn file, bấm Tải lên | "Vui lòng chọn file" — chặn ngay ở client |
| Chọn file `.txt` | Chặn ở client bằng thuộc tính `accept`, backend cũng chặn |
| File quá lớn | Backend trả lỗi kích thước, hiện lại cho người dùng |
| Job `FAILED` | Hiện `errorMessage` trong bảng, cho bấm xem đầy đủ |

Cột `errorMessage` có thể rất dài. Cắt ngắn trong bảng, bấm vào mở popup xem đầy đủ.

### 5. Thanh điều hướng của Admin (10 phút)

Admin không có giỏ hàng, không có đơn hàng, không có ví. Menu chỉ gồm: Sản phẩm, Nhập kho, Đăng xuất.

Hiện menu giỏ hàng cho Admin là bug về trải nghiệm — bấm vào sẽ nhận `403`.

## AC

- [ ] Bảng sản phẩm hiện đủ dữ liệu, phân trang và tìm kiếm chạy
- [ ] Tạo sản phẩm mới → xuất hiện trong bảng
- [ ] Sửa sản phẩm → dữ liệu cập nhật
- [ ] Xoá sản phẩm → có xác nhận, xong thì biến mất khỏi bảng
- [ ] Sửa giá tại chỗ → lưu được, bảng cập nhật
- [ ] Form validate: giá âm bị chặn ngay ở client
- [ ] Upload file → `202`, toast hiện ngay
- [ ] ★ Bảng job **tự cập nhật** trạng thái mà không cần F5
- [ ] Trạng thái đổi `ACCEPTED` → `PROCESSING` → `DONE` quan sát được
- [ ] Job `FAILED` hiện nhãn đỏ và thông báo lỗi
- [ ] Rời trang nhập kho → không còn request nào chạy ngầm
- [ ] Menu Admin không hiện Giỏ hàng / Đơn hàng
- [ ] Đủ 4 trạng thái ở cả 2 màn hình

## Cách test

### 1. CRUD sản phẩm

Tạo, sửa, xoá một sản phẩm thử. Sau khi xoá, chuyển sang tài khoản CUSTOMER xem `/products` → sản phẩm đó không còn.

Kiểm tra trong DB: row vẫn còn với `IsDeleted = 1`. Đó là xoá mềm hoạt động đúng.

### 2. Sửa giá tại chỗ

Đổi giá một sản phẩm. Mở cửa sổ ẩn danh đăng nhập CUSTOMER, thêm sản phẩm đó vào giỏ → giá trong giỏ phải là giá mới.

Đây là chuỗi §3 → §6 chạy xuyên suốt từ giao diện admin tới giỏ khách hàng.

### ★ 3. Bảng job tự cập nhật

Chuẩn bị một file to hơn để job chạy lâu hơn, dễ quan sát:

```bash
### Tạo file 500 dòng
{ echo "Code,Type,Price,Quantity"; for i in $(seq 1 500); do echo "MS-001,Phụ kiện,55000$((i % 10)),$((i + 100))"; done; } > /tmp/to.csv
```

Upload qua giao diện, **không rời trang**, quan sát cột trạng thái.

Phải thấy nhãn chuyển màu theo thời gian thực. Nếu phải F5 mới thấy đổi → thiếu cơ chế tự làm mới.

### 4. Không rò rỉ interval

Mở tab Network, vào trang Nhập kho → thấy request lặp mỗi vài giây.

Chuyển sang trang Sản phẩm → request lặp phải **dừng hẳn**.

Còn chạy nghĩa là thiếu `clearInterval` trong cleanup của `useEffect`. Để lâu sẽ chồng nhiều interval, tab ngốn CPU.

### 5. Job thất bại

```bash
cat > /tmp/hong.csv <<'EOF'
Code,Type,Price
MS-001,Phụ kiện,khong-phai-so
EOF
```

Upload → bảng hiện nhãn đỏ `FAILED` kèm thông báo lỗi. Bấm vào xem được nội dung đầy đủ.

### 6. Kiểm chứng validate frontend không phải bảo mật

Form chặn giá âm. Giờ gửi thẳng bằng curl:

```bash
curl -i -X PATCH http://localhost:5118/api/products/<ID_SP>/price \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -H "Content-Type: application/json" \
  -d '{"price": -100}'
```

→ phải `400` từ FluentValidation. Nếu `204` thì backend đang thiếu validate, và form của bạn là phòng tuyến duy nhất — không chấp nhận được.

---

**Tiếp theo:** [15-polish.md](15-polish.md)
