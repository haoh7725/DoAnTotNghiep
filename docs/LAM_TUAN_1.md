# Hợp đồng API giảng viên, lý lịch khoa học và loại sản phẩm (tuần 1 của Lâm)

Dùng chung quy ước của [bàn giao tuần 1 của Hào](HAO_TUAN_1.md): xác thực bằng cookie HttpOnly (Web) hoặc `Authorization: Bearer <token>` (Mobile); mọi POST/PUT/DELETE gửi thêm `X-Requested-With: ResearchHub`. JSON dùng camelCase, ngày dùng `yyyy-MM-dd`, thời điểm dùng ISO 8601 UTC.

Schema SQL không đổi: API chỉ ánh xạ vào `giang_vien`, `ly_lich_khoa_hoc`, `loai_san_pham` có sẵn trong `infra/database/01_schema_postgresql.sql`.

## Danh sách endpoint

| API | Quyền | Nội dung |
|---|---|---|
| GET `/api/lecturers` | Đăng nhập, kết quả lọc theo phạm vi | Danh sách phân trang |
| GET `/api/lecturers/me` | Đăng nhập | Hồ sơ giảng viên của tài khoản hiện tại |
| PUT `/api/lecturers/me` | Chính chủ | Tự sửa thông tin cá nhân |
| GET `/api/lecturers/me/scientific-profile` | Chính chủ | Lý lịch khoa học của mình |
| PUT `/api/lecturers/me/scientific-profile` | Chính chủ | Tạo hoặc ghi đè lý lịch của mình |
| GET `/api/lecturers/{id}` | Chính chủ hoặc phạm vi bao trùm đơn vị | Chi tiết giảng viên |
| POST `/api/lecturers` | `PHONG_QLKH`, `QUAN_TRI` | Tạo giảng viên, liên kết một tài khoản |
| PUT `/api/lecturers/{id}` | `PHONG_QLKH`, `QUAN_TRI` | Sửa toàn bộ thông tin |
| DELETE `/api/lecturers/{id}` | `QUAN_TRI` | Xóa giảng viên và lý lịch |
| GET `/api/lecturers/{id}/scientific-profile` | Như GET `/api/lecturers/{id}` | Xem lý lịch |
| PUT `/api/lecturers/{id}/scientific-profile` | Chính chủ, `PHONG_QLKH`, `QUAN_TRI` | Tạo hoặc ghi đè lý lịch |
| GET `/api/product-types` | Đăng nhập | Toàn bộ loại sản phẩm, không phân trang |
| GET `/api/product-types/{id}` | Đăng nhập | Một loại sản phẩm |
| POST/PUT/DELETE `/api/product-types[/{id}]` | `QUAN_TRI` | Quản lý danh mục |

"Phạm vi bao trùm đơn vị" lấy từ `ICurrentUser.CanAccess`: `TOAN_TRUONG` thấy tất cả, `KHOA` thấy giảng viên cùng khoa, `BO_MON` thấy giảng viên cùng bộ môn. Quyền `CA_NHAN` chỉ cho xem hồ sơ của chính mình. Trưởng khoa, trưởng bộ môn và Ban giám hiệu chỉ xem, không sửa lý lịch của người khác.

## Giảng viên

### GET `/api/lecturers`

Tham số: `page` (mặc định 1), `pageSize` (mặc định 20, tối đa 100), `keyword` (họ tên hoặc mã, không phân biệt hoa thường), `facultyId`, `departmentId`. Sắp theo `code`.

