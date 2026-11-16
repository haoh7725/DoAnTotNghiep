# Kế hoạch kiểm thử chức năng qua giao diện

## 1. Mục tiêu

Kiểm tra hệ thống quản lý hồ sơ nghiên cứu khoa học của giảng viên theo đề cương
`CNTT-KLCN187`, ưu tiên các chức năng được chấm điểm trên Web và Mobile. Mỗi ca
kiểm thử chức năng phải được thao tác trên giao diện người dùng thật; API, log và
CSDL chỉ được dùng để chẩn đoán hoặc đối chiếu sau khi đã quan sát kết quả UI.

## 2. Phạm vi

- Web: đăng nhập/đăng xuất, phân quyền, hồ sơ NCKH, kế hoạch đầu năm, chỉ tiêu,
  phân bổ, sản phẩm, minh chứng, duyệt nhiều cấp, quy đổi, đánh giá, dashboard,
  thống kê và xuất Excel/PDF.
- Mobile: đăng nhập, hồ sơ cá nhân, sản phẩm, cập nhật trạng thái bài viết, thông
  báo, tiến độ cá nhân/đơn vị và kết quả thi đua.
- Kiểm tra quyền theo 5 nhóm: Quản trị viên, Phòng QLKH, Trưởng khoa, Trưởng bộ
  môn và Giảng viên.
- Không kiểm thử tải lớn, xâm nhập, Copyleaks thật hoặc khả năng chịu lỗi hạ tầng
  trong vòng kiểm thử chức năng này.

## 3. Môi trường và dữ liệu

| Thành phần | Cấu hình |
|---|---|
| Web | `http://localhost:5173` |
| API | `http://localhost:8081` |
| CSDL | PostgreSQL trong Docker, cổng máy chủ `5433` |
| Lưu trữ | MinIO trong Docker |
| Năm kiểm thử | `2026-2027` |
| Mật khẩu dữ liệu mẫu | `Huit@2026!` (trừ tài khoản quản trị theo `.env`) |

Tài khoản chạy luồng chính: `qlkh`, `truongkhoa`, `tbm.cnpm` và
`demo.gv2026`. Dữ liệu tạo mới dùng tiền tố `UI-TEST-2026` để dễ nhận diện.

## 4. Nguyên tắc thực thi

1. Chụp hoặc ghi nhận màn hình trước và sau điểm kiểm tra quan trọng.
2. Không đánh dấu Pass chỉ dựa trên phản hồi API; kết quả phải xuất hiện đúng trên UI.
3. Sau thao tác ghi dữ liệu, tải lại trang hoặc đăng nhập lại để kiểm tra tính bền vững.
4. Kiểm tra cả luồng đúng và ít nhất một luồng sai tại mỗi ranh giới nghiệp vụ.
5. Lỗi P0/P1 làm dừng luồng liên quan; ghi rõ bước tái hiện, tài khoản và dữ liệu.

## 5. Mức ưu tiên và tiêu chí

- **P0:** luồng cốt lõi không thể trình diễn nếu lỗi.
- **P1:** quyền truy cập, kiểm tra dữ liệu sai, báo cáo và tính nhất quán.
- **P2:** hiển thị, khả năng sử dụng và trường hợp biên ít gặp.

Điều kiện đạt vòng chính: 100% ca P0 Pass, không còn lỗi nghiêm trọng, ít nhất 90%
ca P1 Pass; mọi ca Fail phải có bằng chứng và bước tái hiện.

## 6. Ma trận kiểm thử Web

