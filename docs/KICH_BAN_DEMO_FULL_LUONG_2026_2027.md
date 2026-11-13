# KỊCH BẢN DEMO FULL LUỒNG NĂM HỌC 2026–2027

Kịch bản này dùng để demo một luồng nghiệp vụ hoàn chỉnh trên đúng dữ liệu năm học **2026–2027**, từ lúc giảng viên chưa có cam kết đến khi sản phẩm được duyệt và chỉ tiêu đạt 100%.

## 1. Kết quả kiểm tra dữ liệu trước khi viết kịch bản

- Năm học `2026-2027` đã tồn tại, thời gian từ `01/08/2026` đến `31/07/2027`; không tạo lại năm học.
- Chỉ tiêu chính của Khoa Công nghệ Thông tin đã được khóa và có dữ liệu thực tế, vì vậy không sửa chỉ tiêu này.
- Các giảng viên hiện có trong năm 2026–2027 đều đã được giao đủ số bài báo cam kết. Riêng `gv.lethanhson` đã cam kết 1 và đã được giao 1 nên không thể dùng để nhận thêm chỉ tiêu.
- Bộ dữ liệu đã có sẵn tài khoản `demo.gv2026`, liên kết đúng Bộ môn Công nghệ phần mềm và có lý lịch khoa học đầy đủ, nhưng chưa có kế hoạch hay cam kết năm 2026–2027.
- Để demo mà không phá dữ liệu đang thống kê, kịch bản tạo một chỉ tiêu riêng gắn với **Hội nghị FAIR**.
- Quy trình duyệt hiện tại có 3 cấp: **Trưởng bộ môn → Trưởng khoa → Phòng QLKH duyệt cuối**. Phòng QLKH nhập điểm ở lần duyệt cuối, không duyệt hai lần.

> Có thể chạy ngay sau khi khởi tạo hoặc reset bộ dữ liệu mẫu. Không cần tạo thủ công tài khoản hay hồ sơ giảng viên.

## 2. Tài khoản dùng trong kịch bản

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Phòng QLKH | `qlkh` | `Huit@2026!` |
| Trưởng khoa CNTT | `truongkhoa` | `Huit@2026!` |
| Trưởng bộ môn CNPM | `tbm.cnpm` | `Huit@2026!` |
| Giảng viên demo có sẵn | `demo.gv2026` | `Huit@2026!` |

## 3. Dữ liệu tạo mới trong buổi demo

- Năm học: `2026-2027` (dùng dữ liệu có sẵn).
- Mã giảng viên: `DEMO2026`.
- Họ tên: `ThS. Nguyễn Hoài Phương`.
- Bộ môn: **Bộ môn Công nghệ phần mềm**.
- Chỉ tiêu riêng: `1 bài báo`, gắn **Hội nghị FAIR**.
- Nội dung kế hoạch: `Ứng dụng AI hỗ trợ giám sát tiến độ nghiên cứu khoa học`.
- Sản phẩm: `Mô hình AI cảnh báo chậm tiến độ nghiên cứu khoa học`.
- Điểm duyệt cuối: `20`.

## 4. Bước 1 – Kiểm tra tài khoản giảng viên có sẵn

Đăng nhập `demo.gv2026` / `Huit@2026!`.

1. Vào **Hồ sơ của tôi** và kiểm tra:
   - Mã giảng viên: `DEMO2026`.
   - Họ tên: `ThS. Nguyễn Hoài Phương`.
   - Bộ môn: **Bộ môn Công nghệ phần mềm**.
   - Email: `hoaiphuong.demo@huit.edu.vn`.
   - Học vị: **Thạc sĩ**.
   - Lý lịch có thông tin đào tạo, ngoại ngữ, quá trình công tác, đề tài và công bố khoa học.
2. Vào **Kế hoạch & Tiến độ**, chọn **2026–2027**.
3. Kiểm tra hệ thống chưa có kế hoạch/cam kết của giảng viên trong năm học này.

**Kết quả cần kiểm tra:** hồ sơ đã đầy đủ và liên kết đúng tài khoản, nhưng năm 2026–2027 vẫn bắt đầu ở trạng thái chưa cam kết.

