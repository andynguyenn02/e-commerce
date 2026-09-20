# 08 — Upload file tồn kho

**2 giờ** · Đáp ứng requirement §5. Phần xử lý file nằm ở task 09.

## Mục tiêu

Admin upload file CSV/Excel. Server **lưu file rồi trả về ngay** — không xử lý trong HTTP request. Trả `202 Accepted` + `jobId` để tra cứu tiến độ.

## Nguyên tắc

Requirement §4: *"The uploaded file must not be fully processed during the HTTP request."*

Task này chỉ làm 3 việc:
1. Nhận file, kiểm tra định dạng
2. Lưu vào `Uploads/` với tên **không thể trùng**
3. Tạo `InventoryJobEntity`, trả `202`

Đọc file và cập nhật sản phẩm là của task 09.

## Các bước

### 1. Khai báo interface lưu trữ (20 phút)

Application không được biết `System.IO` — requirement §14: *"Business logic should not depend directly on local file-system implementation."*

Tạo `IFileStorage` ở `Application/Common/Interfaces/` với 4 khả năng:

| Việc | Nhận vào | Trả về |
|---|---|---|
| Lưu vào Uploads | `Stream` + tên file gốc + token | Tên file đã lưu (duy nhất) |
| Chuyển sang Archive | Tên file đã lưu | — |
| Chuyển sang Failed | Tên file đã lưu | — |
| Mở file để đọc | Tên file đã lưu | `Stream` |

Ba thư mục chứ không phải hai: requirement §5 nói *"Failed files must not be treated as successfully archived files."* File hỏng phải đi chỗ khác.

### 2. Cài đặt ở Infrastructure (30 phút)

Tạo `LocalFileStorage` ở `Infrastructure/Storage/`.

Constructor đọc đường dẫn gốc từ `IConfiguration`, ghép ra 3 thư mục con, và **tạo sẵn cả 3** nếu chưa có.

> ### Điểm mấu chốt — tên file phải duy nhất
>
> Requirement §5: *"Files with the same original filename must not accidentally overwrite each other."*
>
> Hướng giải: tên lưu trữ = một `Guid` mới + phần mở rộng của file gốc. Hai admin cùng upload `tonkho.csv` thì thành 2 file GUID khác nhau.
>
> Tên gốc vẫn giữ trong `InventoryJobEntity.OriginalFileName` để hiển thị cho người dùng.
>
> Đây chính là lý do entity của bạn có **cả hai** trường `OriginalFileName` và `StoredFileName`. Giờ bạn biết vì sao.

Đăng ký DI dạng **Singleton** — class này không giữ state gì ngoài mấy chuỗi đường dẫn.

Thêm thư mục lưu trữ vào `.gitignore`.

### 3. Command upload (30 phút)

Command nhận `Stream` nội dung + tên file gốc, trả về `Guid` của job.

> **Truyền `Stream`, không truyền `IFormFile`.** `IFormFile` là type của ASP.NET Core — đưa vào Application là kéo HTTP vào tầng nghiệp vụ. Controller chịu trách nhiệm mở `IFormFile` thành `Stream`.

Handler theo thứ tự:

1. Kiểm tra phần mở rộng nằm trong danh sách cho phép (`.csv`, `.xlsx`) — không thì ném lỗi xung đột
2. Gọi `IFileStorage` lưu file, nhận về tên đã lưu
3. Tạo `InventoryJobEntity` với `Status = STORED`, lưu DB
4. Đẩy `job.Id` vào hàng đợi (task 09 — chưa làm thì bỏ qua bước này)
5. Đổi `Status = ACCEPTED`, lưu lần nữa

> **Thứ tự bước 3–5 quan trọng.** Job phải tồn tại trong DB **trước** khi đẩy vào hàng đợi. Ngược lại thì worker có thể nhặt được jobId trước khi row được commit → không tìm thấy job, lỗi khó hiểu.

Chưa làm task 09 thì để trạng thái dừng ở `STORED`, quay lại bổ sung sau.

### 4. Query danh sách job (20 phút)

Query không tham số. Sắp giảm dần theo thời gian tạo.

DTO trả về: id job, tên file **gốc**, trạng thái (dạng chuỗi), thời gian tạo, thời điểm gửi mail, thông báo lỗi.

Hai lưu ý:

