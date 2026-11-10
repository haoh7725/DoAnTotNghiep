# KỊCH BẢN DEMO FULL LUỒNG – TẠO MỚI VÀ DUYỆT THỰC TẾ

Kịch bản này dùng để chứng minh nghiệp vụ từ đầu đến cuối, không chỉ mở dữ liệu mẫu để xem.

## 1. Tài khoản dùng trong kịch bản

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Quản trị viên | `admin` | `change_me` |
| Phòng QLKH | `qlkh` | `Huit@2026!` |
| Trưởng khoa CNTT | `truongkhoa` | `Huit@2026!` |
| Trưởng bộ môn CNPM | `tbm.cnpm` | `Huit@2026!` |
| Giảng viên Lê Thanh Sơn | `gv.lethanhson` | `Huit@2026!` |

## 2. Dữ liệu sẽ tạo trong buổi demo

- Năm học: `2027-2028`.
- Chỉ tiêu Khoa Công nghệ Thông tin: `1 bài báo`.
- Phân bổ Bộ môn Công nghệ phần mềm: `1 bài báo`.
- Phân công cho TS. Lê Thanh Sơn: `1 bài báo`.
- Nội dung cam kết: `Ứng dụng trí tuệ nhân tạo hỗ trợ quản lý nghiên cứu khoa học`.
- Sản phẩm: `Mô hình AI hỗ trợ quản lý tiến độ nghiên cứu khoa học`.
- Nơi công bố: `International Conference on ICT`.

> Dùng năm học mới để không làm thay đổi dữ liệu 2026–2027 đã khóa và đã có kết quả thống kê.

## 3. Bước 1 – Quản trị viên tạo năm học mới

Đăng nhập `admin` / `change_me`.

1. Vào **Quản trị dữ liệu nền**.
2. Chọn tab **Năm học** → **Thêm mới**.
3. Nhập:
   - Mã: `2027-2028`.
   - Ngày bắt đầu: `01/09/2027`.
   - Ngày kết thúc: `30/06/2028`.
4. Nhấn **Lưu**.
5. Đăng xuất.

Kết quả cần chỉ ra: năm học 2027–2028 xuất hiện trong danh sách và có thể được chọn ở các màn hình nghiệp vụ.

## 4. Bước 2 – Giảng viên lập và đăng ký kế hoạch đầu năm

Đăng nhập `gv.lethanhson` / `Huit@2026!`.

1. Vào **Kế hoạch & Tiến độ**.
2. Chọn năm học **2027–2028**.
3. Nhấn **Tạo kế hoạch**, nhập:
   - Số sản phẩm cam kết: `1`.
   - Ngày lập: ngày hiện tại.
   - Hạn hoàn thành: `31/05/2028`.
4. Trong **Thêm nội dung nghiên cứu**, nhập:
   - Loại sản phẩm: **Bài báo**.
   - Tên nội dung: `Ứng dụng trí tuệ nhân tạo hỗ trợ quản lý nghiên cứu khoa học`.
   - Số lượng cam kết: `1`.
   - Hạn hoàn thành: `31/05/2028`.
5. Nhấn **Thêm nội dung**.
6. Nhấn **Đăng ký kế hoạch** và xác nhận.
7. Đăng xuất.

Kết quả cần nói: đây là cam kết cá nhân đầu năm. Cam kết bài báo là căn cứ để Trưởng bộ môn được phép giao tối đa một bài cho giảng viên.

## 5. Bước 3 – Phòng QLKH tạo chỉ tiêu cho khoa

Đăng nhập `qlkh` / `Huit@2026!`.

1. Vào **Chỉ tiêu và phân bổ NCKH**.
2. Nhấn **Thêm chỉ tiêu**.
3. Nhập:
   - Khoa: **Khoa Công nghệ Thông tin**.
   - Năm học: **2027–2028**.
   - Tạp chí/hội nghị liên kết: **International Conference on ICT**.
   - Số bài giao: `1`.
   - Hạn hoàn thành: `30/06/2028`.
4. Nhấn **Lưu chỉ tiêu**.
5. Mở **Thông báo** để giải thích hệ thống đã gửi thông báo cho Trưởng khoa.
6. Đăng xuất.

