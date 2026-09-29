# Bàn giao phần nền tảng tuần 1 của Hào

Backend cung cấp đăng nhập, phiên làm việc, phân quyền và CRUD dữ liệu nền trên schema `nckh` hiện có. Không tạo lại schema bằng EF migrations hoặc `EnsureCreated`.

## Chạy tại máy phát triển

1. Sao chép `.env.example` thành `.env`, đặt mật khẩu PostgreSQL. Nếu chạy seed, đặt `SEED_PASSWORD` từ 12 đến 128 ký tự.
2. Chạy database: `docker compose --env-file .env -f infra/compose.yaml up -d postgres`. SQL khởi tạo chỉ chạy khi volume mới. Giữ nguyên volume đang có dữ liệu.
3. Chạy API: `dotnet run --project backend/src/ResearchManagement.Api --launch-profile http` tại cổng 5152. Khi chạy ngoài Docker, cấu hình `ConnectionStrings__DefaultConnection` bằng biến môi trường khớp với mật khẩu PostgreSQL tại cổng 5433; file `.env` không được .NET tự đọc.
4. Trong `web`, chạy `npm install` rồi `npm run dev`. Vite chuyển `/api` đến `http://localhost:5152`.
5. Khi dùng API Docker cổng 8081, đặt biến `API_PROXY_TARGET=http://localhost:8081` trước `npm run dev`.

