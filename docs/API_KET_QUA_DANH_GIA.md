# Hợp đồng API kết quả đánh giá

Tài liệu này chốt cấu trúc dữ liệu để Web và Mobile tích hợp trước khi công thức tính được triển khai trong tuần 3. JSON dùng camelCase, ngày giờ dùng ISO 8601 UTC và lỗi dùng ProblemDetails như các API hiện có.

## Endpoint dự kiến

| API | Quyền | Nội dung |
|---|---|---|
| `GET /api/evaluations/me?academicYearId={id}` | Giảng viên chính chủ | Kết quả mới nhất của tài khoản hiện tại trong năm học |
| `GET /api/evaluations/{id}` | Chính chủ hoặc quyền quản lý bao trùm đơn vị | Chi tiết kết quả và từng sản phẩm quy đổi |
| `GET /api/evaluations?academicYearId={id}&lecturerId={id}` | Phòng QLKH, quản trị hoặc quản lý trong phạm vi | Danh sách kết quả |

`GET /me` trả 404 khi tài khoản chưa liên kết giảng viên hoặc chưa có lần đánh giá. Mobile hiển thị trạng thái “Chưa có đánh giá” cho trường hợp này. Kết quả `NHAP` chỉ người đánh giá và quản trị được xem; giảng viên chỉ xem kết quả `CHOT`.

## Kết quả tóm tắt

```json
{
  "id": 25,
  "lecturerId": 7,
  "academicYearId": 3,
  "academicYearCode": "2026-2027",
  "attempt": 1,
  "status": "CHOT",
  "classification": "Hoàn thành tốt",
  "totalHours": 180.0,
  "totalPoints": 12.5,
  "evaluatedAt": "2026-10-15T08:30:00Z"
}
```

`status` nhận `NHAP` hoặc `CHOT`. `classification` có thể là `null` khi kết quả chưa chốt. `totalHours` và `totalPoints` luôn có giá trị, mặc định bằng 0 khi không có sản phẩm hợp lệ.

## Kết quả chi tiết

Kết quả chi tiết có toàn bộ trường của bản tóm tắt, thêm căn cứ xếp loại, ảnh chụp hồ sơ giảng viên và danh sách `details`:

```json
{
  "id": 25,
  "lecturerId": 7,
  "academicYearId": 3,
  "academicYearCode": "2026-2027",
  "attempt": 1,
  "status": "CHOT",
  "classification": "Hoàn thành tốt",
  "classificationBasis": "Đạt ngưỡng điểm và giờ NCKH của năm học",
  "lecturerCodeSnapshot": "GV001",
  "lecturerNameSnapshot": "Nguyễn Văn A",
  "departmentIdSnapshot": 3,
  "academicRankSnapshot": "Phó giáo sư",
  "degreeSnapshot": "Tiến sĩ",
  "totalHours": 180.0,
  "totalPoints": 12.5,
  "evaluatedAt": "2026-10-15T08:30:00Z",
  "details": [
    {
      "id": 91,
      "productId": 14,
      "productTitle": "Ứng dụng học máy trong dự báo",
      "productTypeId": 1,
      "productTypeName": "Bài báo",
      "conversionRuleId": 6,
      "ruleCode": "BB_Q1",
      "ruleVersion": 2,
      "baseValue": 200.0,
      "authorCoefficient": 0.6,
      "convertedValue": 120.0,
      "unit": "GIO",
      "calculationBasis": "Quy định BB_Q1 phiên bản 2; tác giả chính hệ số 0.6"
    }
  ]
}
```

Mỗi chi tiết lưu mã quy định, phiên bản, giá trị gốc, hệ số và căn cứ tính để Mobile có thể giải thích kết quả mà không phải tự tính lại. Mobile chỉ định dạng số và đơn vị từ dữ liệu API.

## Quy tắc tích hợp

- Mobile lấy `academicYearId` từ `GET /api/lookups` và `lecturerId` từ `GET /api/lecturers/me`.
- Không dùng trường `scoreEquivalent` của sản phẩm để thay cho tổng kết quả; kết quả chính thức lấy từ API đánh giá.
- Khi chưa có đánh giá hoặc kết quả chưa chốt, Mobile không suy đoán xếp loại.
- API phải kiểm tra chính chủ và phạm vi dữ liệu ở backend; ẩn nút trên giao diện không thay thế kiểm tra quyền.
