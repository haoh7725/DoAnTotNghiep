# Bàn giao tuần 3 của Hào

## Engine đánh giá và quy đổi

Backend đã triển khai vòng đời kết quả đánh giá: tạo bản nháp, tính chi tiết quy đổi từ sản phẩm đã
duyệt, xem danh sách/chi tiết theo phạm vi và chốt kết quả. Mỗi lần chạy tạo một `lan_danh_gia` mới,
không ghi đè lịch sử. Hồ sơ giảng viên, quy định, phiên bản, giá trị gốc, hệ số và căn cứ tính đều được
lưu dưới dạng ảnh chụp.

Engine chọn quy định theo loại sản phẩm, ngày hiệu lực và phiên bản mới nhất cho từng đơn vị `GIO` /
`DIEM`. Engine hỗ trợ đủ các tiêu chí `NAM_CONG_BO`, `DIEM_CONG_TRINH`, `CHI_SO_TAP_CHI`,
`PHAN_LOAI_TAP_CHI` và `CAP_DE_TAI`. Hệ số tác giả được lấy từ vai trò lưu trên từng người tham gia,
không còn gán chung mọi đồng tác giả vào một hệ số.

## Phân quyền

- Giảng viên chỉ xem kết quả `CHOT` của chính mình qua `/api/evaluations/me`.
- Quản lý chỉ xem dữ liệu trong khoa/bộ môn được phân quyền.
- Bản `NHAP` chỉ người tạo đánh giá và quản trị viên được xem.
- Chỉ Phòng QLKH hoặc quản trị viên được tạo; chỉ người đánh giá hoặc quản trị viên được chốt.

Chi tiết request và response nằm trong [API_KET_QUA_DANH_GIA.md](API_KET_QUA_DANH_GIA.md).
