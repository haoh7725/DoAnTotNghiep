# Hợp đồng API sản phẩm khoa học, trạng thái bài báo và đồng tác giả (tuần 2 của Lâm)

Dùng chung quy ước của [HAO_TUAN_1.md](HAO_TUAN_1.md) và [LAM_TUAN_1.md](LAM_TUAN_1.md): cookie HttpOnly (Web) hoặc `Authorization: Bearer` (Mobile); POST/PUT/DELETE gửi thêm `X-Requested-With: ResearchHub`; JSON camelCase, ngày `yyyy-MM-dd`; lỗi theo ProblemDetails.

API ánh xạ vào `san_pham_khoa_hoc` và `tham_gia_san_pham` có sẵn trong `infra/database/01_schema_postgresql.sql`; **schema không đổi**. Bản nháp trước đó của module này trỏ vào bảng `san_pham`/`dong_tac_gia` không tồn tại trong schema nên đã được viết lại.

## Hai loại trạng thái

| Trạng thái | Cột | Giá trị | Ai đổi |
|---|---|---|---|
| Bài báo (tiến độ) | `trang_thai_bai_bao`, chỉ loại `BAI_BAO` | `DANG_VIET` → `DANG_PHAN_BIEN` → `DA_NHAN_XET` → `DA_XUAT_BAN` | Tác giả, qua `PUT /api/products/{id}/article-status` |
| Xét duyệt | `trang_thai_duyet` | `NHAP`, `CHO_DUYET`, `CAN_BO_SUNG`, `DA_DUYET`, `TU_CHOI` | Luồng gửi duyệt (tuần 3). Tuần 2 mọi sản phẩm là `NHAP` |

Chuyển trạng thái bài báo hợp lệ: `DANG_VIET → DANG_PHAN_BIEN`; `DANG_PHAN_BIEN → DA_NHAN_XET | DANG_VIET`; `DA_NHAN_XET → DANG_PHAN_BIEN | DA_XUAT_BAN | DANG_VIET`; `DA_XUAT_BAN` là cuối. Chuyển sang `DA_XUAT_BAN` yêu cầu đã có `journalName` và `publicationYear` (hoặc `publishedDate`).

## Endpoint

| API | Nội dung |
|---|---|
| GET `/api/products` | Danh sách phân trang theo phạm vi được xem. Tham số: `page`, `pageSize`, `keyword` (tên, mã, tạp chí, DOI), `productTypeId`, `academicYearId`, `reviewStatus`, `articleStatus`, `lecturerId`, `facultyId`, `departmentId`, `mine`. Sắp theo `id` giảm dần |
| GET `/api/products/{id}` | Chi tiết kèm `authors` và `permissions` |
| POST `/api/products` | Tạo sản phẩm (201). Người tạo là tác giả chính |
| PUT `/api/products/{id}` | Sửa nội dung (không đổi được loại) |
| PUT `/api/products/{id}/article-status` | `{ "status": "DANG_PHAN_BIEN" }` |
| GET `/api/products/{id}/authors` | Danh sách tác giả theo thứ tự |
| PUT `/api/products/{id}/authors` | Thay toàn bộ danh sách: `{ "authors": [{ "lecturerId": 1, "role": "TAC_GIA_CHINH" }, …] }`, thứ tự mảng là thứ tự tác giả |
| POST `/api/products/{id}/authors` | Thêm một đồng tác giả vào cuối: `{ "lecturerId": 3, "role": "DONG_TAC_GIA" }` |
| DELETE `/api/products/{id}/authors/{lecturerId}` | Bỏ một đồng tác giả, dồn lại thứ tự. Trả danh sách còn lại |
| GET `/api/products/author-candidates?keyword=` | Tìm giảng viên toàn trường (≥ 2 ký tự, tối đa 10) để chọn tác giả. Chỉ trả mã, tên, bộ môn, khoa |

Các endpoint cũ `/api/research-plan-items/{id}/products`, `/api/products/{id}/submit|review|review-history` và `/co-authors` không còn (chưa có client nào dùng). Gửi duyệt/xét duyệt là hạng mục tuần 3; liên kết sản phẩm với nội dung kế hoạch (`noi_dung_ke_hoach_san_pham`) thuộc phần của Phi.

## Tạo và sửa

```json
{
  "productTypeId": 1, "academicYearId": 3, "title": "Học máy cho dự báo điểm",
  "articleStatus": "DANG_VIET",
  "publicationYear": 2026, "doi": "10.1000/abc", "journalName": "Journal of X",
  "journalIndex": "SCIE", "journalCategory": "Q2", "issn": "1234-5678", "workScore": 1.5,
  "submittedDate": "2026-03-01", "publishedDate": null,
  "coAuthors": [{ "lecturerId": 3, "role": "DONG_TAC_GIA" }],
  "mainAuthorLecturerId": null
}
```