| ID | Ưu tiên | Vai trò | Chức năng và thao tác UI chính | Kết quả mong đợi |
|---|---|---|---|---|
| WEB-AUTH-01 | P0 | Mọi vai trò | Đăng nhập đúng, tải lại trang, đăng xuất | Vào đúng màn hình theo quyền; phiên còn sau tải lại; đăng xuất xóa phiên |
| WEB-AUTH-02 | P1 | Khách | Đăng nhập sai và thử mở URL chức năng trực tiếp | Hiện lỗi phù hợp; không vào được nội dung bảo vệ |
| WEB-ADM-01 | P0 | Quản trị | Mở danh sách tài khoản, tạo/sửa tài khoản thử | Dữ liệu lưu đúng và còn sau tải lại |
| WEB-ADM-02 | P0 | Quản trị | Phân quyền và phạm vi cho tài khoản thử | Menu và dữ liệu nhìn thấy thay đổi đúng theo quyền |
| WEB-ADM-03 | P1 | Quản trị | Tạo/sửa năm học và định mức giờ NCKH | Giá trị hiển thị đúng; không cho nhập định mức không hợp lệ |
| WEB-PRO-01 | P0 | Giảng viên | Xem ba phần hồ sơ cá nhân | Thông tin, lý lịch khoa học và công trình đã công nhận đầy đủ |
| WEB-PRO-02 | P1 | Giảng viên | Sửa một mục lý lịch, lưu và tải lại | Dữ liệu giữ nguyên sau tải lại |
| WEB-PLAN-01 | P0 | Giảng viên | Tạo kế hoạch năm, thêm nội dung, đăng ký | Tổng số lượng hợp lệ; trạng thái chuyển sang Đã đăng ký |
| WEB-PLAN-02 | P1 | Giảng viên | Đăng ký khi tổng nội dung khác cam kết hoặc ngày sai | UI chặn và giải thích rõ nguyên nhân |
| WEB-TAR-01 | P0 | QLKH | Tạo chỉ tiêu bài viết cho khoa | Chỉ tiêu ở trạng thái chờ Trưởng khoa tiếp nhận |
| WEB-TAR-02 | P0 | Trưởng khoa | Tiếp nhận, mở phân bổ, giao cho bộ môn và khóa | Tổng phân bổ đúng; sau khóa không còn sửa được |
| WEB-TAR-03 | P0 | Trưởng bộ môn | Phân công chỉ tiêu cho giảng viên | Không vượt cam kết hoặc số lượng bộ môn được nhận |
| WEB-TAR-04 | P1 | Sai vai trò | Thử sửa chỉ tiêu/phân bổ ngoài phạm vi | Không hiện lệnh hoặc bị từ chối rõ ràng |
| WEB-PRD-01 | P0 | Giảng viên | Tạo sản phẩm từ nội dung kế hoạch, liên kết chỉ tiêu | Sản phẩm nháp lưu đúng kế hoạch, loại và nơi công bố |
| WEB-PRD-02 | P0 | Giảng viên | Thêm DOI, mô tả, đồng tác giả và PDF minh chứng | Thông tin và tệp hiện đúng sau tải lại |
| WEB-PRD-03 | P0 | Giảng viên | Gửi duyệt sản phẩm | Trạng thái chuyển Đã gửi; dữ liệu bị khóa theo quy tắc |
| WEB-PRD-04 | P1 | Giảng viên | Khai báo trùng DOI/mã công trình | Bị từ chối và không tạo bản trùng |
| WEB-PRD-05 | P1 | Giảng viên | Chọn nơi công bố/hạn không khớp chỉ tiêu | Bị từ chối với lý do nghiệp vụ cụ thể |
| WEB-REV-01 | P0 | TBM | Duyệt cấp bộ môn trên giao diện | Sản phẩm chuyển đúng cấp, có lịch sử và ghi chú |
| WEB-REV-02 | P0 | Trưởng khoa | Duyệt cấp khoa trên giao diện | Sản phẩm chuyển Phòng QLKH, lịch sử không mất |
| WEB-REV-03 | P0 | QLKH | Kiểm tra minh chứng và duyệt cuối | Sản phẩm được công nhận; chỉ tiêu cá nhân tăng tiến độ |
| WEB-REV-04 | P1 | Người duyệt | Yêu cầu bổ sung có lý do; giảng viên sửa và gửi lại | Giảng viên nhận thông báo; luồng tiếp tục đúng cấp |
| WEB-REV-05 | P1 | Người duyệt | Duyệt cuối khi thiếu ngày hoặc minh chứng | Không cho duyệt; hiển thị trường còn thiếu |
| WEB-EVA-01 | P0 | QLKH | Tạo đánh giá theo Giờ NCKH và chốt | Lấy đúng sản phẩm công nhận, định mức năm học và tỷ lệ |
| WEB-EVA-02 | P0 | QLKH | Tạo đánh giá theo Điểm NCKH và chốt | Dùng điểm, không dùng định mức giờ |
| WEB-EVA-03 | P1 | Quản trị/QLKH | Sửa định mức năm học đã có kết quả chốt | Bị khóa, tránh thay đổi kết quả lịch sử |
| WEB-DAS-01 | P0 | Trưởng khoa | Xem dashboard tiến độ khoa/bộ môn | Số liệu, tỷ lệ và biểu đồ khớp dữ liệu luồng chính |
| WEB-DAS-02 | P1 | Trưởng bộ môn | Xem dashboard trong phạm vi bộ môn | Không lộ dữ liệu ngoài phạm vi |
| WEB-REP-01 | P0 | QLKH | Lọc thống kê theo năm/bộ môn/học hàm/học vị | Bảng thay đổi đúng theo bộ lọc |
| WEB-REP-02 | P0 | QLKH | Xuất Excel và mở kiểm tra | Tệp tải được; tiêu đề, cột và số liệu đúng UI |
| WEB-REP-03 | P0 | QLKH | Xuất PDF và mở kiểm tra | Tệp tải được, tiếng Việt và bảng không bị cắt |
| WEB-NOT-01 | P1 | Các vai trò | Kiểm tra thông báo sau giao việc, trả lại, duyệt | Đúng người, đúng nội dung, điều hướng tới đúng đối tượng |

