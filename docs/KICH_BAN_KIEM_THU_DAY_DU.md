# Kịch bản kiểm thử đầy đủ đồ án

Kịch bản này chạy trên database đã reset hiện tại. Mật khẩu của các tài khoản demo là `Huit@2026!`. Năm dùng trong kịch bản là `2026-2027` (ID hiện tại: `1`), thời gian năm học từ 01/08/2026 đến 31/07/2027.

Kịch bản tạo một chỉ tiêu mới gắn **Hội nghị FAIR**, nên không sửa chỉ tiêu chung 27 bài đã có. Nếu muốn chạy lại từ đầu, reset riêng volume PostgreSQL theo hướng dẫn README; không xóa volume MinIO nếu muốn giữ minh chứng.

## Tài khoản

| Tài khoản | Vai trò | Dùng ở bước |
|---|---|---|
| `demo.gv2026` | Giảng viên demo, ThS. Nguyễn Hoài Phương, mã DEMO2026 | Hồ sơ, kế hoạch, chỉ tiêu, sản phẩm, mobile |
| `qlkh` | Phòng QLKH | Tạo chỉ tiêu, duyệt cuối, tạo/chốt đánh giá, báo cáo |
| `truongkhoa` | Trưởng khoa CNTT | Tiếp nhận, công bố, phân bổ và khóa chỉ tiêu; duyệt cấp khoa |
| `tbm.cnpm` | Trưởng bộ môn Công nghệ phần mềm | Phân công cá nhân; duyệt cấp bộ môn |

## A. Kiểm tra ban đầu

Đăng nhập `demo.gv2026`.

1. Vào **Hồ sơ của tôi**. Kiểm tra mã `DEMO2026`, đơn vị Công nghệ phần mềm, lý lịch đào tạo, công tác, đề tài và công bố.
2. Vào **Tổng quan công việc**. Chọn năm học `2026-2027`. Vì tài khoản này chưa có kế hoạch năm đó, phần việc cần làm phải hướng tới lập kế hoạch.
3. Vào **Nghiên cứu của tôi → Chỉ tiêu**. Chưa có chỉ tiêu cá nhân.

## B. Giảng viên lập kế hoạch và cam kết

Vẫn dùng `demo.gv2026`.

1. Vào **Nghiên cứu của tôi → Kế hoạch**; chọn `2026-2027`.
2. Tạo kế hoạch:
   - Số sản phẩm cam kết: `1`.
   - Ngày lập: `13/11/2026`.
   - Hạn hoàn thành: `31/05/2027`.
3. Thêm nội dung:
   - Loại: **Bài báo**.
   - Tên: `Ứng dụng AI hỗ trợ giám sát tiến độ nghiên cứu khoa học`.
   - Số lượng: `1`.
   - Hạn nội dung: `31/05/2027`.
4. Kiểm tra tổng số lượng bằng số cam kết `1`.
5. Nhấn **Đăng ký kế hoạch**.

Kỳ vọng: trạng thái kế hoạch là **Đã đăng ký**. Giảng viên có thể tự đăng ký sản phẩm theo kế hoạch; chỉ tiêu là nhiệm vụ được giao bổ sung.

## C. Phòng QLKH tạo chỉ tiêu cho khoa

Đăng nhập `qlkh`.

1. Vào **Chỉ tiêu và phân bổ NCKH → Thêm chỉ tiêu**.
2. Nhập:
   - Khoa: **Khoa Công nghệ Thông tin**.
   - Năm học: `2026-2027`.
   - Nơi công bố: **Hội nghị FAIR**.
   - Số bài: `1`.
   - Hạn: `30/06/2027`.
3. Lưu.

Kỳ vọng: trạng thái **Chờ Trưởng khoa tiếp nhận**; QLKH chưa được tự phân bổ cho bộ môn.

## D. Trưởng khoa tiếp nhận, phân bổ và khóa

Đăng nhập `truongkhoa`.

1. Mở **Thông báo**, kiểm tra chỉ tiêu Hội nghị FAIR.
2. Vào **Chỉ tiêu và phân bổ NCKH**, mở chỉ tiêu mới.
3. Nhấn **Tiếp nhận chỉ tiêu**.
4. Nhấn **Mở phân bổ cho bộ môn**.
5. Chọn **Bộ môn Công nghệ phần mềm**, nhập `1` bài, lưu.
6. Kiểm tra tổng phân bổ `1/1`, còn lại `0`.
7. Nhấn **Khóa và chuyển cho bộ môn**.