Kết quả: chỉ tiêu ở trạng thái **Chờ tiếp nhận**. Phòng QLKH chưa được tự chia xuống bộ môn.

## 6. Bước 4 – Trưởng khoa tiếp nhận và phân bổ xuống bộ môn

Đăng nhập `truongkhoa` / `Huit@2026!`.

1. Mở **Thông báo**, đọc thông báo được Phòng QLKH giao một bài.
2. Vào **Chỉ tiêu và phân bổ NCKH**.
3. Chọn chỉ tiêu Khoa CNTT – năm học 2027–2028.
4. Nhấn **Tiếp nhận chỉ tiêu**.
5. Nhấn **Mở phân bổ cho bộ môn**.
6. Chọn tab **Phân bổ theo bộ môn** → **Phân bổ cho bộ môn**.
7. Nhập:
   - Bộ môn: **Bộ môn Công nghệ phần mềm**.
   - Số bài phân bổ: `1`.
8. Nhấn **Lưu phân bổ**.
9. Quay lại **Tổng quan tiến độ** và nhấn **Khóa và chuyển cho bộ môn**.
10. Đăng xuất.

Kết quả cần chỉ ra:

- Tổng chỉ tiêu khoa: 1 bài.
- Đã phân bổ: 1 bài.
- Còn chưa phân bổ: 0 bài.
- Sau khi khóa, Trưởng khoa không sửa số phân bổ được nữa.
- Trưởng bộ môn chỉ nhận quyền phân công giảng viên sau bước khóa.

## 7. Bước 5 – Trưởng bộ môn giao chỉ tiêu cho giảng viên

Đăng nhập `tbm.cnpm` / `Huit@2026!`.

1. Mở **Thông báo**, đọc thông báo bộ môn được nhận một bài.
2. Vào **Chỉ tiêu và phân bổ NCKH**.
3. Mở chỉ tiêu năm học 2027–2028.
4. Chọn **Chỉ tiêu bộ môn được nhận** → **Xem / phân công**.
5. Trong bảng căn cứ, tìm **TS. Lê Thanh Sơn** và chỉ ra:
   - Cam kết bài báo: 1.
   - Đã giao: 0.
   - Có thể giao thêm: 1.
6. Tại **Giao chỉ tiêu chính thức**:
   - Giảng viên: **TS. Lê Thanh Sơn**.
   - Số bài giao: `1`.
7. Nhấn **Giao chỉ tiêu**.
8. Đăng xuất.

Kết quả: bộ môn đã giao đủ 1/1 bài; giảng viên không thể được giao vượt cam kết.

## 8. Bước 6 – Giảng viên tiếp nhận và tạo sản phẩm gắn với chỉ tiêu

Đăng nhập `gv.lethanhson` / `Huit@2026!`.

1. Mở **Thông báo** để xem thông báo được giao một bài.
2. Mở **Chỉ tiêu của tôi**:
   - Được giao: 1.
   - Hoàn thành: 0.
   - Còn lại: 1.
3. Vào **Sản phẩm khoa học** → **Thêm sản phẩm**.
4. Nhập:
   - Nội dung kế hoạch: `Ứng dụng trí tuệ nhân tạo hỗ trợ quản lý nghiên cứu khoa học`.
   - Chỉ tiêu được giao: chỉ tiêu Khoa CNTT 2027–2028.
   - Tạp chí/hội nghị: **International Conference on ICT**.
   - Tên sản phẩm: `Mô hình AI hỗ trợ quản lý tiến độ nghiên cứu khoa học`.
   - Trạng thái bài viết: **Đã xuất bản**.
   - Ngày công bố: `15/10/2027`.
   - Nơi công bố: `International Conference on ICT 2027`.
   - Chỉ số: `SCOPUS`.
   - Phân loại: `QUOC_TE`.
   - Mô tả: `Nghiên cứu xây dựng mô hình AI hỗ trợ theo dõi và cảnh báo tiến độ nghiên cứu khoa học.`
5. Nhấn **Lưu sản phẩm**.
6. Mở chi tiết sản phẩm, tải lên một file minh chứng và nhập mô tả `Bản toàn văn bài báo`.
7. Nhấn **Gửi duyệt**.
8. Đăng xuất.

