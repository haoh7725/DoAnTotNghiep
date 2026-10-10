# Bàn giao tuần 2 của Hào

## Quản trị tài khoản

Màn hình quản trị đã hỗ trợ gán nhiều quyền cho một tài khoản và đặt lại mật khẩu. Vai trò và phạm vi được ràng buộc theo các cặp hợp lệ của hệ thống; quyền bộ môn bắt buộc chọn cả khoa và bộ môn. Giao diện ngăn quản trị viên tự xóa hoặc tự thay đổi quyền của tài khoản đang dùng, đồng thời API vẫn là lớp kiểm tra cuối cùng.

## Quy định quy đổi

API quản trị dùng đường dẫn `/api/admin/conversion-rules` với các thao tác GET, POST, PUT và DELETE. Chỉ tài khoản `QUAN_TRI/TOAN_TRUONG` được truy cập. Dữ liệu được lưu trực tiếp vào các bảng `quy_dinh_quy_doi`, `tieu_chi_quy_doi` và `he_so_tac_gia_quy_doi` có sẵn; không dùng EF migrations.

Mỗi quy định gồm loại sản phẩm, mã, phiên bản, tên, mô tả điều kiện, mức và đơn vị quy đổi (`GIO` hoặc `DIEM`), quy tắc tác giả, khoảng hiệu lực và văn bản căn cứ. Danh sách `criteria` mô tả điều kiện có cấu trúc; `authorCoefficients` lưu hệ số theo vai trò tác giả.

Mã quy định và phiên bản không được trùng. Ngày hết hiệu lực không được trước ngày bắt đầu. Tiêu chí chữ chỉ áp dụng cho chỉ số tạp chí, phân loại tạp chí và cấp đề tài; tiêu chí số áp dụng cho điểm công trình và năm công bố. Vai trò tác giả trong một quy định không được trùng.

Màn hình Web **Quy định quy đổi** cho phép quản lý toàn bộ nội dung trên, gồm nhiều tiêu chí và nhiều hệ số tác giả. Công thức tính kết quả từ các quy định này thuộc hạng mục tuần 3.
