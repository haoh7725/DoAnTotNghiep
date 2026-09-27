# Hệ thống quản lý hồ sơ nghiên cứu khoa học của giảng viên

Đồ án tốt nghiệp gồm Web React, Mobile Flutter và backend ASP.NET Core REST API dùng chung PostgreSQL.

## Cấu trúc

- `backend/`: ASP.NET Core Web API, Entity Framework Core và Npgsql.
- `web/`: React và TypeScript.
- `mobile/`: Flutter và Dart.
- `docs/`: yêu cầu, thiết kế, tài liệu API và hướng dẫn.
- `infra/`: Docker Compose và cấu hình triển khai.

## Khởi động nhanh

### Cơ sở dữ liệu và API bằng Docker

```bash
docker compose --env-file .env -f infra/compose.yaml up --build
```

Sao chép `.env.example` thành `.env` và đổi mật khẩu trước khi chạy.

### Web React

```bash
cd web
npm install
npm run dev
```

### Mobile Flutter

```bash
cd mobile
flutter pub get
flutter run
```

## Trạng thái

Đã khởi tạo solution ASP.NET Core, React, Flutter, PostgreSQL bằng Docker và một endpoint kiểm tra tại `GET /api/system/info`.

## Phạm vi

Tài khoản và phân quyền; hồ sơ giảng viên; sản phẩm khoa học và minh chứng; xét duyệt; kế hoạch và tiến độ; chỉ tiêu; quy đổi và đánh giá; báo cáo và thông báo.

## Thứ tự triển khai

1. Database, đăng nhập và phân quyền.
2. Hồ sơ, sản phẩm, minh chứng và xét duyệt.
3. Kế hoạch, chỉ tiêu, tiến độ và nhắc hạn.
4. Quy đổi, đánh giá, dashboard và xuất báo cáo.
5. Tích hợp Mobile, kiểm thử và triển khai demo.
