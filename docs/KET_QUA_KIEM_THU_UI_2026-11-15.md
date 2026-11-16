# Kết quả kiểm thử giao diện - 15/11/2026

## 1. Căn cứ kiểm thử

- Đề cương `CNTT-KLCN187_Nguyen Van Le.pdf`.
- Phạm vi vòng này: Web, dữ liệu mẫu năm học `2026-2027`.
- Môi trường: Web `http://127.0.0.1:5173`, API Docker `http://localhost:8081`, PostgreSQL cổng `5433`.
- Công cụ thao tác: trình duyệt thật trong Codex, điều khiển bằng Playwright DOM.
- Không thực hiện thao tác tạo, sửa, xóa dữ liệu trong vòng smoke test này.

## 2. Tài khoản đã dùng

| Tài khoản | Vai trò mong đợi | Kết quả đăng nhập |
|---|---|---|
| `qlkh` | Phòng QLKH, toàn trường | Đăng nhập thành công nhưng không có quyền |
| `truongkhoa` | Trưởng khoa CNTT | Thành công, đúng phạm vi khoa |
| `gv.nguyenminhanh` | Giảng viên | Thành công, đúng phạm vi cá nhân |

Mật khẩu dữ liệu mẫu được lấy từ cấu hình/hướng dẫn nội bộ của dự án và không ghi lại trong báo cáo.

## 3. Kết quả thực tế

| ID | Yêu cầu/kịch bản | Kết quả quan sát trên UI | Trạng thái |
|---|---|---|---|
| WEB-AUTH-01 | Đăng nhập bằng tài khoản hoặc mật khẩu sai | Hiện thông báo `Tên đăng nhập hoặc mật khẩu không đúng.` và vẫn ở màn hình đăng nhập | Pass |
| WEB-AUTH-02 | Đăng nhập đúng bằng `qlkh` | Vào được hệ thống nhưng hiện `Tài khoản chưa được gán quyền` | Fail |
| WEB-AUTH-03 | Tải lại trang sau khi đăng nhập | Phiên `qlkh` vẫn được giữ và không quay lại màn hình đăng nhập | Pass |
| WEB-RBAC-01 | Đăng nhập bằng Trưởng khoa | Hiện vai trò Trưởng khoa, Khoa CNTT và 5 bộ môn thuộc phạm vi | Pass |
| WEB-DAS-01 | Xem dashboard tiến độ của Trưởng khoa | Hiện 4 chỉ tiêu; bài báo đạt 9/24, tương ứng 37,5% | Pass |
| WEB-REV-01 | Mở danh sách sản phẩm chờ duyệt | Màn hình `Sản phẩm chờ duyệt` và `Danh sách cần xử lý` tải thành công | Pass |
| WEB-PRO-01 | Mở hồ sơ giảng viên trong phạm vi khoa | Màn hình `Hồ sơ giảng viên` tải thành công | Pass |
| WEB-TAR-01 | Mở quản lý chỉ tiêu và phân bổ | Danh sách chỉ tiêu, quy trình và dashboard tiến độ tải thành công | Pass |
| WEB-REP-01 | Xem thống kê năm học 2026-2027 | Hiện 25 giảng viên, 22 sản phẩm, biểu đồ và bảng tổng hợp theo 5 bộ môn | Pass |
| WEB-REP-02 | Xuất Excel theo bộ lọc hiện tại | Nút và URL xuất hiện đúng; trình duyệt kiểm thử không được cấp quyền tải trong thời gian chờ | Blocked |
| WEB-REP-03 | Xuất PDF theo bộ lọc hiện tại | Nút và URL xuất hiện đúng; chưa thể thực hiện vì cùng giới hạn tải của trình duyệt kiểm thử | Blocked |
| WEB-LEC-01 | Mở tổng quan công việc giảng viên | Hiện tiến độ 1/4 (25%), 5 sản phẩm công nhận, 4 bản nháp và 3 sản phẩm cần bổ sung | Pass |
| WEB-LEC-02 | Mở hồ sơ cá nhân | Màn hình `Hồ sơ của tôi` và `Thông tin hồ sơ` tải thành công | Pass |
| WEB-PLAN-01 | Xem kế hoạch đầu năm | Hiện kế hoạch 2026-2027, 4 nội dung và phần tổng quan kế hoạch | Pass |
| WEB-EVA-01 | Xem kết quả thi đua cá nhân | Hiện kết quả NCKH, từng chỉ tiêu và danh sách sản phẩm theo giờ | Pass |
| WEB-NOT-01 | Xem thông báo/nhắc hạn | Hiện 2 thông báo chưa đọc, gồm phản hồi minh chứng và phân công chỉ tiêu | Pass |

Tổng hợp vòng smoke: **13 Pass, 1 Fail, 2 Blocked**.

## 4. Lỗi phát hiện

### BUG-UI-001 - Tài khoản `qlkh` không có phân quyền

- Mức độ: P0 đối với luồng xuyên vai trò.
- Bước tái hiện:
  1. Mở trang đăng nhập.
  2. Đăng nhập bằng tài khoản mẫu `qlkh`.
  3. Quan sát màn hình tổng quan.
- Kết quả thực tế: UI báo tài khoản chưa được gán quyền; chỉ còn Tổng quan, Thông báo và Đổi mật khẩu.
- Kết quả mong đợi: có vai trò `PHONG_QLKH`, phạm vi `TOAN_TRUONG`, truy cập được nghiệp vụ chỉ tiêu, duyệt cuối, đánh giá và báo cáo.
- Đối chiếu CSDL: tài khoản tồn tại trong `nckh.tai_khoan` nhưng không có bản ghi tương ứng trong `nckh.phan_quyen`.
- Ảnh hưởng: không thể chạy luồng P0 QLKH tạo chỉ tiêu, duyệt cuối và chốt đánh giá.

## 5. Phạm vi chưa chạy

- Các thao tác ghi dữ liệu: tạo kế hoạch, phân bổ chỉ tiêu, tạo sản phẩm, gửi và duyệt nhiều cấp.
- Kiểm tra nội dung thực tế của tệp Excel/PDF sau tải.
- Web trên kích thước màn hình mobile.
- Ứng dụng Flutter Mobile và đồng bộ Web-Mobile.