## 5. Bước 2 – Giảng viên lập kế hoạch và cam kết bài báo

Đăng nhập `demo.gv2026` / `Huit@2026!`.

1. Vào **Kế hoạch & Tiến độ**.
2. Chọn năm học **2026–2027**.
3. Nhấn **Tạo kế hoạch**, nhập:
   - Số sản phẩm cam kết: `1`.
   - Ngày lập: `05/09/2026`.
   - Hạn hoàn thành: `31/05/2027`.
4. Trong **Thêm nội dung nghiên cứu**, nhập:
   - Loại sản phẩm: **Bài báo**.
   - Tên nội dung: `Ứng dụng AI hỗ trợ giám sát tiến độ nghiên cứu khoa học`.
   - Số lượng cam kết: `1`.
   - Hạn hoàn thành: `31/05/2027`.
5. Nhấn **Thêm nội dung**.
6. Kiểm tra tổng số lượng nội dung bằng số sản phẩm cam kết.
7. Nhấn **Đăng ký kế hoạch** và xác nhận.
8. Đăng xuất.

**Kết quả cần nói:** cam kết là kế hoạch tự nguyện của cá nhân. Vì cam kết 1 bài báo và chưa được giao bài nào nên giảng viên có khả năng nhận thêm 1 bài.

## 6. Bước 3 – Phòng QLKH tạo chỉ tiêu riêng cho khoa

Đăng nhập `qlkh` / `Huit@2026!`.

1. Vào **Chỉ tiêu và phân bổ NCKH** → **Thêm chỉ tiêu**.
2. Nhập:
   - Khoa: **Khoa Công nghệ Thông tin**.
   - Năm học: **2026–2027**.
   - Tạp chí/hội nghị liên kết: **Hội nghị FAIR**.
   - Số bài báo được giao: `1`.
   - Hạn hoàn thành: `30/06/2027`.
3. Nhấn **Lưu chỉ tiêu**.
4. Kiểm tra chỉ tiêu mới có trạng thái **Chờ Trưởng khoa tiếp nhận**.
5. Đăng xuất.

**Kết quả cần kiểm tra:** QLKH chỉ giao chỉ tiêu cho khoa, chưa được tự phân bổ xuống bộ môn. Trưởng khoa nhận được thông báo mới.

## 7. Bước 4 – Trưởng khoa tiếp nhận, phân bổ và khóa

Đăng nhập `truongkhoa` / `Huit@2026!`.

1. Mở **Thông báo**, kiểm tra thông báo về chỉ tiêu **Hội nghị FAIR**.
2. Vào **Chỉ tiêu và phân bổ NCKH**.
3. Tại dòng năm học 2026–2027 có `1 · Hội nghị FAIR`, nhấn **Mở chi tiết**.
4. Nhấn **Tiếp nhận chỉ tiêu**.
5. Nhấn **Mở phân bổ cho bộ môn**.
6. Chọn **Phân bổ theo bộ môn** → **Phân bổ cho bộ môn**.
7. Nhập:
   - Bộ môn: **Bộ môn Công nghệ phần mềm**.
   - Số bài phân bổ: `1`.
8. Nhấn **Lưu phân bổ**.
9. Kiểm tra:
   - Chỉ tiêu: 1 bài.
   - Đã phân bổ: 1 bài.
   - Còn chưa phân bổ: 0 bài.
10. Quay lại **Tổng quan tiến độ**.
11. Nhấn **Khóa và chuyển cho bộ môn**.
12. Đăng xuất.

**Kết quả cần kiểm tra:** trạng thái chuyển thành **Đã khóa · Bộ môn thực hiện**; sau khi khóa, Trưởng khoa chỉ theo dõi và không còn sửa phân bổ.

## 8. Bước 5 – Trưởng bộ môn giao chính thức cho giảng viên

Đăng nhập `tbm.cnpm` / `Huit@2026!`.