## 7. Ma trận kiểm thử Mobile

| ID | Ưu tiên | Vai trò | Chức năng và thao tác UI chính | Kết quả mong đợi |
|---|---|---|---|---|
| MOB-AUTH-01 | P0 | Giảng viên | Đăng nhập và đăng xuất | Phiên hoạt động đúng, không lộ màn hình sau đăng xuất |
| MOB-PRO-01 | P0 | Giảng viên | Tra cứu hồ sơ, đề tài, bài báo đã duyệt | Dữ liệu khớp Web |
| MOB-PRD-01 | P0 | Giảng viên | Cập nhật trạng thái bài viết qua các trạng thái hợp lệ | Trạng thái lưu và đồng bộ lại trên Web |
| MOB-NOT-01 | P0 | Giảng viên | Xem nhắc hạn và phản hồi mới | Thông báo đúng dữ liệu và điều hướng phù hợp |
| MOB-DAS-01 | P0 | Giảng viên | Xem tiến độ cá nhân và tóm tắt đơn vị | Tỷ lệ khớp Web, biểu đồ không tràn màn hình |
| MOB-EVA-01 | P1 | Giảng viên | Xem kết quả thi đua theo Giờ/Điểm | Hiện đúng phương thức, định mức và tỷ lệ |
| MOB-RES-01 | P1 | Giảng viên | Kiểm tra màn hình nhỏ, tải lại, mất/kết nối lại mạng | Không vỡ giao diện; lỗi mạng có thể phục hồi |

## 8. Thứ tự chạy luồng xuyên vai trò P0

1. `demo.gv2026`: kiểm tra hồ sơ, tạo và đăng ký kế hoạch.
2. `qlkh`: tạo chỉ tiêu khoa.
3. `truongkhoa`: tiếp nhận, phân bổ cho `tbm.cnpm`, khóa.
4. `tbm.cnpm`: giao một bài cho `demo.gv2026`.
5. `demo.gv2026`: tạo sản phẩm, tải minh chứng, gửi duyệt.
6. `tbm.cnpm` -> `truongkhoa` -> `qlkh`: duyệt ba cấp.
7. `demo.gv2026`: xác nhận tiến độ thay đổi từ 0% lên 100%.
8. `qlkh`: tạo/chốt đánh giá, xem dashboard và xuất Excel/PDF.
9. Mobile: xác nhận hồ sơ, sản phẩm, thông báo, tiến độ và thi đua khớp Web.

## 9. Nhật ký kết quả

| ID | Trạng thái | Thời điểm | Bằng chứng | Lỗi/ghi chú |
|---|---|---|---|---|
| Chưa chạy | Not Run | - | - | Cập nhật sau mỗi ca kiểm thử UI |

## 10. Ước lượng tài nguyên

- Docker với PostgreSQL, MinIO và API: thường khoảng 1-2.5 GB RAM tùy lúc build.
- Web Vite và một trình duyệt: thường khoảng 0.5-1.5 GB RAM.
- Android Emulator và Flutter: thường thêm 2-4 GB RAM, là phần tốn tài nguyên nhất.
- Chạy Web trước rồi Mobile sau giúp giảm tải; không cần chạy build/test song song.
- Mức suy luận của trợ lý ảnh hưởng token sử dụng, còn tải CPU/RAM máy chủ yếu đến
  từ Docker, trình duyệt và Android Emulator.
