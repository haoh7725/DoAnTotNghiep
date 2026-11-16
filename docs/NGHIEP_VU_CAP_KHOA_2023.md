# Nghiệp vụ NCKH cấp khoa theo tài liệu chỉnh sửa

Hệ thống sử dụng bốn nhóm NCKH và bảng giờ trong hai ảnh do người dùng cung cấp. Định mức hoàn thành được cấu hình chung theo từng năm học; năm 2026-2027 mặc định 586 giờ. Bảng ảnh chưa chỉ rõ ngày hiệu lực và cách chia giờ giữa tác giả; danh mục được đặt mốc cấu hình 01/01/2023, kế thừa hệ số tác giả đã cấu hình trước đó để khoa kiểm tra lại.

## Vai trò và luồng

- Trưởng khoa tạo chỉ tiêu, mở phân bổ cho bộ môn, khóa phân bổ, công nhận sản phẩm và chốt kết quả năm.
- Trưởng bộ môn được giao chỉ tiêu cho giảng viên thuộc bộ môn dù chưa có cam kết. Cam kết dùng làm thông tin ưu tiên, không giới hạn số được giao; tổng giao không vượt phân bổ bộ môn.
- Giảng viên khai sản phẩm, minh chứng và gửi duyệt. Trưởng bộ môn duyệt trước, Trưởng khoa công nhận cuối.
- Người có tên trong danh sách tác giả không tự duyệt công trình. Nếu Trưởng khoa là tác giả, quản trị viên thực hiện vai trò duyệt thay thế, vẫn bị chặn tự duyệt.
- Phân quyền QLKH/Ban giám hiệu được lưu trữ rồi gỡ. Tài khoản và lý lịch được giữ lại; tài khoản chỉ có vai trò bị gỡ sẽ cần quản trị viên phân quyền phù hợp.
- Tiến độ công bố ở tạp chí được người có quyền duyệt xác nhận theo từng bước và minh chứng; việc duyệt nội bộ không tự đổi trạng thái công bố.

### Đề nghị cập nhật trạng thái bài báo

1. Tác giả chính chọn đúng bước kế tiếp: Đang viết → Đang phản biện → Đã nhận xét → Đã xuất bản.
2. Tác giả chọn một tệp minh chứng thuộc sản phẩm và gửi đề nghị trên Web hoặc Mobile. Một bài báo chỉ có một đề nghị đang chờ.
3. Trưởng bộ môn xác nhận bước Đang phản biện. Trưởng khoa xác nhận bước Đã nhận xét và Đã xuất bản. Người tham gia bài báo không được tự xác nhận; quản trị viên chỉ đóng vai trò thay thế khi có xung đột.
4. Khi xác nhận, hệ thống mới cập nhật trạng thái bài báo và ghi lịch sử. Khi trả lại, trạng thái bài báo giữ nguyên, lý do được gửi cho tác giả.
5. Bước Đã xuất bản bắt buộc ngày và nơi công bố; ngày không được nằm trong tương lai. Tác giả có thể bổ sung minh chứng mới trong lúc hồ sơ đang xử lý nhưng không được xóa minh chứng đã dùng trong đề nghị.

## Tổng hợp và đánh giá

- GET `/api/annual-research?academicYearId=...`: danh sách giảng viên trong phạm vi đơn vị, gồm tổng giờ, chỉ tiêu và vấn đề cần kiểm tra.
- GET `/api/annual-research/me?academicYearId=...`: giảng viên tự xem kết quả tạm tính.
- POST `/api/annual-research/{lecturerId}/finalize`: Trưởng khoa chốt ảnh chụp kết quả; body có `academicYearId`. Không chốt khi còn vấn đề quy đổi hoặc tự chốt cho mình.
- GET `/api/evaluations` và `/api/evaluations/{id}` tiếp tục cung cấp kết quả lịch sử. Các API tạo/chốt cũ theo điểm và định mức riêng không còn được công bố.
- Mỗi sản phẩm chỉ tính một lần cho một giảng viên trong năm công bố/nghiệm thu. Không cộng thêm giờ chỉ vì liên kết sản phẩm với chỉ tiêu.
- Tỷ lệ chỉ tiêu = số sản phẩm được công nhận / số được giao. Mức hoàn thành năm = tổng giờ so với định mức chung của năm học. Hai kết quả độc lập.
- 25 nhóm quy đổi mới bao gồm đơn vị bài, đề tài, quyển, năm thực hiện và trang dịch gốc. `recognitionQuantity` mặc định 1; chỉ các nhóm tính theo năm/trang được nhận số khác 1.
- Hội thảo cấp khoa không ISBN tại HUIT có mức 88 giờ/bài, giới hạn 120 giờ/giảng viên/năm học sau hệ số tác giả.
- Hồ sơ có ngày tương lai, thiếu ngày, trùng công trình, chưa có phân loại phù hợp hoặc thiếu hệ số sẽ hiển thị vấn đề và không được chốt. Không tự sửa ngày hoặc đoán phân loại của dữ liệu cũ.

## Đề xuất danh hiệu

Danh sách chỉ được tính và hiển thị, không lưu kết quả. Giảng viên đạt định mức giờ của năm học và không có vấn đề quy đổi được xếp theo tổng giờ giảm dần trong khoa. Top 10% hiển thị là Lao động tiên tiến; nhóm từ sau top 10% đến hết top 30% hiển thị là Chiến sĩ thi đua. Số suất làm tròn lên; đồng giờ tại ranh giới được cảnh báo. Hệ thống không tạo quyết định hoặc lưu danh hiệu.

Định mức giờ nằm trong danh mục Năm học. Quản trị viên có thể sửa trước khi năm học có kết quả đánh giá đã chốt; sau đó hệ thống khóa giá trị để bảo toàn căn cứ lịch sử.

## Hồ sơ và báo cáo

Lý lịch có bốn ô sản phẩm đã công nhận, gồm sách/giáo trình. Xuất Excel/PDF tại `/api/lecturers/{id}/research-export/xlsx` hoặc `pdf`; có thể lọc `academicYearId`, `productTypeCode`. Dữ liệu nền có tìm kiếm, danh sách tài khoản tìm trên server qua tham số `search`.

## Dữ liệu cần xác nhận

- Danh sách tên khoa/bộ môn đúng chưa có trong tài liệu, cần người dùng cung cấp trước khi đổi.
- Ngày hiệu lực chính thức, quy tắc chia giờ tác giả và điều kiện danh hiệu ngoài giờ NCKH cần được khoa xác nhận.
- Các kết quả lịch sử và quy định cũ được giữ nguyên. Dữ liệu cũ phân loại mơ hồ cần xác minh trước khi chốt theo bảng mới.
- Không chạy lại seed để tạo sẵn kết quả mới khi khởi động ứng dụng. Tài khoản demo trống được giữ nguyên để kiểm thử.
