# Bàn giao tuần 4 của Hào

## Thống kê kết quả nghiên cứu

API `GET /api/reports/research-results` tổng hợp lần đánh giá đã chốt mới nhất của từng giảng viên
trong năm học. Bộ lọc gồm khoa, bộ môn, học hàm và học vị. Kết quả có tổng số giảng viên, số sản
phẩm, tổng giờ, tổng điểm; các nhóm theo bộ môn, học hàm, học vị; và danh sách chi tiết giảng viên.

Phạm vi được kiểm tra tại backend bằng quyền `TOAN_TRUONG`, `KHOA` hoặc `BO_MON`. Dữ liệu ngoài
phạm vi không được đưa vào thống kê kể cả khi người dùng tự sửa query string.

Màn hình Web **Thống kê kết quả** dùng cùng bộ lọc với API. Hai endpoint `export.xlsx` và
`export.pdf` gọi chung hàm dựng báo cáo với endpoint JSON, vì vậy số liệu hiển thị và số liệu xuất
không dùng các truy vấn độc lập.

## Xuất báo cáo

Excel có tiêu đề, chỉ số tổng hợp, bảng chi tiết, định dạng số, cố định hàng tiêu đề và chế độ in
ngang. PDF có tiêu đề năm học, chỉ số tổng hợp, bảng chi tiết và số trang. Tên file chứa mã năm học.

## Tích hợp Mobile

Mobile đăng nhập bằng access token, lấy danh sách năm học từ `/api/lookups` và đọc kết quả cá nhân
từ `/api/evaluations/me`. Màn hình hiển thị xếp loại, căn cứ, tổng giờ/điểm và từng phép quy đổi.
HTTP 404 được hiển thị thành trạng thái **Chưa có đánh giá**, không suy đoán kết quả.

Khi chạy Android Emulator, URL mặc định là `http://10.0.2.2:8081/api`. Có thể đổi khi chạy:

```bash
flutter run --dart-define=API_URL=http://<dia-chi-api>/api
```