```json
{
  "items": [
    {
      "id": 7,
      "accountId": 12,
      "code": "GV001",
      "fullName": "Nguyễn Văn A",
      "birthDate": "1985-03-20",
      "gender": "NAM",
      "email": "a@huit.edu.vn",
      "phone": "0901234567",
      "academicRank": "Phó giáo sư",
      "degree": "Tiến sĩ",
      "position": "Giảng viên chính",
      "departmentId": 3,
      "departmentName": "Bộ môn Công nghệ phần mềm",
      "facultyId": 1,
      "facultyName": "Khoa Công nghệ thông tin"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

Mọi endpoint trả một giảng viên dùng cùng cấu trúc của phần tử `items`. `birthDate`, `gender`, `email`, `phone`, `academicRank`, `degree`, `position` có thể là `null`.

### GET `/api/lecturers/me`

Trả 404 nếu tài khoản chưa được liên kết giảng viên. Mobile dùng `id` trong kết quả này làm `lecturerId` khi gọi API kế hoạch (`CreateResearchPlanRequest.lecturerId`).

### PUT `/api/lecturers/me`

Giảng viên chỉ tự sửa được thông tin cá nhân. Bộ môn, mã, học hàm, học vị, chức vụ do Phòng QLKH quản lý vì thống kê và đánh giá dựa vào các trường này.

```json
{ "fullName": "Nguyễn Văn A", "birthDate": "1985-03-20", "gender": "NAM", "email": "a@huit.edu.vn", "phone": "0901234567" }
```

### POST `/api/lecturers` và PUT `/api/lecturers/{id}`

```json
{
  "accountId": 12,
  "departmentId": 3,
  "code": "GV001",
  "fullName": "Nguyễn Văn A",
  "birthDate": "1985-03-20",
  "gender": "NAM",
  "email": "a@huit.edu.vn",
  "phone": "0901234567",
  "academicRank": "Phó giáo sư",
  "degree": "Tiến sĩ",
  "position": "Giảng viên chính"
}
```

`accountId` chỉ có ở POST; tài khoản liên kết không đổi được sau khi tạo. Trường tùy chọn để trống thì gửi `null` hoặc bỏ qua; riêng `email` gửi chuỗi rỗng `""` sẽ bị 400. Các trường văn bản rỗng hoặc chỉ có khoảng trắng được lưu là `null`. `gender` chỉ nhận `NAM`, `NU`, `KHAC`. Ngày sinh phải từ năm 1900 và không ở tương lai.

POST trả 201 kèm `Location`. Tài khoản phải tồn tại và chưa liên kết giảng viên khác; `code` không trùng; bộ môn phải tồn tại.

## Lý lịch khoa học

Mỗi giảng viên có tối đa một lý lịch. Cấu trúc đọc và ghi:

```json
{
  "lecturerId": 7,
  "expertise": "Kỹ thuật phần mềm",
  "researchFields": "Phân tích dữ liệu",
  "researchDirections": "Học máy ứng dụng",
  "activitySummary": "…",
  "updatedAt": "2026-10-03T08:15:00+00:00"
}
```

Khi ghi (PUT) chỉ gửi bốn trường văn bản, mỗi trường tối đa 4000 ký tự. PUT ghi đè toàn bộ: trường không gửi hoặc rỗng sẽ được lưu là `null`. Chưa có lý lịch thì GET vẫn trả 200 với `updatedAt: null` và bốn trường văn bản `null`, để giao diện không phải xử lý 404. PUT trả 200 với bản đã lưu, cho cả lần tạo đầu tiên.

## Loại sản phẩm

```json
[ { "id": 1, "code": "BAI_BAO", "name": "Bài báo" } ]
```

Dữ liệu ban đầu có bốn loại: `BAI_BAO`, `DE_TAI`, `SACH`, `CHUNG_NHAN`. Khi tạo, `code` gồm chữ hoa không dấu, số và gạch dưới, bắt đầu bằng chữ, tối đa 30 ký tự. PUT chỉ đổi `name` (`{ "name": "…" }`); mã giữ nguyên vì biểu mẫu sản phẩm và quy đổi dựa vào mã. DELETE trả 409 nếu loại đã được sản phẩm, kế hoạch hoặc quy định quy đổi dùng.

## Mã lỗi

Lỗi trả theo ProblemDetails (`status`, `title`, `detail`).

| Mã | Khi nào |
|---|---|
| 400 | Thiếu `X-Requested-With`; dữ liệu không qua kiểm tra (kèm `errors` theo từng trường) |
| 401 | Chưa đăng nhập hoặc token hết hạn |
| 403 | Ngoài phạm vi hoặc không đủ vai trò |
| 404 | Không có bản ghi, hoặc tài khoản chưa liên kết giảng viên (`/me`) |
| 409 | Trùng mã giảng viên, tài khoản đã liên kết, trùng mã loại, hoặc xóa bản ghi đang được dùng |
| 422 | Bộ môn hoặc tài khoản trong body không tồn tại; ngày sinh không hợp lệ |

## Tích hợp với phần của Hào và Phi

- Bộ môn, khoa lấy từ dữ liệu nền của Hào (`bo_mon`, `khoa`) và trả kèm tên trong mỗi giảng viên. Tài khoản phải do `POST /api/admin/accounts` tạo trước. Giảng viên cần quyền `GIANG_VIEN/CA_NHAN`, trưởng bộ môn cần `TRUONG_BO_MON/BO_MON` để thấy đúng phạm vi.
- Phi: `GET /api/product-types` cấp `productTypeId` cho nội dung kế hoạch; `GET /api/lecturers/me` cấp `lecturerId` cho kế hoạch.
- Hào: thống kê tuần 4 theo học hàm, học vị lấy từ `academicRank`, `degree` của danh sách giảng viên theo cùng phạm vi.
- Dữ liệu mẫu: chạy `backend/tests/seed_demo_lecturers.py` trên database phát triển để có giảng viên ở các bộ môn hiện có.

## Điểm cần cả nhóm chốt

1. `academicRank` và `degree` đang là văn bản tự do (cột `VARCHAR(100)` không có ràng buộc). Thống kê theo học hàm, học vị của Hào sẽ tách nhóm sai nếu nhập "PGS" lẫn "Phó giáo sư". Nên chốt danh sách giá trị cố định trước tuần 4.
2. Tập giá trị `gender` (`NAM`, `NU`, `KHAC`) là đề xuất ở tầng API; DB chưa ràng buộc.
3. Ngoài phạm vi trả 403 (không phải 404). Đổi sang 404 nếu nhóm muốn che sự tồn tại của hồ sơ.
4. Lý lịch ghi theo kiểu ghi đè, bản ghi sau thắng. Nếu Web và Mobile cùng sửa một lý lịch, cần thêm kiểm tra phiên bản dựa trên `updatedAt`.
