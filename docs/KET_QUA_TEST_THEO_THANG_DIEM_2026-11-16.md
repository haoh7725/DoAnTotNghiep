# Kết quả kiểm thử theo thang điểm đề cương ngày 16/11/2026

## 1. Phạm vi thực hiện

- Kịch bản nguồn: `docs/KICH_BAN_TEST_THEO_THANG_DIEM_DE_CUONG.md`.
- Web: `http://127.0.0.1:5173`.
- API: `http://localhost:8081`.
- Dữ liệu: PostgreSQL và MinIO trong Docker.
- Năm học: `2026-2027`.
- Các thao tác Web được thực hiện trên giao diện thật bằng trình duyệt và Playwright DOM.
- Không có thiết bị Android hoặc Android Emulator khả dụng trong môi trường kiểm thử.

## 2. Kết quả tổng hợp

| Trạng thái | Số ca |
|---|---:|
| Pass | 12 |
| Partial | 2 |
| Blocked | 4 |
| Fail chức năng chính | 0 |

## 3. Kết quả từng ca

| ID | Trạng thái | Kết quả thực tế |
|---|---|---|
| TC-01 | Pass | Đăng nhập sai bị từ chối; đăng nhập đúng chỉ hiện chức năng giảng viên; đăng xuất và tải lại quay về màn hình đăng nhập. |
| TC-02 | Pass | Admin tìm được `demo.gv2026`, quyền hiển thị `GIANG_VIEN / CA_NHAN`; giảng viên gọi API quản trị nhận HTTP 403. |
| TC-03 | Partial | Cả ba năm hiển thị 586 giờ. Năm 2026-2027 bị khóa đúng với thông báo có kết quả đã chốt; không có năm chưa khóa để thử trường hợp được phép sửa. |
| TC-04 | Pass | Hồ sơ cá nhân và bốn nhóm sản phẩm hiển thị đúng. API xuất hồ sơ Excel và PDF đều trả HTTP 200 với đúng MIME type. |
| TC-05 | Pass | Tạo kế hoạch, thêm một nội dung và đăng ký thành công; trạng thái chuyển `Đã đăng ký`, các lệnh sửa trực tiếp biến mất. |
| TC-06 | Pass | Tạo chỉ tiêu bài báo riêng gắn `Journal of Educational Data`, số lượng 1, hạn 30/06/2027; không xung đột chỉ tiêu chung. |
| TC-07 | Pass | Mở phân bổ, giao 1/1 cho CNPM, khóa thành công; trước khi đủ, nút khóa bị vô hiệu; sau khóa không còn lệnh sửa phân bổ. |
| TC-08 | Partial | Luồng chính đạt: TBM CNPM giao 1 sản phẩm cho DEMO2026 và dashboard ban đầu là 0/1. Dữ liệu bổ sung không đúng kịch bản: `gv.danghaiyen` hiện có cam kết 1 ở cả bốn loại sản phẩm, không phải giảng viên không cam kết. |
| TC-09 | Pass | Tạo sản phẩm, gắn đúng kế hoạch và chỉ tiêu, DOI được chuẩn hóa; tải PDF 139,1 KB lên MinIO thành công. |
| TC-10 | Pass | Ba đề nghị trạng thái đi đúng thứ tự và đúng cấp duyệt; trạng thái chỉ đổi sau xác nhận; ngày công bố và nơi công bố được lưu. |
| TC-11 | Pass | Sản phẩm được gửi, TBM duyệt, Trưởng khoa chọn `QD2023_BB_SCOPUS` và công nhận; tiến độ đạt 1/1 = 100%. |
| TC-12 | Pass | DEMO2026 đạt 1.172/586 giờ, hoàn thành 1/1 chỉ tiêu; chi tiết hiển thị căn cứ `QD2023_BB_SCOPUS v1`; đề xuất top 10%/30% hiển thị và không lưu. |
| TC-13 | Pass | Dashboard chỉ tiêu #13 đạt 100%; bộ lọc năm/bộ môn/học hàm/học vị và ba chiều tổng hợp hoạt động. Excel tải được và chứa đúng một bản ghi phù hợp. PDF API trả 200, một trang A4 ngang, tiếng Việt và bảng không bị cắt. Browser kiểm thử không mở trực tiếp PDF nhưng tệp API hợp lệ. |
| TC-14 | Blocked | Không có Android device/emulator để kiểm tra hồ sơ trên Mobile thật. |
| TC-15 | Blocked | Không có Android device/emulator để thao tác đề nghị trạng thái trên Mobile thật. |
| TC-16 | Blocked | Không có Android device/emulator để kiểm tra thông báo, điều hướng và badge trên Mobile thật. |
| TC-17 | Blocked | Không có Android device/emulator để đối chiếu tiến độ Mobile với Web. |
| TC-18 | Pass | Backend 118 pass, 1 skip; Web build thành công; Flutter analyze không lỗi; 3 widget test pass; APK debug build thành công; PostgreSQL, MinIO và API đang chạy. |

## 4. Dữ liệu được tạo trong vòng kiểm thử

- Kế hoạch 2026-2027 của `demo.gv2026`, trạng thái `Đã đăng ký`.
- Chỉ tiêu bài báo `#13`, số lượng 1, gắn `Journal of Educational Data`, đã khóa phân bổ.
- Phân công 1 sản phẩm cho `DEMO2026`.
- Sản phẩm `#281`: `Mô hình AI cảnh báo chậm tiến độ nghiên cứu khoa học`.
- DOI/mã: `demo-ai-nckh-2026-001`.
- Trạng thái bài viết: `Đã xuất bản`; xét duyệt: `Đã duyệt`.
- Một PDF minh chứng lưu trong MinIO.

## 5. Các ranh giới lỗi đã kiểm tra

- Sai mật khẩu bị từ chối.
- Giảng viên gọi API quản trị nhận 403.
- Không thể khóa phân bổ khi chưa phân bổ đủ.
- Minh chứng đã dùng trong đề nghị không còn lệnh xóa.
- Kế hoạch đăng ký không còn lệnh sửa trực tiếp.
- Định mức năm học có kết quả chốt bị khóa.

## 6. Điểm cần xử lý trước khi bảo vệ

1. Chuẩn bị Android device/emulator và chạy lại TC-14 đến TC-17.
2. Sửa dữ liệu hoặc mô tả ca bổ sung TC-08 vì `gv.danghaiyen` hiện có cam kết.
3. Thay mật khẩu quản trị đang dùng giá trị cấu hình mẫu trước khi trình diễn hoặc triển khai.
4. Bộ test backend còn một integration test PostgreSQL bị skip.
5. Web build thành công nhưng bundle JavaScript chính khoảng 835 KB và có cảnh báo vượt 500 KB.

