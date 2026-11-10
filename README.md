# Hệ thống quản lý hồ sơ nghiên cứu khoa học của giảng viên

Đồ án tốt nghiệp gồm Web React, Mobile Flutter và backend ASP.NET Core REST API dùng chung PostgreSQL.

## Cấu trúc

- `backend/`: ASP.NET Core Web API, Entity Framework Core và Npgsql.
- `web/`: React và TypeScript.
- `mobile/`: Flutter và Dart.
- `infra/`: Docker Compose và cấu hình triển khai.

## Khởi động nhanh

## Hướng dẫn chạy sau khi pull code (dành cho thành viên trong nhóm)

Điều kiện: cài **Docker Desktop**, **Node.js 20+** và **.NET SDK 9**. Mở Docker
Desktop trước khi chạy. Từ thư mục gốc của dự án, thực hiện đúng thứ tự sau:

```powershell
# 1. Lấy phiên bản mới nhất của đúng nhánh làm việc
git pull origin <ten-nhanh>

# 2. Tạo cấu hình local (chỉ làm lần đầu trên mỗi máy)
Copy-Item .env.example .env
Copy-Item web\.env.example web\.env.local
```

Mở `.env` và tối thiểu thay ba giá trị mẫu: `POSTGRES_PASSWORD`,
`MINIO_ROOT_PASSWORD`, `JWT_SECRET_KEY` (khóa JWT dài từ 32 ký tự). Không commit
file `.env` hay `web/.env.local`.

```powershell
# 3. Chạy Database + MinIO + API
docker compose --env-file .env -f infra/compose.yaml up --build -d

# 4. Kiểm tra ba container đều đang chạy
docker compose --env-file .env -f infra/compose.yaml ps

# 5. Cài và chạy Web (mở terminal thứ hai)
Set-Location web
npm install
npm run dev
```

Mở `http://localhost:5173`. API phải phản hồi tại `http://localhost:8081/health`
(hoặc endpoint Swagger nếu được bật). Khi khởi tạo database mới với
`SEED_SAMPLE_DATA=true`, hệ thống có sẵn bộ dữ liệu quy mô một khoa. Các tài khoản
`qlkh`, `bgh`, `truongkhoa`, `tbm.cnpm`, `tbm.httt`, `gv.nguyenminhanh`… dùng chung
mật khẩu `Huit@2026!`.

### Nếu pull về nhưng không chạy được

- Cổng `5173`, `8081`, `5433`, `9000` hoặc `9001` đang được dùng: tắt ứng dụng
  chiếm cổng, rồi chạy lại lệnh Docker/Web.
- API không lên: xem log bằng
  `docker compose --env-file .env -f infra/compose.yaml logs api --tail=100`.
- Web báo không kết nối API: kiểm tra API đã ở trạng thái `running`, và đảm bảo
  `web/.env.local` có `API_PROXY_TARGET=http://localhost:8081` khi dùng Docker.
- Sau khi backend hoặc SQL đổi: chạy `docker compose --env-file .env -f infra/compose.yaml up --build -d`
  để build lại API.
- Muốn làm sạch toàn bộ dữ liệu local để tạo lại bộ dữ liệu mẫu: `docker compose --env-file .env -f infra/compose.yaml down -v`, rồi chạy lại bước 3. Lệnh này xóa dữ liệu local của dự án.

### Database đã được tạo từ phiên bản cũ

Các script SQL chỉ tự chạy khi volume PostgreSQL được tạo lần đầu. Với database
đã tồn tại, chạy lần lượt các migration còn thiếu bên dưới trong PowerShell:

```powershell
Get-Content -Raw -Encoding UTF8 infra/database/08_scientific_profile_details.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw -Encoding UTF8 infra/database/08_target_workflow.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw -Encoding UTF8 infra/database/09_lecturer_target_allocations.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw -Encoding UTF8 infra/database/10_conversion_priority.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw -Encoding UTF8 infra/database/11_emulation_classification_and_target_reminders.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
```

Nếu đã đổi tên database hoặc user trong `.env`, thay `research_management` và
`postgres` trong các lệnh trên. Mỗi script có thể báo dữ liệu/ràng buộc đã tồn tại
nếu đã được áp dụng; không chạy song song nhiều thành viên trên cùng một database.

### Cơ sở dữ liệu và API bằng Docker

```bash
docker compose --env-file .env -f infra/compose.yaml up --build
```

Sao chép `.env.example` thành `.env` và đổi mật khẩu trước khi chạy.

