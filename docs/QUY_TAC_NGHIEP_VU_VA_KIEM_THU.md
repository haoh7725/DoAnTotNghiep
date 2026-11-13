# Quy tắc nghiệp vụ sau đợt sửa ngày 13/11/2026

Phạm vi bắt buộc theo ảnh đề cương CNTT-KLCN187 do người dùng cung cấp. File `quydinhquydoi.docx` là tài liệu tham khảo, không phải danh sách điều kiện phải triển khai toàn bộ. Mỗi lần đánh giá chọn Giờ NCKH hoặc Điểm NCKH, đúng với yêu cầu trong ảnh.

## Công nhận sản phẩm

- Mọi nhóm sản phẩm phải có ngày công bố/công nhận/nghiệm thu không ở tương lai và ít nhất một tệp minh chứng trước khi QLKH công nhận cuối cùng. Bài báo còn phải ở trạng thái đã xuất bản.
- Đây là kiểm soát tối thiểu. Người duyệt vẫn kiểm tra nội dung và tính hợp lệ của minh chứng. Chưa áp đặt danh sách giấy tờ riêng theo từng nhóm khi chưa có quy định của trường.
- Hồ sơ cũ đã duyệt nhưng thiếu ngày: QLKH/Admin mở chi tiết sản phẩm trên web, dùng **Bổ sung ngày công nhận còn thiếu**. Bắt buộc ghi lý do; hệ thống lưu lịch sử và phiên bản. Thao tác này không mở khóa nội dung học thuật và không cho đổi ngày đã tồn tại.
- Việc bổ sung ngày không tự tính lại các kết quả đã chốt. Tạo lần đánh giá mới khi cần áp dụng kỳ ghi nhận và dữ liệu đã bổ sung.

## Xung đột lợi ích khi xét duyệt

- Người dùng không được tự duyệt sản phẩm mà mình là tác giả chính hoặc đồng tác giả, kể cả Admin.
- Nếu một tác giả là Trưởng bộ môn của đơn vị chủ trì, hồ sơ bỏ qua bước bộ môn và chuyển Trưởng khoa.
- Nếu bước Trưởng khoa cũng có xung đột tác giả, hồ sơ chuyển QLKH. Người quản lý thuộc khoa/bộ môn khác không làm thay đổi luồng của đơn vị chủ trì.
- Danh sách chờ duyệt, quyền trên màn chi tiết và thông báo dùng chung bước xét duyệt mà API xác định. Thông báo xét duyệt không gửi cho các tác giả của hồ sơ.
- Nếu QLKH có xung đột, một cán bộ QLKH khác hoặc Admin không phải tác giả xử lý. Chưa triển khai cơ chế ủy quyền cá nhân riêng.

## Một công trình dùng chung một hồ sơ

- Có thêm trường **DOI / mã công trình**. DOI dạng URL và `doi:` được chuẩn hóa về cùng giá trị; mã không phân biệt hoa/thường.
- Database chặn hai hồ sơ có cùng mã, kể cả khi hai yêu cầu tạo chạy đồng thời.
- Khi gửi duyệt/công nhận, nếu thiếu định danh, hệ thống kiểm tra tên đã chuẩn hóa, ngày và nhóm với các công trình đã công nhận. Hai công trình có định danh rõ ràng khác nhau không bị gộp chỉ vì trùng tên.
- Công trình kê khai trùng phải được xử lý trước khi đánh giá, không cộng quy đổi cho hai bản ghi cùng định danh hoặc cùng nhóm/tên/ngày khi chưa có định danh.
- Tác giả phối hợp dùng một hồ sơ và danh sách đồng tác giả. Việc kiểm tra theo tên/ngày chỉ hỗ trợ dữ liệu chưa có định danh; chưa phát hiện được mọi trường hợp thay tên hoặc cố tình dùng mã khác.

## Năm ghi nhận

- Theo lựa chọn của chủ đồ án: đánh giá mới và số sản phẩm trong báo cáo tính theo ngày công bố/nghiệm thu nằm trong khoảng ngày của năm học.
- Ví dụ: kế hoạch 2025–2026, công bố ngày 10/11/2026, kết quả thuộc kỳ 2026–2027.
- Cam kết và tiến độ thực hiện kế hoạch vẫn theo kế hoạch gốc. Phân công bài báo là việc theo dõi hoàn thành nhiệm vụ được giao; không đồng nhất với kỳ tính giờ/điểm thi đua.
- Không cho tạo/sửa khoảng năm học chồng lấn qua API quản trị. Khoảng thời gian năm học đã có đánh giá bị khóa thay đổi.
- Lý lịch hiển thị năm theo ngày công bố khi có; sản phẩm cũ thiếu ngày không được tự suy thành một năm công bố.
- Kết quả đã chốt trước đợt sửa giữ nguyên số giờ/điểm. Báo cáo có thể cho thấy số sản phẩm hiện tại khác số sản phẩm quy đổi trong một kết quả cũ; cần tạo và chốt đánh giá mới để có kết quả theo chính sách mới. Không tự di chuyển hoặc xóa kết quả cũ.

