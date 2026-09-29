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

Khi volume PostgreSQL được tạo lần đầu, Docker tự chạy `infra/database/01_schema_postgresql.sql` để tạo schema `nckh`, 28 bảng, khóa ngoại, ràng buộc, trigger, index và dữ liệu danh mục ban đầu.
PostgreSQL của đồ án được công bố tại cổng `5433` trên máy phát triển để không xung đột với dự án TaskManagement đang dùng cổng `5432`.
Khi chạy toàn bộ Docker Compose, REST API khả dụng tại `http://localhost:8081`.

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

Đã có nền tảng đăng nhập, phân quyền theo phạm vi, API CRUD khoa/bộ môn/năm học/tài khoản và giao diện Web quản trị cơ bản. Mobile hiện là khung ban đầu.

Xem [bàn giao tuần 1 của Hào](docs/HAO_TUAN_1.md) để chạy seed tài khoản, cấu hình Web/API và tích hợp các endpoint cho nhóm.

## Phạm vi

Tài khoản và phân quyền; hồ sơ giảng viên; sản phẩm khoa học và minh chứng; xét duyệt; kế hoạch và tiến độ; chỉ tiêu; quy đổi và đánh giá; báo cáo và thông báo.

## Thứ tự triển khai

1. Database, đăng nhập và phân quyền.
2. Hồ sơ, sản phẩm, minh chứng và xét duyệt.
3. Kế hoạch, chỉ tiêu, tiến độ và nhắc hạn.
4. Quy đổi, đánh giá, dashboard và xuất báo cáo.
5. Tích hợp Mobile, kiểm thử và triển khai demo.