1. Mở **Thông báo**, kiểm tra thông báo bộ môn nhận 1 bài từ chỉ tiêu FAIR.
2. Vào **Chỉ tiêu và phân bổ NCKH**.
3. Mở dòng `1 · Hội nghị FAIR`.
4. Chọn **Chỉ tiêu bộ môn được nhận** → **Xem / phân công**.
5. Trong bảng căn cứ, tìm `DEMO2026 – ThS. Nguyễn Hoài Phương` và kiểm tra:
   - Cam kết bài báo: 1.
   - Đã giao: 0.
   - Có thể giao thêm: 1.
6. Trong **Giao chỉ tiêu chính thức**:
   - Giảng viên: `DEMO2026 – ThS. Nguyễn Hoài Phương`.
   - Số bài giao: `1`.
7. Nhấn **Giao chỉ tiêu**.
8. Kiểm tra bộ môn đã giao cá nhân `1/1` bài.
9. Đăng xuất.

**Kết quả cần nói:** hệ thống giới hạn số giao theo cả chỉ tiêu bộ môn và cam kết bài báo của giảng viên.

## 9. Bước 6 – Giảng viên xem chỉ tiêu và tạo sản phẩm

Đăng nhập `demo.gv2026` / `Huit@2026!`.

1. Mở **Thông báo**, kiểm tra thông báo được giao chính thức 1 bài.
2. Vào **Chỉ tiêu của tôi**, kiểm tra:
   - Được giao: 1.
   - Đã hoàn thành: 0.
   - Còn lại: 1.
   - Tiến độ: 0%.
3. Vào **Sản phẩm khoa học** → **Thêm sản phẩm**.
4. Chọn:
   - Nội dung kế hoạch: `Ứng dụng AI hỗ trợ giám sát tiến độ nghiên cứu khoa học`.
   - Chỉ tiêu bài báo: chỉ tiêu `Hội nghị FAIR` được giao.
5. Nhập:
   - Tên sản phẩm: `Mô hình AI cảnh báo chậm tiến độ nghiên cứu khoa học`.
   - Trạng thái bài viết: **Đã xuất bản**.
   - Ngày công bố: `10/11/2026`.
   - Nơi công bố: `Hội nghị FAIR 2026`.
   - Chỉ số: `SCOPUS`.
   - Phân loại: `QUOC_TE`.
   - Mô tả: `Mô hình hỗ trợ theo dõi, dự báo và cảnh báo nguy cơ chậm tiến độ nghiên cứu khoa học.`
6. Nhấn **Lưu sản phẩm**.
7. Mở chi tiết sản phẩm.
8. Trong **Minh chứng**, tải một file PDF thử nghiệm và nhập mô tả `Bản toàn văn bài báo`.
9. Nhấn **Tải lên**.
10. Nhấn **Gửi duyệt** và xác nhận.
11. Đăng xuất.

**Kết quả cần kiểm tra:** sản phẩm chuyển từ **Nháp** sang **Đã gửi**. Chỉ tiêu vẫn 0% vì sản phẩm chưa được duyệt hoàn tất.

## 10. Bước 7 – Duyệt sản phẩm qua đúng 3 cấp

### 10.1. Trưởng bộ môn duyệt

Đăng nhập `tbm.cnpm`.

1. Vào **Sản phẩm chờ duyệt**.
2. Mở `Mô hình AI cảnh báo chậm tiến độ nghiên cứu khoa học`.
3. Kiểm tra thông tin và minh chứng.
4. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
5. Ghi chú: `Sản phẩm phù hợp kế hoạch và chỉ tiêu của bộ môn.`
6. Nhấn **Xác nhận phê duyệt** rồi đăng xuất.

### 10.2. Trưởng khoa duyệt

Đăng nhập `truongkhoa`.

1. Vào **Sản phẩm chờ duyệt** và mở sản phẩm trên.
2. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
3. Ghi chú: `Đồng ý chuyển Phòng QLKH thẩm định và công nhận.`
4. Nhấn **Xác nhận phê duyệt** rồi đăng xuất.

### 10.3. Phòng QLKH duyệt cuối

Đăng nhập `qlkh`.