Ví dụ PowerShell cho API local (điền giá trị thật trong terminal, không đưa vào Git):

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=research_management;Username=postgres;Password=<mat-khau-db>"
$env:Seed__Password = "<mat-khau-demo-tu-12-ky-tu>"
dotnet run --project backend/src/ResearchManagement.Api --launch-profile http -- --seed-demo
dotnet run --project backend/src/ResearchManagement.Api --launch-profile http
```

Hoặc seed trong Docker sau khi build image:

```sh
docker compose --env-file .env -f infra/compose.yaml build api
docker compose --env-file .env -f infra/compose.yaml run --rm api dotnet ResearchManagement.Api.dll --seed-demo
```

Seed là lệnh chủ động, chỉ hoạt động trong Development. Lệnh tạo khoa `DEMO_CNTT`, bộ môn `DEMO_CNPM`, năm học `2026-2027`, sáu tài khoản và liên kết giảng viên cho ba vai trò giảng viên/trưởng bộ môn/trưởng khoa. Chạy lại không đặt lại mật khẩu hay cấp lại quyền cho tài khoản đã tồn tại.

| Tài khoản | Vai trò | Phạm vi |
|---|---|---|
| demo.quan_tri | Quản trị | Toàn trường |
| demo.giang_vien | Giảng viên | Cá nhân |
| demo.truong_bo_mon | Trưởng bộ môn | Bộ môn demo |
| demo.truong_khoa | Trưởng khoa | Khoa demo |
| demo.phong_qlkh | Phòng quản lý khoa học | Toàn trường |
| demo.ban_giam_hieu | Ban giám hiệu | Toàn trường |

Các tài khoản mới dùng mật khẩu truyền qua `Seed__Password` hoặc `SEED_PASSWORD` trong Compose. Không có mật khẩu demo cố định trong mã nguồn.

## Hợp đồng API dùng chung

Mọi request thay đổi dữ liệu (POST/PUT/DELETE) cần header `X-Requested-With: ResearchHub`. JSON dùng camelCase. ID trong database là bigint; giao diện hiện dùng số JavaScript, phù hợp dữ liệu demo/đồ án. Không sử dụng ID vượt giới hạn số nguyên an toàn của JavaScript.

| API | Quyền | Nội dung |
|---|---|---|
| POST /api/auth/login | Công khai | `{username,password}` → `{accessToken,tokenType,expiresAt}` và cookie HttpOnly |
| GET /api/auth/me | Đăng nhập | ID, tên đăng nhập, họ tên, email, danh sách quyền và phạm vi |
| POST /api/auth/logout | Công khai | Xóa cookie phiên trên trình duyệt |
| PUT /api/auth/password | Đăng nhập | `{currentPassword,newPassword}`, hết hiệu lực mọi token dùng mật khẩu cũ |
| GET /api/lookups | Đăng nhập | Khoa/bộ môn trong phạm vi được xem và các năm học |
| GET/POST /api/admin/faculties | Quản trị | Danh sách / tạo khoa |
| GET/PUT/DELETE /api/admin/faculties/{id} | Quản trị | Đọc / sửa / xóa khoa |
| GET/POST /api/admin/departments | Quản trị | Danh sách / tạo bộ môn |
| GET/PUT/DELETE /api/admin/departments/{id} | Quản trị | Đọc / sửa / xóa bộ môn |
| GET/POST /api/admin/academic-years | Quản trị | Danh sách / tạo năm học |
| GET/PUT/DELETE /api/admin/academic-years/{id} | Quản trị | Đọc / sửa / xóa năm học |
| GET /api/admin/accounts?page=1&pageSize=20 | Quản trị | `{items,total,page,pageSize}`; tối đa 100 bản ghi/trang |
| POST /api/admin/accounts | Quản trị | `{username,fullName,email,password}`; chưa tự gán quyền |
| GET/PUT/DELETE /api/admin/accounts/{id} | Quản trị | Đọc / sửa `{fullName,email,status}` / xóa tài khoản chưa được sử dụng |
| PUT /api/admin/accounts/{id}/password | Quản trị | Đặt lại `{password}` và hết hiệu lực token cũ |
| GET/PUT /api/admin/accounts/{id}/permissions | Quản trị | Xem / thay toàn bộ danh sách quyền trong transaction |

Khoa: `{code,name}`. Bộ môn: `{facultyId,code,name}`. Năm học: `{code,startDate,endDate}` với ngày dạng `YYYY-MM-DD`. Tên đăng nhập phân biệt hoa thường; chỉ cho chữ không dấu, số, dấu chấm, gạch ngang và gạch dưới. Mật khẩu mới có độ dài 12–128. Email có thể null. Trạng thái tài khoản là `HOAT_DONG` hoặc `KHOA`.

Ví dụ gán quyền trưởng bộ môn (thay ID bằng dữ liệu thật):

```json
[
  { "role": "TRUONG_BO_MON", "scope": "BO_MON", "facultyId": 1, "departmentId": 1 }
]
```

Quyền hợp lệ: `GIANG_VIEN/CA_NHAN`, `TRUONG_BO_MON/BO_MON`, `TRUONG_KHOA/KHOA`, hoặc `PHONG_QLKH`, `BAN_GIAM_HIEU`, `QUAN_TRI` với `TOAN_TRUONG`. Bộ môn phải thuộc khoa đã chọn. Tài khoản có thể có nhiều quyền. Quản trị viên không được tự khóa, tự xóa hoặc sửa quyền của chính phiên đang dùng.

Lỗi: 400 dữ liệu không hợp lệ, 401 chưa đăng nhập/hết phiên/tài khoản khóa, 403 thiếu quyền, 404 không tìm thấy, 409 trùng dữ liệu hoặc bản ghi đang được tham chiếu, 429 đăng nhập quá 8 lần/phút/IP, 503 lỗi database. Lỗi nghiệp vụ trả `title`; lỗi validation còn có `errors` theo trường.

## Quy tắc tích hợp cho Lâm và Phi

- Web dùng cookie HttpOnly, SameSite Strict; không lưu token trong localStorage. Vite proxy giữ API cùng origin. Production cần reverse proxy `/api` về backend và HTTPS.
- Mobile lấy `accessToken` sau đăng nhập, gửi `Authorization: Bearer <token>`, lưu bằng kho lưu trữ an toàn của nền tảng; vẫn gửi custom header cho request ghi.
- JWT sống 30 phút, chưa có refresh token. Khi hết hạn, đăng nhập lại. Development không đặt `Auth__SigningKey` sẽ dùng khóa tạm mới sau mỗi lần restart; Production bắt buộc khóa bí mật ngẫu nhiên ít nhất 32 byte. Có thể cấu hình `Auth__LifetimeMinutes` từ 5 đến 120.
- Logout xóa cookie; Mobile phải xóa token ở thiết bị. Token bearer đã sao chép còn hiệu lực đến khi hết hạn, đổi mật khẩu hoặc khóa tài khoản. Chưa có danh sách thu hồi từng token.
- Trạng thái tài khoản và quyền được đọc lại từ database ở mỗi request; không tin quyền/phạm vi do client tự gửi. Đổi mật khẩu làm token cũ không dùng được nữa.
- Endpoint nghiệp vụ mới phải dùng `[Authorize]` và lọc dữ liệu theo chủ sở hữu/khoa/bộ môn từ database. `CurrentSession.CanRead(ownerAccountId, facultyId, departmentId)` kiểm tra phạm vi đọc; đây không phải quyền duyệt hay quyền sửa. Quyền hành động cụ thể phải chốt riêng.
- `GET /api/lookups` chỉ cung cấp danh mục trong phạm vi tài khoản hoặc đơn vị liên kết giảng viên; không phải API liệt kê toàn bộ giảng viên. Năm học là danh mục chung.
- Xóa bản ghi đang được sử dụng sẽ trả 409; không xóa dây chuyền dữ liệu nghiệp vụ. Đối với tài khoản đã có lịch sử, dùng trạng thái khóa.

## Trạng thái bàn giao

Đã có backend xác thực, CRUD dữ liệu nền, API gán quyền, API đặt lại/đổi mật khẩu; Web đăng nhập/đăng xuất, khôi phục phiên, guard quản trị, xem phạm vi, đổi mật khẩu và CRUD khoa/bộ môn/năm học/tài khoản. Giao diện gán quyền và đặt lại mật khẩu quản trị thuộc phần hoàn thiện quản trị tuần 2; hiện sử dụng API. Quy chế quy đổi và ngưỡng xếp loại cần tài liệu chính thức của trường trước khi triển khai tuần 2–3.

## Kiểm thử

```sh
dotnet test backend/ResearchManagement.slnx
cd web
npm run build
```

`backend/tests/smoke_api.py` chạy HTTP trên API thật và PostgreSQL có dữ liệu seed. Chỉ chạy trên database kiểm thử riêng: script sửa dữ liệu demo và đặt lại mật khẩu demo giảng viên. Đặt `TEST_API_URL` và `TEST_SEED_PASSWORD`, rồi chạy `python backend/tests/smoke_api.py`. Chờ cửa sổ giới hạn đăng nhập mới hoặc khởi động lại API kiểm thử trước khi chạy. Script kiểm tra 65 phản hồi HTTP cùng quyền theo vai trò/phạm vi, không trả hash mật khẩu, thay quyền có hiệu lực ngay và thu hồi token sau đổi mật khẩu.

Đã chạy trên PostgreSQL 18.4 cài sẵn trong database tách biệt; Docker khai báo PostgreSQL 16 chưa được kiểm thử runtime vì Docker Engine chưa chạy. SQL schema đã chạy thành công trên database kiểm thử. Không thay đổi dữ liệu trong database chính.