- **Không lọc theo user.** Requirement §2 cho Admin *"View inventory-related information"* — mọi Admin xem được mọi job.
- **Không trả `StoredFileName` ra ngoài.** Đó là chi tiết nội bộ; lộ ra là mở đường dò cấu trúc file hệ thống.

### 5. Controller (20 phút)

Tạo `InventoryController`, `[Authorize(Roles = "ADMIN")]` ở cấp class.

| Method | Route | Việc |
|---|---|---|
| POST | `/api/inventory/upload` | Nhận file (multipart) |
| GET | `/api/inventory/jobs` | Danh sách job |

Endpoint upload:
- Nhận tham số kiểu `IFormFile`
- Chặn trường hợp file null hoặc rỗng → `400`
- Mở stream từ `IFormFile`, gửi command
- Trả **`202 Accepted`**, không phải `200`

Ba điểm:

- **`202` là đúng ngữ nghĩa REST** cho tác vụ bất đồng bộ: "đã nhận, sẽ xử lý sau". `200` ngụ ý đã xong.
- Gắn giới hạn kích thước request (tra `[RequestSizeLimit]`). Mặc định ASP.NET Core cho ~28 MB; đặt rõ ràng vẫn tốt hơn.
- **`IFormFile` chỉ xuất hiện ở file controller này.** Không lọt xuống Application.

### 6. Cấu hình đường dẫn (5 phút)

Thêm mục cấu hình cho thư mục gốc lưu trữ vào `appsettings.json`. Đường dẫn tương đối tính từ thư mục chạy app.

## AC

- [ ] Admin upload `.csv` → `202` + `jobId`
- [ ] File xuất hiện trong `Uploads/` với tên GUID, **không phải** tên gốc
- [ ] Bảng `InventoryJobs` có dòng mới: `OriginalFileName` = tên gốc, `StoredFileName` = tên GUID
- [ ] Upload **2 file trùng tên gốc** → 2 file riêng biệt, không đè nhau
- [ ] Upload `.txt` → `409`
- [ ] Không chọn file → `400`
- [ ] Customer upload → `403`
- [ ] Không token → `401`
- [ ] `GET /api/inventory/jobs` → danh sách, mới nhất trước
- [ ] Response **không** chứa `storedFileName`

## Test API

Tạo file thử:

```bash
cat > /tmp/tonkho.csv <<'EOF'
Code,Type,Price,Quantity
MS-001,Phụ kiện máy tính,550000,120
KB-001,Phụ kiện máy tính,1350000,80
EOF
```

```bash
### 1. Admin upload
curl -i -X POST http://localhost:5118/api/inventory/upload \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -F "file=@/tmp/tonkho.csv"
```
→ `202` + `{"jobId":"...","status":"ACCEPTED"}`

```bash
### 2. Kiểm tra tên file đã lưu
ls -la ecommerce.Api/Storage/Uploads/
```
→ phải thấy `0199....csv`, **không** phải `tonkho.csv`

```bash
### 3. Upload lại CÙNG tên file → phải ra file thứ 2
curl -i -X POST http://localhost:5118/api/inventory/upload \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -F "file=@/tmp/tonkho.csv"

ls -la ecommerce.Api/Storage/Uploads/
```
→ **2 file** GUID khác nhau. Chỉ có 1 nghĩa là đang đè file.

```bash
### 4. Sai định dạng → phải 409
echo "abc" > /tmp/sai.txt
curl -i -X POST http://localhost:5118/api/inventory/upload \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -F "file=@/tmp/sai.txt"
```

```bash
### 5. Customer upload → phải 403
curl -i -X POST http://localhost:5118/api/inventory/upload \
  -H "Authorization: Bearer <TOKEN_CUSTOMER>" \
  -F "file=@/tmp/tonkho.csv"
```

```bash
### 6. Không token → phải 401
curl -i -X POST http://localhost:5118/api/inventory/upload \
  -F "file=@/tmp/tonkho.csv"
```

```http
### 7. Danh sách job
GET http://localhost:5118/api/inventory/jobs
Authorization: Bearer <TOKEN_ADMIN>
```
→ 2 dòng, `originalFileName: "tonkho.csv"` ở cả hai, **không có** trường `storedFileName`

---

**Tiếp theo:** [09-background-worker.md](09-background-worker.md)