Kỳ vọng: trạng thái **Đã khóa**. Sau khóa, Trưởng khoa không sửa phân bổ được.

## E. Trưởng bộ môn giao cho giảng viên

Đăng nhập `tbm.cnpm`.

1. Mở thông báo chỉ tiêu đã khóa.
2. Mở chỉ tiêu → **Chỉ tiêu bộ môn được nhận → Xem / phân công**.
3. Chọn `DEMO2026 – ThS. Nguyễn Hoài Phương`.
4. Kiểm tra căn cứ:
   - Cam kết bài báo: `1`.
   - Đã giao: `0`.
   - Có thể giao thêm: `1`.
5. Giao `1` bài.

Kỳ vọng: giảng viên nhận thông báo; không thể giao `2` bài vì vượt cam kết và chỉ tiêu bộ môn.

## F. Giảng viên khai báo sản phẩm

Đăng nhập `demo.gv2026`.

1. Vào **Nghiên cứu của tôi → Chỉ tiêu**. Kiểm tra `1 bài`, hoàn thành `0`, còn `1`.
2. Vào **Nghiên cứu của tôi → Sản phẩm → + Thêm**.
3. Chọn nội dung kế hoạch của năm `2026-2027`.
4. Chọn chỉ tiêu Hội nghị FAIR.
5. Nhập thông tin cơ bản:
   - Tên: `Mô hình AI cảnh báo chậm tiến độ nghiên cứu khoa học`.
   - Trạng thái bài viết: **Đã xuất bản**.
   - Ngày công bố: `10/11/2026`.
   - Nơi công bố: chỉ tiêu Hội nghị FAIR được chọn.
6. Mở **Thông tin bổ sung**:
   - DOI/mã công trình: `DEMO-FAIR-2026-001`.
   - Phân loại: để QLKH xác nhận hoặc chọn phân loại phù hợp.
   - Mô tả: `Mô hình hỗ trợ theo dõi và cảnh báo nguy cơ chậm tiến độ nghiên cứu khoa học.`
7. Lưu thông tin.
8. Mở chi tiết sản phẩm → thêm đồng tác giả nếu cần → tải PDF minh chứng, mô tả `Bản toàn văn bài báo`.
9. Gửi duyệt.

Kỳ vọng: sản phẩm là **Đã gửi**, tiến độ chỉ tiêu vẫn `0%`.

## G. Duyệt sản phẩm qua ba cấp

### G1. Trưởng bộ môn

Đăng nhập `tbm.cnpm` → **Sản phẩm chờ duyệt** → mở sản phẩm → **Phê duyệt**.

Ghi chú: `Sản phẩm phù hợp kế hoạch và chỉ tiêu bộ môn.`

### G2. Trưởng khoa

Đăng nhập `truongkhoa` → mở cùng sản phẩm → **Phê duyệt**.

Ghi chú: `Khoa thống nhất đề nghị công nhận.`

### G3. Phòng QLKH

Đăng nhập `qlkh` → mở sản phẩm → **Phê duyệt**.

Kiểm tra ngày công bố và minh chứng. Có thể nhập điểm thẩm định `20` nếu muốn kiểm tra trường này; điểm này không thay thế kết quả quy đổi giờ/điểm thi đua. Nhập ghi chú `Minh chứng và thông tin công bố hợp lệ.` rồi xác nhận.

Kỳ vọng: sản phẩm **Đã duyệt**, đúng ba bước; chỉ tiêu lúc này tăng lên `1/1` và `100%`.

## H. Kiểm tra trả lại và bỏ liên kết

Tạo một sản phẩm nháp thứ hai trong cùng kế hoạch.

1. Chưa gửi duyệt, mở chi tiết → **Bỏ liên kết chỉ tiêu**.
2. Kiểm tra sản phẩm không còn giữ suất chỉ tiêu.
3. Thêm lại liên kết hoặc xóa sản phẩm tùy mục đích demo.
4. Với sản phẩm đã gửi, Trưởng bộ môn chọn **Yêu cầu bổ sung**, bắt buộc nhập lý do.
5. Giảng viên mở thông báo, sửa sản phẩm, bổ sung minh chứng rồi gửi lại.

Kỳ vọng: sản phẩm Nháp/Trả lại mới được bỏ liên kết; sản phẩm đã duyệt không thể sửa nội dung.

## I. Tạo và chốt đánh giá theo giờ

Đăng nhập `qlkh`.

