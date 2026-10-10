# Bàn giao tuần 2 của Lâm

## Sản phẩm khoa học

Web có mục **Sản phẩm khoa học** cho giảng viên. Màn hình hỗ trợ danh sách, chi tiết, thêm và sửa
sản phẩm gắn với nội dung kế hoạch; cập nhật trạng thái bài báo, nơi và ngày công bố, chỉ số/phân loại
tạp chí và cấp đề tài. Giảng viên có thể gửi sản phẩm vào quy trình xét duyệt; sản phẩm đã gửi không
được sửa cho đến khi được yêu cầu bổ sung.

Đồng tác giả được quản lý theo thứ tự và vai trò. `TAC_GIA_CHINH` dành cho người nộp sản phẩm;
các vai trò của đồng tác giả được lưu trực tiếp để engine quy đổi áp dụng đúng hệ số đã cấu hình.

## Minh chứng

API và Web hỗ trợ upload, liệt kê, tải xuống và xóa minh chứng. Tệp tối đa 20 MB; chỉ nhận PDF,
Word, Excel, PowerPoint, JPG, PNG và ZIP. Tệp được đổi sang tên ngẫu nhiên khi lưu, còn tên gốc được
giữ trong database. Đường dẫn tải xuống được kiểm tra nằm trong thư mục upload của sản phẩm.

Chỉ tác giả chính hoặc quản trị viên được sửa sản phẩm, đồng tác giả và minh chứng. API là lớp kiểm
tra cuối cùng; việc ẩn nút trên Web không thay thế phân quyền backend.

## Cơ sở dữ liệu

`infra/database/02_products_evaluations.sql` tạo các bảng ứng dụng sản phẩm và chi tiết quy đổi theo
mô hình hiện tại. Database mới chạy script tự động qua Docker Compose. Với volume đã tồn tại, cần áp
dụng script một lần bằng `psql` hoặc tạo lại volume phát triển sau khi đã sao lưu dữ liệu cần giữ.
