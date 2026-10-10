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

Web mặc định kết nối API Docker Compose tại `http://localhost:8081`, vì vậy
không cần tạo thêm file cấu hình. Chỉ sao chép `web/.env.example` thành
`web/.env.local` và đổi sang `http://localhost:5152` khi chạy backend bằng
`dotnet run`.

### Mobile Flutter

```bash
cd mobile
flutter pub get
flutter run
```

Android Emulator dùng API Docker trên máy phát triển qua `10.0.2.2`. Khi API chạy ở địa chỉ khác:

```bash
flutter run --dart-define=API_URL=http://10.0.2.2:8081/api
```

## Trạng thái

Đã có nền tảng đăng nhập, phân quyền theo phạm vi, API CRUD khoa/bộ môn/năm học/tài khoản và giao diện Web quản trị cơ bản. Mobile hiện là khung ban đầu.

Xem [bàn giao tuần 1 của Hào](docs/HAO_TUAN_1.md) để chạy seed tài khoản, cấu hình Web/API và tích hợp các endpoint cho nhóm.

Xem [hợp đồng API giảng viên, lý lịch khoa học và loại sản phẩm của Lâm](docs/LAM_TUAN_1.md) để tích hợp `/api/lecturers` và `/api/product-types`.

Backend đã có xác thực JWT và phân quyền theo vai trò/phạm vi:

- `POST /api/auth/login`: đăng nhập, trả về access token (giới hạn 10 lần/phút/IP).
- `GET /api/auth/me`: thông tin tài khoản và các phân quyền hiện tại (Web dùng cookie HttpOnly, Mobile dùng header `Authorization: Bearer <token>`).
- Policy: `Admin`, `ResearchOffice`, `Management` (xem `Api/Authorization/Policies.cs`). Kiểm tra phạm vi khoa/bộ môn qua `ICurrentUser.CanAccess(facultyId, departmentId)`.
- Lần chạy đầu, nếu đặt `SEED_ADMIN_PASSWORD` trong `.env`, hệ thống tạo tài khoản QUAN_TRI (mặc định tên `admin`).
- Lỗi trả về theo chuẩn ProblemDetails; danh sách có phân trang dùng `PagedQuery` / `PagedResult<T>`.

## Phạm vi

Tài khoản và phân quyền; hồ sơ giảng viên; sản phẩm khoa học và minh chứng; xét duyệt; kế hoạch và tiến độ; chỉ tiêu; quy đổi và đánh giá; báo cáo và thông báo.

## Thứ tự triển khai

1. Database, đăng nhập và phân quyền.
2. Hồ sơ, sản phẩm, minh chứng và xét duyệt.
3. Kế hoạch, chỉ tiêu, tiến độ và nhắc hạn.
4. Quy đổi, đánh giá, dashboard và xuất báo cáo.
5. Tích hợp Mobile, kiểm thử và triển khai demo.