Khi volume PostgreSQL được tạo lần đầu, Docker tự chạy `infra/database/01_schema_postgresql.sql` để tạo schema `nckh`, 28 bảng, khóa ngoại, ràng buộc, trigger, index và dữ liệu danh mục ban đầu.
PostgreSQL của đồ án được công bố tại cổng `5433` trên máy phát triển để không xung đột với dự án TaskManagement đang dùng cổng `5432`.
Khi chạy toàn bộ Docker Compose, REST API khả dụng tại `http://localhost:8081`.
File minh chứng được lưu trong MinIO tại bucket `research-evidences`; dữ liệu MinIO
được giữ bền vững bằng volume `minio_data`. Giao diện quản trị MinIO ở
`http://localhost:9001` và dùng tài khoản trong `.env`.

Kiểm tra tương đồng dùng Copyleaks theo luồng bất đồng bộ. Điền các biến
`COPYLEAKS_*` trong `.env`; để `COPYLEAKS_SANDBOX=true` trong lúc phát triển.
`COPYLEAKS_PUBLIC_BASE_URL` phải là HTTPS công khai để Copyleaks gọi webhook
(khi chạy local cần một tunnel HTTPS).

Database đã tồn tại cần áp dụng bảng phiên bản sản phẩm mới một lần (giữ nguyên dữ liệu):

```powershell
Get-Content -Raw infra/database/02_products_evaluations.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw infra/database/03_product_snapshots.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw infra/database/04_journal_conference_catalog.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw infra/database/05_notification_products.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw infra/database/06_feedback_and_target_catalog.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw infra/database/07_plagiarism_checks.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw -Encoding UTF8 infra/database/08_scientific_profile_details.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw -Encoding UTF8 infra/database/08_target_workflow.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw -Encoding UTF8 infra/database/09_lecturer_target_allocations.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw -Encoding UTF8 infra/database/10_conversion_priority.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
Get-Content -Raw -Encoding UTF8 infra/database/11_emulation_classification_and_target_reminders.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
```

Nếu đã thay đổi `POSTGRES_USER` hoặc `POSTGRES_DB` trong `.env`, thay hai giá trị
tương ứng trong lệnh trên. Database mới tự chạy script này khi khởi tạo.
Phiên bản được lưu từ các lần nộp/xét duyệt sau khi cập nhật; lịch sử cũ không được tạo lại.

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

Backend đã có xác thực JWT và phân quyền theo vai trò/phạm vi:

- `POST /api/auth/login`: đăng nhập, trả về access token (giới hạn 10 lần/phút/IP).
- `GET /api/auth/me`: thông tin tài khoản và các phân quyền hiện tại (Web dùng cookie HttpOnly, Mobile dùng header `Authorization: Bearer <token>`).
- Policy: `Admin`, `ResearchOffice`, `Management` (xem `Api/Authorization/Policies.cs`). Kiểm tra phạm vi khoa/bộ môn qua `ICurrentUser.CanAccess(facultyId, departmentId)`.
- Lần chạy đầu, nếu đặt `SEED_ADMIN_PASSWORD` trong `.env`, hệ thống tạo tài khoản QUAN_TRI (mặc định tên `admin`).
- Trong môi trường Development, hệ thống tự tạo bộ dữ liệu mẫu quy mô vận hành: 2 khoa, 8 bộ môn, 32 giảng viên,
  3 năm học, 80 kế hoạch, hơn 150 nội dung/sản phẩm, chỉ tiêu phân bổ cá nhân, thông báo, quy đổi và đánh giá.
  Các tài khoản `qlkh`, `bgh`, `truongkhoa`, `tbm.cnpm`, `tbm.httt` và các tài khoản `gv.*`
  dùng chung mật khẩu `Huit@2026!`. Seeder chạy lặp lại an toàn và có thể tắt bằng cấu hình
  `Seed:SampleData=false`. Đây là dữ liệu phát triển cục bộ; không dùng tài khoản hoặc mật khẩu này khi triển khai thật.
  Hai PDF minh chứng mẫu được đóng gói trong `ResearchManagement.Api/DemoEvidence` và tự tải
  lên bucket MinIO khi seed, nên endpoint tải xuống hoạt động cả local và Docker.
- Lỗi trả về theo chuẩn ProblemDetails; danh sách có phân trang dùng `PagedQuery` / `PagedResult<T>`.

## Phạm vi

Tài khoản và phân quyền; hồ sơ giảng viên; sản phẩm khoa học và minh chứng; xét duyệt; kế hoạch và tiến độ; chỉ tiêu; quy đổi và đánh giá; báo cáo và thông báo.

## Thứ tự triển khai

1. Database, đăng nhập và phân quyền.
2. Hồ sơ, sản phẩm, minh chứng và xét duyệt.
3. Kế hoạch, chỉ tiêu, tiến độ và nhắc hạn.
4. Quy đổi, đánh giá, dashboard và xuất báo cáo.
5. Tích hợp Mobile, kiểm thử và triển khai.
