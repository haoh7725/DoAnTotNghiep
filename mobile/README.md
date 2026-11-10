# ResearchHub Mobile

Ứng dụng Flutter dành cho giảng viên, gồm tra cứu hồ sơ cá nhân, sản phẩm khoa học,
kết quả thi đua, tiến độ chỉ tiêu và trung tâm thông báo.

## Chạy ứng dụng

```powershell
flutter pub get
flutter run --dart-define=API_URL=http://10.0.2.2:8081/api
```

Khi chạy trên thiết bị thật, thay `10.0.2.2` bằng địa chỉ IP của máy chạy backend.

## Cơ chế thông báo

- Backend kiểm tra nhắc hạn theo lịch và lưu thông báo của từng tài khoản.
- Khi ứng dụng đang mở, badge thông báo được cập nhật khi vào màn hình và mỗi 60 giây.
- Phản hồi mới từ Phòng QLKH và cảnh báo chỉ tiêu được xem trong tab **Thông báo**.

Hiện phiên bản đồ án chưa tích hợp Firebase Cloud Messaging/APNs. Vì vậy ứng dụng
không hiện push notification của hệ điều hành khi đã bị tắt hoàn toàn. Muốn triển khai
thực tế cần cấu hình dự án Firebase/APNs, khóa ký ứng dụng và token thiết bị cho từng
tài khoản; các thông tin này không được giả lập hoặc đưa khóa bí mật vào repository.