## Giờ và điểm

- Tiếp tục dùng định mức và quy chế năm học đã cấu hình; không tự đặt mức quy đổi hoặc ngưỡng xếp loại mới.
- QLKH chọn Giờ NCKH hoặc Điểm NCKH khi tạo đánh giá; chỉ tính và so sánh định mức của phương thức đã chọn. Có định mức điểm không làm phát sinh yêu cầu điểm trong một lần đánh giá theo giờ.
- Quy định riêng theo phân loại phù hợp được ưu tiên trước quy định chung của nhóm. Nếu cả hai đều thiếu cho phương thức đã chọn, API yêu cầu cấu hình; không tự suy ra mức quy đổi từ Word.
- Chứng nhận được quản lý bình thường. Chứng nhận không có quy định phù hợp được giữ trong hồ sơ, không tự tính quy đổi và không chặn các sản phẩm nghiên cứu khác.
- Bản nháp cũ cần chọn giờ hoặc điểm trước khi chốt. Kết quả đã chốt trước migration 18 giữ nguyên phân loại/căn cứ và được hiển thị là quy chế trước đây; không tự tính lại.
- Không có sản phẩm đủ điều kiện trong kỳ vẫn có thể cho kết quả 0 theo quy chế hiện có; trường hợp này khác với thiếu quy định cho một sản phẩm đã có.

## Bảo toàn dữ liệu khi chạy lại

- Nếu đã có tài khoản đánh dấu dữ liệu mẫu `qlkh`, khởi động API không chạy lại các thao tác chỉnh hồ sơ, xóa đồng tác giả hay bổ sung kết quả demo. Dữ liệu người dùng đã sửa được giữ nguyên.
- Sửa điều kiện đếm nhắc hạn chỉ tiêu để tính cả bài tham gia với tư cách đồng tác giả, thống nhất với màn chỉ tiêu cá nhân.

## Cập nhật và kiểm thử

Schema bổ sung nằm ở `infra/database/17_product_business_rules.sql` và `18_project_requirements.sql`. API nhúng các script và tự chạy khi khởi động, có thể chạy lặp lại. Compose cũng gắn script cho database khởi tạo mới. Migration khôi phục chứng nhận một lần, không ghi đè lựa chọn ngừng dùng của quản trị viên ở các lần chạy sau. Không cần reset volume.

Build lại API và web trước khi sử dụng giao diện mới. Nên dùng database kiểm thử riêng cho các kịch bản tạo/duyệt/đánh giá; không chạy bộ kiểm thử tích hợp trên database đang demo.

```powershell
dotnet test backend/ResearchManagement.slnx --no-restore
```

Các kiểm thử không cần PostgreSQL chạy mặc định. Kiểm thử tích hợp `ProductBusinessWorkflowTests` chỉ chạy khi đặt `BUSINESS_TEST_DB` là connection string tới database tên bắt đầu bằng `research_business_test_`, đã khởi tạo lần lượt từ toàn bộ script SQL trong `infra/database`.

Kiểm thử tích hợp dùng mã dữ liệu riêng cho mỗi lần chạy và kiểm tra:

1. Đồng tác giả là Trưởng bộ môn được loại khỏi quyền duyệt; Trưởng khoa thấy hồ sơ và nhận thông báo.
2. Thiếu ngày hoặc thiếu minh chứng bị chặn công nhận; đủ điều kiện thì hoàn tất.
3. Trùng tên/ngày bị chặn gửi duyệt; DOI tương đương bị chặn ở database.
4. Công trình công bố khác năm kế hoạch xuất hiện trong đúng kỳ đánh giá và báo cáo.
5. Đánh giá theo giờ vẫn chốt được khi chưa có quy đổi điểm; đánh giá theo điểm dùng định mức điểm và hiển thị đúng tỷ lệ trên mobile. Thiếu quy định cho đơn vị được chọn thì báo lỗi.
6. QLKH bổ sung ngày cho hồ sơ cũ có lịch sử/phiên bản; tác giả không có quyền tự sửa.
7. Chạy lại seeder không thay đổi trạng thái, đồng tác giả và minh chứng đã có.
8. Chứng nhận có thể được thêm vào kế hoạch và khai báo sản phẩm; không có quy định quy đổi thì không cản đánh giá.

Web có kiểm thử render hồi quy `npm run test:profile` để xác nhận đề tài kê khai cũ, công bố thủ công và chứng nhận đã duyệt đều xuất hiện trong lý lịch.
