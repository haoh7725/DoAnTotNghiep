# Lý lịch khoa học theo mẫu Bộ GDĐT

Đối chiếu mẫu `LY_LICH_KHOA_HOC_MAU_CUA_BO.doc` do người dùng cung cấp, ghi Phụ lục V Thông tư 08/2011/TT-BGDĐT ngày 17/02/2011. Triển khai trên Web và API dùng chung, không thay đổi quy trình xét duyệt sản phẩm nghiên cứu.

## Nội dung theo mẫu

| Mục | Dữ liệu |
| --- | --- |
| I. Lý lịch sơ lược | Họ tên, giới tính, ngày sinh, nơi sinh, quê quán, dân tộc; học vị cao nhất và năm/nước nhận; chức danh khoa học và năm bổ nhiệm; chức vụ, đơn vị công tác; địa chỉ, điện thoại cơ quan/nhà riêng/di động, fax, email; CMND/CCCD, ngày/nơi cấp |
| II. Đào tạo | Nhiều dòng phân loại đại học, bằng đại học 2, thạc sĩ, tiến sĩ; hệ đào tạo, nơi đào tạo, ngành/chuyên ngành, nước đào tạo, năm tốt nghiệp/cấp bằng, tên luận án |
| II. Ngoại ngữ | Nhiều dòng ngoại ngữ và mức độ sử dụng |
| III. Công tác chuyên môn | Từ ngày, đến ngày, nơi công tác, công việc đảm nhiệm |
| IV.1. Đề tài | Tên đề tài, năm bắt đầu/hoàn thành, cấp đề tài, trách nhiệm tham gia |
| IV.2. Công trình công bố | Tên công trình, năm công bố, tạp chí/nơi công bố |
| Kê khai | Địa điểm, ngày kê khai, vị trí ký của người khai và xác nhận cơ quan trên bản in |

Họ tên, giới tính, ngày sinh, học hàm, học vị, chức vụ, di động, email lấy từ hồ sơ giảng viên, không lưu bản sao để tránh lệch thông tin. Đơn vị công tác có trường riêng để khai cả đơn vị trước khi nghỉ hưu. Chữ ký và con dấu thực hiện trên bản in; hệ thống không giả lập xác nhận cơ quan.

## Nhập và xem

Trong **Hồ sơ của tôi** hoặc chi tiết giảng viên, chọn **Lập lý lịch / Chỉnh sửa lý lịch**, bổ sung các mục và thêm/xóa từng dòng. Có thể lưu dần các mục, nhưng dòng đã thêm cần đủ các trường có dấu *. Bấm **Hủy thay đổi** để trở về dữ liệu đã lưu. Chỉ hiện thông báo thành công sau khi API lưu thành công.

Các bảng chưa khai hiển thị “Chưa kê khai”. Thông tin chuyên môn, lĩnh vực, hướng nghiên cứu và tóm tắt cũ vẫn được giữ trong mục tổng hợp, không tự suy diễn thành dòng đào tạo/đề tài/công bố. Các bảng này là thông tin tự kê khai theo mẫu, không tự xác nhận là sản phẩm đã được duyệt và không tính thêm vào thống kê sản phẩm.

**In / Lưu PDF** sử dụng hộp thoại in của trình duyệt để in bản đã lưu gồm bốn phần theo mẫu và khu vực ký xác nhận. Không phải chức năng xuất Word giống nguyên bản về bố cục.

## Quy tắc dữ liệu và quyền

- Giảng viên sửa lý lịch của mình; Phòng QLKH/Quản trị toàn trường sửa theo quyền hiện tại. Trưởng bộ môn, trưởng khoa, Ban giám hiệu chỉ xem theo phạm vi sẵn có. API kiểm tra quyền trước khi xử lý dữ liệu.
- Năm từ 1900 đến 2100; năm nhận học vị, bổ nhiệm, tốt nghiệp và công bố không ở tương lai. Năm bắt đầu đề tài không ở tương lai; năm hoàn thành có thể là dự kiến và phải từ năm bắt đầu.
- Ngày kết thúc công tác phải từ ngày bắt đầu; ngày công tác, cấp giấy tờ và kê khai không ở tương lai. Để trống ngày kết thúc công tác nếu đang làm việc; để trống năm hoàn thành nếu chưa xác định.
- Tối đa 100 dòng cho mỗi bảng đào tạo, ngoại ngữ, công tác; 500 dòng cho mỗi bảng đề tài, công bố. API kiểm tra giới hạn ký tự, các dòng null, danh sách null và các trường bắt buộc của dòng.
- Không bắt buộc khai đủ toàn bộ lý lịch trong một lần lưu. Thông tin này là tự kê khai; lưu lý lịch không đồng nghĩa với xét duyệt hoặc xác minh.

## API và lưu trữ

Giữ nguyên GET/PUT `/api/lecturers/me/scientific-profile` và `/api/lecturers/{id}/scientific-profile`. Thêm đối tượng `details` bên cạnh bốn trường tổng hợp và `updatedAt`.

```json
{
  "expertise": "Kỹ thuật phần mềm",
  "researchFields": null,
  "researchDirections": null,
  "activitySummary": null,
  "details": {
    "birthPlace": "TP. Hồ Chí Minh",
    "education": [{ "level": "TIEN_SI", "institution": "ĐHQG", "specialization": "CNTT", "graduationYear": 2020, "thesisTitle": "Học máy" }],
    "languages": [{ "name": "Tiếng Anh", "proficiency": "B2" }],
    "employment": [{ "fromDate": "2020-01-01", "toDate": null, "organization": "Trường Đại học", "position": "Giảng viên" }],
    "researchProjects": [{ "title": "Ứng dụng AI", "startYear": 2024, "endYear": null, "level": "Bộ", "responsibility": "Chủ nhiệm" }],
    "publications": [{ "title": "Mô hình học máy", "year": 2023, "journal": "Tạp chí CNTT" }],
    "declarationPlace": "TP. Hồ Chí Minh",
    "declarationDate": "2024-01-01"
  }
}
```

PUT có `details` thay thế toàn bộ phần chi tiết; gửi `{}` để xóa chi tiết hoặc mảng rỗng để xóa các dòng của một bảng trong đối tượng thay thế. Bỏ qua `details` hoặc gửi null giữ nguyên chi tiết để client Mobile cũ không xóa nhầm. Bốn trường tổng hợp vẫn theo hợp đồng PUT cũ: trường bỏ qua trở thành null. Hồ sơ chưa lập trả `updatedAt: null`, `details` có các trường rỗng và các danh sách rỗng. Validation mô hình trả HTTP 400; vi phạm quy tắc thời gian/dữ liệu ở service trả 422.

Chi tiết được lưu vào cột JSONB `nckh.ly_lich_khoa_hoc.chi_tiet`. Cần áp dụng script trước khi chạy API phiên bản mới trên database có sẵn:

```powershell
Get-Content -Raw -Encoding UTF8 infra/database/08_scientific_profile_details.sql | docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
```

Thay tài khoản/database tương ứng nếu môi trường có cấu hình riêng. Script chạy lặp lại an toàn và giữ dữ liệu cũ; database mới được Docker Compose tự khởi tạo thêm cột này. Không xóa volume PostgreSQL để cập nhật schema.