Kết quả: sản phẩm chuyển từ **Nháp** sang **Đã nộp**, chờ Trưởng bộ môn. Tiến độ chỉ tiêu vẫn là 0% vì chưa duyệt hoàn tất.

## 9. Bước 7 – Duyệt sản phẩm qua đầy đủ các cấp

### 9.1. Trưởng bộ môn duyệt

Đăng nhập `tbm.cnpm` / `Huit@2026!`.

1. Vào **Sản phẩm chờ duyệt**.
2. Mở `Mô hình AI hỗ trợ quản lý tiến độ nghiên cứu khoa học`.
3. Kiểm tra thông tin và minh chứng.
4. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
5. Ghi chú: `Sản phẩm phù hợp kế hoạch và chỉ tiêu của bộ môn.`
6. Xác nhận và đăng xuất.

### 9.2. Trưởng khoa duyệt

Đăng nhập `truongkhoa` / `Huit@2026!`.

1. Vào **Sản phẩm chờ duyệt** và mở sản phẩm trên.
2. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
3. Ghi chú: `Đồng ý chuyển Phòng QLKH thẩm định.`
4. Xác nhận và đăng xuất.

### 9.3. Phòng QLKH thẩm định

Đăng nhập `qlkh` / `Huit@2026!`.

1. Vào **Sản phẩm chờ duyệt** và mở sản phẩm.
2. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
3. Ghi chú: `Thông tin và minh chứng hợp lệ.`
4. Xác nhận.

Kết quả: sản phẩm chuyển sang bước Phòng QLKH công nhận cuối cùng.

### 9.4. Phòng QLKH công nhận kết quả

1. Mở lại sản phẩm vừa thẩm định.
2. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
3. Nhập điểm quy đổi nếu giao diện yêu cầu: `20`.
4. Ghi chú: `Công nhận sản phẩm khoa học năm học 2027-2028.`
5. Xác nhận.

Kết quả cuối cùng: sản phẩm ở trạng thái **Đã duyệt** và lịch sử thể hiện đủ các cấp xét duyệt.

## 10. Bước 8 – Kiểm chứng kết quả sau duyệt

### Với giảng viên

Đăng nhập lại `gv.lethanhson`.

1. Mở **Chỉ tiêu của tôi**.
2. Kiểm tra:
   - Được giao: 1 bài.
   - Đã hoàn thành: 1 bài.
   - Còn lại: 0 bài.
   - Tiến độ: 100%.
3. Mở **Kế hoạch & Tiến độ**: nội dung cam kết đã hoàn thành 1/1.
4. Mở sản phẩm để xem trạng thái Đã duyệt và toàn bộ lịch sử.

### Với Trưởng khoa

Đăng nhập `truongkhoa`.

1. Mở chỉ tiêu 2027–2028.
2. Xem **Tổng quan tiến độ**.
3. Kết quả toàn khoa và Bộ môn Công nghệ phần mềm đều là 1/1, đạt 100%.

## 11. Câu kết luận khi báo cáo

> Kịch bản đã chứng minh hai luồng gặp nhau: chỉ tiêu được giao từ Phòng QLKH xuống khoa, bộ môn và giảng viên; kế hoạch và sản phẩm được khai báo từ giảng viên đi lên qua các cấp xét duyệt. Chỉ sản phẩm bài báo đã gắn đúng chỉ tiêu, đã xuất bản và được duyệt hoàn tất mới làm tăng tỷ lệ hoàn thành.

## 12. Thứ tự rút gọn để demo trong 12–15 phút

1. Admin tạo năm học – 1 phút.
2. Giảng viên tạo và đăng ký kế hoạch – 2 phút.
3. QLKH tạo chỉ tiêu – 1 phút.
4. Trưởng khoa tiếp nhận, phân bổ và khóa – 2 phút.
5. Trưởng bộ môn giao giảng viên – 1 phút.
6. Giảng viên tạo sản phẩm, tải minh chứng và gửi duyệt – 2 phút.
7. Trưởng bộ môn, Trưởng khoa, QLKH duyệt – 3 phút.
8. Kiểm tra tiến độ chuyển từ 0% lên 100% – 1 phút.