1. Vào **Sản phẩm chờ duyệt** và mở sản phẩm.
2. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
3. Nhập **Điểm quy đổi**: `20`.
4. Ghi chú: `Thông tin, minh chứng và điều kiện công bố hợp lệ.`
5. Nhấn **Xác nhận phê duyệt**.

**Kết quả cuối:** sản phẩm chuyển thẳng sang **Đã duyệt**. Không thực hiện thêm một lần duyệt QLKH thứ hai.

## 11. Bước 8 – Kiểm chứng kết quả sau duyệt

### 11.1. Tài khoản giảng viên

Đăng nhập `demo.gv2026`.

1. Vào **Chỉ tiêu của tôi**, kiểm tra:
   - Được giao: 1 bài.
   - Đã hoàn thành: 1 bài.
   - Còn lại: 0 bài.
   - Tiến độ: 100%.
2. Vào **Kế hoạch & Tiến độ**:
   - Nội dung cam kết ở trạng thái **Đã thực hiện**.
   - Sản phẩm thực tế: 1.
   - Đã hoàn thành: 1/1.
3. Vào **Sản phẩm khoa học**, mở sản phẩm và kiểm tra trạng thái **Đã duyệt**, điểm 20 và lịch sử đủ 3 cấp.

### 11.2. Tài khoản Trưởng khoa

Đăng nhập `truongkhoa`.

1. Mở chỉ tiêu `1 · Hội nghị FAIR`.
2. Chọn **Tổng quan tiến độ**.
3. Kiểm tra:
   - Toàn khoa: 1/1 bài, 100%.
   - Bộ môn Công nghệ phần mềm: 1/1 bài, 100%.

## 12. Các điểm không nên demo sai

- Không tạo lại năm học 2026–2027 vì năm học đã tồn tại.
- Không dùng `gv.lethanhson` nhận thêm bài vì đã giao đủ cam kết.
- Không chọn chỉ tiêu chung 27 bài để sửa; dùng chỉ tiêu riêng gắn Hội nghị FAIR.
- Không nói QLKH được phân bổ xuống bộ môn; quyền này thuộc Trưởng khoa.
- Không nói Trưởng khoa giao trực tiếp cho giảng viên; quyền này thuộc Trưởng bộ môn.
- Không duyệt QLKH hai lần. Phiên bản hiện tại gộp thẩm định và công nhận thành một lần duyệt cuối.
- Chỉ sản phẩm bài báo đã gắn chỉ tiêu, đã xuất bản, có ngày công bố và được duyệt hoàn tất mới làm tăng tiến độ.
- Báo cáo giờ/điểm thi đua chỉ đầy đủ sau khi tạo và chốt đánh giá cuối năm; việc duyệt sản phẩm mới chỉ hoàn thành chỉ tiêu bài báo.

## 13. Thứ tự demo rút gọn 15–18 phút

1. Kiểm tra hồ sơ giảng viên có sẵn và trạng thái chưa cam kết – 1 phút.
2. Giảng viên lập và đăng ký kế hoạch – 2 phút.
3. QLKH tạo chỉ tiêu FAIR – 1 phút.
4. Trưởng khoa tiếp nhận, phân bổ và khóa – 2 phút.
5. Trưởng bộ môn giao giảng viên – 1 phút.
6. Giảng viên tạo sản phẩm, tải minh chứng và gửi duyệt – 3 phút.
7. Trưởng bộ môn, Trưởng khoa và QLKH duyệt – 3 phút.
8. Kiểm tra tiến độ 0% → 100% – 2 phút.

## 14. Câu kết luận khi báo cáo

> Kịch bản năm học 2026–2027 chứng minh hai luồng nghiệp vụ gặp nhau. Chỉ tiêu được giao từ Phòng QLKH xuống khoa, bộ môn và giảng viên; kế hoạch, cam kết và sản phẩm được giảng viên khai báo rồi chuyển ngược qua các cấp xét duyệt. Hệ thống chỉ ghi nhận hoàn thành khi bài báo gắn đúng chỉ tiêu, đã xuất bản và được Phòng QLKH duyệt cuối.