- Chỉ `productTypeId` (khi tạo), `academicYearId` và `title` bắt buộc. Các trường khác tùy loại: bài báo dùng `journal*`, `issn`, `doi`, `submittedDate`, `publishedDate`; đề tài dùng `projectLevel`, `hostUnit`, `projectObjective`, `projectContent`, `expectedResult`, `startDate`, `endDate`; sách dùng `isbn`, `publisher`; chứng nhận dùng `certificateNumber`, `issuingAuthority`. Chuỗi rỗng được lưu là `null`.
- `articleStatus` chỉ cho loại `BAI_BAO` (mặc định `DANG_VIET`); loại khác gửi giá trị này sẽ bị 422.
- `mainAuthorLecturerId`, `coAuthors` chỉ có ở POST. `mainAuthorLecturerId` chỉ `PHONG_QLKH`/`QUAN_TRI` được đặt (nhập hộ); người khác gửi giá trị khác hồ sơ của mình bị 403.
- `code` (mã sản phẩm) do hệ thống sinh dạng `BB-2026-K7M2QX` (`BB`, `DT`, `SA`, `CN` theo loại), không sửa được.
- `publicationYear` bỏ trống thì lấy từ `publishedDate`. `doi` duy nhất không phân biệt hoa thường (409 nếu trùng).
- `version` bắt đầu từ 1 và chưa tăng ở tuần 2 (xem mục cần chốt).

`permissions` trong chi tiết cho giao diện biết `canEdit`, `canManageAuthors`, `canChangeArticleStatus` và `nextArticleStatuses`; backend vẫn kiểm tra lại mọi thao tác.

## Tác giả

Vai trò (`role`): `TAC_GIA_CHINH`, `TAC_GIA_LIEN_HE`, `DONG_TAC_GIA`. Đây cũng là khóa `he_so_tac_gia_quy_doi.vai_tro_tac_gia`, nên **quy định quy đổi của Hào phải dùng đúng các mã này**.

- Mỗi sản phẩm có 1–30 tác giả, đúng một `TAC_GIA_CHINH`, mỗi giảng viên một lần. Thứ tự là 1..n liên tục do server đánh lại.
- `departmentId` của tác giả là bộ môn **tại thời điểm thêm** (`bo_mon_id_ghi_nhan`); người đã có trong danh sách giữ nguyên bộ môn khi sửa danh sách.
- Không bỏ được tác giả chính; muốn đổi, dùng PUT toàn bộ danh sách để chuyển vai trò. POST không nhận `TAC_GIA_CHINH`.
- Tác giả đã có chi tiết quy đổi (`chi_tiet_quy_doi`) không bỏ được: trả 409.

## Quyền

| Thao tác | Ai được làm |
|---|---|
| Xem | Quản trị; người tạo và các tác giả; người có phạm vi `BO_MON`/`KHOA`/`TOAN_TRUONG` bao trùm đơn vị của một tác giả, **chỉ khi sản phẩm đã rời trạng thái `NHAP`** (bản nháp là riêng tư) |
| Sửa nội dung, đổi trạng thái bài báo, quản lý tác giả | Người tạo, tác giả chính hoặc Quản trị, **chỉ khi `trang_thai_duyet` là `NHAP` hoặc `CAN_BO_SUNG`** |
| Tạo | Tài khoản có hồ sơ giảng viên; `PHONG_QLKH`/`QUAN_TRI` nhập hộ được |

## Mã lỗi

| Mã | Khi nào |
|---|---|
| 400 | Thiếu `X-Requested-With`; dữ liệu không qua kiểm tra |
| 403 | Không được xem, hoặc không phải người quản lý sản phẩm |
| 404 | Không có sản phẩm, hoặc giảng viên không phải tác giả khi bỏ |
| 409 | Trùng DOI; giảng viên đã là tác giả; tác giả đã được tính quy đổi; danh sách tác giả bị đổi ở nơi khác |
| 422 | Loại/năm học không tồn tại; chuyển trạng thái bài báo sai bước; sản phẩm đã gửi duyệt; sai quy tắc tác giả; ngày kết thúc trước ngày bắt đầu; xuất bản thiếu tạp chí/năm |

## Điểm cần cả nhóm chốt

1. **Phiên bản (`phien_ban`)**: tuần 2 chưa ghi `san_pham_phien_ban`. Đề xuất: ghi bản chụp khi **gửi duyệt** và tăng phiên bản khi sửa trong `CAN_BO_SUNG` (tuần 3), vì sửa bản nháp liên tục không nên tạo hàng chục phiên bản.
2. **Đổi trạng thái bài báo sau khi gửi duyệt**: hiện bị khóa cùng quyền sửa. Nếu bài đã duyệt ở `DANG_PHAN_BIEN` rồi mới được nhận đăng thì tác giả không cập nhật được `DA_XUAT_BAN`. Cần chốt có cho phép riêng việc này không (và có phải duyệt lại không).
3. **Vai trò tác giả**: 3 mã ở trên đã đủ cho bài báo/sách; đề tài có thể cần "Chủ nhiệm", "Thành viên". Chốt cùng Hào trước khi nhập hệ số.
4. **Bản nháp riêng tư**: trưởng bộ môn, trưởng khoa và Phòng QLKH không thấy bản nháp. Đổi lại nếu nhóm muốn.
5. **Minh chứng và lịch sử xét duyệt** (`EvidenceService`, `ReviewHistory`) vẫn là bản nháp cũ chưa khớp `minh_chung` và `lich_su_xet_duyet`; sẽ làm lại ở hạng mục tương ứng.
