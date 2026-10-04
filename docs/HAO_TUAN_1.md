# Bàn giao nền tảng tuần 1 của Hào

Backend dùng schema `nckh` hiện có và giữ kiến trúc xác thực đã được hợp nhất trên `main`. Nhánh này bổ sung CRUD dữ liệu nền, quản trị tài khoản, cookie phiên cho Web, giao diện đăng nhập và kiểm thử tích hợp.

## Chạy tại máy phát triển

1. Sao chép `.env.example` thành `.env` và đặt mật khẩu PostgreSQL, khóa JWT cùng mật khẩu quản trị ban đầu.
2. Chạy database và API: `docker compose --env-file .env -f infra/compose.yaml up --build`.
3. Trong thư mục `web`, chạy `npm install` rồi `npm run dev`.

Khi chạy API trực tiếp bằng `dotnet run`, cấu hình các biến tương ứng:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=research_management;Username=postgres;Password=<mat-khau-db>"
$env:Jwt__SecretKey = "<chuoi-ngau-nhien-tu-32-ky-tu>"
$env:Seed__AdminUsername = "admin"
$env:Seed__AdminPassword = "<mat-khau-quan-tri>"
dotnet run --project backend/src/ResearchManagement.Api --launch-profile http
```

Seeder chỉ tạo tài khoản quản trị khi `Seed__AdminPassword` có giá trị và tên đăng nhập chưa tồn tại. Seeder không ghi đè mật khẩu hoặc quyền của tài khoản cũ.

## API dùng chung

Mọi request POST, PUT và DELETE dưới `/api` phải gửi `X-Requested-With: ResearchHub`. Web sử dụng cookie HttpOnly; Mobile sử dụng `Authorization: Bearer <token>`.

| API | Quyền | Nội dung |
|---|---|---|
| POST `/api/auth/login` | Công khai | Đăng nhập, trả token và đặt cookie Web |
| GET `/api/auth/me` | Đăng nhập | Hồ sơ và danh sách vai trò/phạm vi |
| POST `/api/auth/logout` | Công khai | Xóa cookie Web |
| PUT `/api/auth/password` | Đăng nhập | Đổi mật khẩu hiện tại |
| GET `/api/lookups` | Đăng nhập | Khoa, bộ môn trong phạm vi và danh sách năm học |
| GET/POST `/api/admin/faculties` | Quản trị | Liệt kê hoặc tạo khoa |
| GET/PUT/DELETE `/api/admin/faculties/{id}` | Quản trị | Đọc, sửa hoặc xóa khoa |
| GET/POST `/api/admin/departments` | Quản trị | Liệt kê hoặc tạo bộ môn |
| GET/PUT/DELETE `/api/admin/departments/{id}` | Quản trị | Đọc, sửa hoặc xóa bộ môn |
| GET/POST `/api/admin/academic-years` | Quản trị | Liệt kê hoặc tạo năm học |
| GET/PUT/DELETE `/api/admin/academic-years/{id}` | Quản trị | Đọc, sửa hoặc xóa năm học |
| GET/POST `/api/admin/accounts` | Quản trị | Danh sách phân trang hoặc tạo tài khoản |
| GET/PUT/DELETE `/api/admin/accounts/{id}` | Quản trị | Đọc, sửa trạng thái hoặc xóa tài khoản |
| PUT `/api/admin/accounts/{id}/password` | Quản trị | Đặt lại mật khẩu |
| GET/PUT `/api/admin/accounts/{id}/permissions` | Quản trị | Xem hoặc thay toàn bộ quyền |

Quyền hợp lệ là `GIANG_VIEN/CA_NHAN`, `TRUONG_BO_MON/BO_MON`, `TRUONG_KHOA/KHOA`, hoặc `PHONG_QLKH`, `BAN_GIAM_HIEU`, `QUAN_TRI` với `TOAN_TRUONG`. Bộ môn phải thuộc khoa đã chọn.

JWT chứa các quyền tại thời điểm đăng nhập. Sau khi quản trị viên đổi quyền, khóa tài khoản hoặc đặt lại mật khẩu, người dùng cần đăng nhập lại để nhận token mới. Trong bản đồ án hiện tại chưa có danh sách thu hồi token riêng lẻ.

## Trạng thái giao diện

Web đã có đăng nhập, đăng xuất, khôi phục phiên cookie, xem phạm vi, đổi mật khẩu và CRUD khoa, bộ môn, năm học, tài khoản. API gán quyền và đặt lại mật khẩu đã có; giao diện cho hai thao tác này thuộc phần hoàn thiện quản trị tuần 2.

## Kiểm thử

```sh
dotnet test backend/ResearchManagement.slnx
cd web
npm run build
```

`backend/tests/smoke_api.py` chỉ dùng với database kiểm thử riêng vì script tạo và xóa dữ liệu. Đặt `TEST_API_URL`, `TEST_ADMIN_USERNAME`, `TEST_ADMIN_PASSWORD`, sau đó chạy script bằng Python.