1. Vào **Đánh giá thi đua**.
2. Chọn giảng viên `DEMO2026`, năm `2026-2027`, phương thức **Giờ NCKH**.
3. Nhấn **Tạo đánh giá**.
4. Kiểm tra sản phẩm đã duyệt xuất hiện trong chi tiết với quy đổi giờ.
5. Nhấn **Tự động xếp loại và chốt**.

Với định mức demo `200 giờ` và quy đổi bài báo `120 giờ`, kết quả kỳ vọng là `60%` và xếp loại **Đạt** nếu ngưỡng Đạt là 50%.

## J. Tạo và chốt đánh giá theo điểm

Tạo lần đánh giá tiếp theo cho cùng giảng viên/năm, chọn **Điểm NCKH**.

1. Kiểm tra chi tiết chỉ lấy dòng quy đổi điểm.
2. Chốt kết quả.
3. Kết quả hiển thị theo định mức điểm, không dùng định mức giờ.

Với định mức demo `100 điểm` và quy đổi `20 điểm`, tỷ lệ kỳ vọng là `20%`, thường là **Chưa đạt** nếu quy chế có ngưỡng thấp nhất 50%. Đây là cách kiểm tra hệ thống phân biệt hai phương thức.

## K. Báo cáo và quyền xem

Đăng nhập `qlkh`:

1. Vào **Thống kê kết quả**.
2. Chọn `2026-2027`.
3. Kiểm tra giảng viên, sản phẩm, giờ, điểm, xếp loại.
4. Xuất Excel và PDF.

Đăng nhập `truongkhoa`:

1. Xem tiến độ chỉ tiêu khoa/bộ môn.
2. Không tạo hoặc chốt đánh giá.

Đăng nhập `tbm.cnpm`:

1. Xem chỉ tiêu bộ môn và phân công cá nhân.
2. Không sửa chỉ tiêu cấp khoa.

Đăng nhập `demo.gv2026`:

1. Chỉ xem kết quả đánh giá của chính mình.
2. Không thấy màn quản lý quy đổi, phân bổ hoặc báo cáo toàn khoa.

## L. Hồ sơ và mobile

Trên web, `demo.gv2026`:

1. Vào **Hồ sơ của tôi**.
2. Kiểm tra ba tab **Thông tin cá nhân**, **Lý lịch khoa học**, **Công trình đã công nhận**.
3. Chỉnh sửa một dòng đề tài thủ công, lưu, tải lại và kiểm tra dữ liệu vẫn còn.

Trên mobile:

1. Chạy `flutter run --dart-define=API_URL=http://10.0.2.2:8081/api` trên Android Emulator.
2. Đăng nhập `demo.gv2026`.
3. Kiểm tra hồ sơ, sản phẩm đã duyệt, thông báo và tiến độ chỉ tiêu.
4. Mở kết quả đánh giá; kiểm tra phương thức Giờ/Điểm và tỷ lệ tương ứng.

## M. Các ca lỗi bắt buộc nên trình diễn

1. Giao 2 bài cho giảng viên chỉ cam kết 1 → bị từ chối.
2. Chọn chỉ tiêu chung nhưng sản phẩm có nơi công bố cụ thể → được chấp nhận.
3. Chọn chỉ tiêu cụ thể nhưng sản phẩm khác nơi công bố → bị từ chối.
4. Liên kết bài sau hạn chỉ tiêu → bị từ chối; gia hạn phải do QLKH ghi lý do.
5. Duyệt cuối khi thiếu ngày hoặc minh chứng → bị từ chối.
6. Tác giả tự duyệt sản phẩm → bị từ chối/chuyển cấp.
7. Khai báo trùng DOI/mã công trình → bị từ chối.
8. Tạo đánh giá theo giờ và theo điểm → hai tỷ lệ dùng đúng định mức riêng.

## N. Thứ tự demo rút gọn

1. `demo.gv2026`: hồ sơ và kế hoạch.
2. `qlkh`: tạo chỉ tiêu.
3. `truongkhoa`: tiếp nhận, phân bổ, khóa.
4. `tbm.cnpm`: giao cho giảng viên.
5. `demo.gv2026`: khai báo sản phẩm, minh chứng, gửi duyệt.
6. `tbm.cnpm` → `truongkhoa` → `qlkh`: duyệt ba cấp.
7. `demo.gv2026`: kiểm tra tiến độ 0% → 100%.
8. `qlkh`: đánh giá theo giờ, chốt; tạo lần khác theo điểm.
9. `qlkh`/`truongkhoa`: báo cáo, Excel/PDF.
10. Mobile: hồ sơ, sản phẩm, thông báo, tiến độ, thi đua.
