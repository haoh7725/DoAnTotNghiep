# Kịch bản kiểm thử theo thang điểm đề cương CNTT-KLCN187

Kịch bản này bám theo thang điểm trong đề cương chính thức và phiên bản nghiệp vụ hiện tại. Mục tiêu là chứng minh đủ yêu cầu trong khoảng 15-20 phút demo phần mềm, đồng thời chỉ rõ tài liệu nào phải mở để lấy điểm phân tích và thiết kế.

## 1. Phạm vi và dữ liệu dùng để test

### Tài khoản

| Tài khoản | Vai trò | Mật khẩu |
|---|---|---|
| `admin` | Quản trị viên | Mật khẩu quản trị đã cấu hình khi chạy dự án |
| `truongkhoa` | Trưởng khoa Công nghệ Thông tin | `Huit@2026!` |
| `tbm.cnpm` | Trưởng bộ môn Công nghệ phần mềm | `Huit@2026!` |
| `demo.gv2026` | Giảng viên demo, tài khoản sạch | `Huit@2026!` |
| `gv.danghaiyen` | Giảng viên không có cam kết nhưng đã được giao chỉ tiêu | `Huit@2026!` |

### Dữ liệu nhập thống nhất

- Năm học: `2026-2027`.
- Ngày lập kế hoạch: `16/11/2026`.
- Hạn kế hoạch: `30/06/2027`.
- Hạn chỉ tiêu: `30/06/2027`.
- Loại sản phẩm: `Bài báo`.
- Nơi công bố: `Journal of Educational Data`.
- Tên nội dung kế hoạch: `Bài báo ứng dụng AI giám sát tiến độ nghiên cứu khoa học`.
- Tên sản phẩm: `Mô hình AI cảnh báo chậm tiến độ nghiên cứu khoa học`.
- DOI/mã công trình: `DEMO-AI-NCKH-2026-001`.
- Ngày công bố dùng ở bước cuối: `15/11/2026`.
- Phân loại cuối: `Bài báo quốc tế Scopus` (`QD2023_BB_SCOPUS`).
- Minh chứng mẫu: `backend/src/ResearchManagement.Api/DemoEvidence/gritnet-student-performance.pdf`.

### Trạng thái ban đầu cần kiểm tra

Đăng nhập `demo.gv2026`:

- Không có kế hoạch năm `2026-2027`.
- Không có sản phẩm.
- Không có chỉ tiêu cá nhân.
- Tổng giờ bằng `0`.

Nếu dữ liệu không còn sạch, dùng một tài khoản giảng viên sạch khác hoặc khôi phục bộ dữ liệu `clean_operational_dataset_v4` trước khi demo.

## 2. Bảng đối chiếu thang điểm

| Mục chấm | Điểm | Minh chứng cần trình bày |
|---|---:|---|
| Lý do, mục tiêu đề tài | 0,25 | Mở phần Mở đầu của báo cáo/slide |
| Khảo sát hiện trạng: tổ chức, quy trình, biểu mẫu | 0,50 | Chương 1, sơ đồ tổ chức và quy trình QT-01 đến QT-08 |
| Kế hoạch, phân công nhóm | 0,25 | Bảng kế hoạch 12 tuần và bảng phân công thành viên |
| Use Case nghiệp vụ và đặc tả | 0,25 | Chương 2, Use Case nghiệp vụ và Activity Diagram |
| Use Case hệ thống và đặc tả | 0,25 | Chương 2, Use Case hệ thống và bảng đặc tả |
| Sơ đồ lớp phân tích | 0,25 | Chương 2, sơ đồ lớp phân tích |
| Sơ đồ lớp thiết kế | 0,25 | Chương 3, sơ đồ lớp thiết kế từng module |
| Mô hình dữ liệu | 0,25 | ERD và mô tả bảng dữ liệu |
| Thiết kế giao diện | 0,50 | Demo giao diện Web/Mobile, trạng thái rỗng/loading/error/thành công |
| API hệ thống | 0,25 | Swagger/API và luồng Web-Mobile dùng chung backend |
| Phân tích chức năng trên các nền tảng | 0,25 | Bảng phân chia chức năng Web/Mobile trong báo cáo |
| Đăng nhập, đăng xuất, quản trị người dùng, phân quyền | 0,50 | Ca TC-01 đến TC-03 |
| Hồ sơ NCKH | 0,25 | Ca TC-04 |
| Phân bổ chỉ tiêu | 0,25 | Ca TC-06 đến TC-08 |
| Kế hoạch đầu năm và trạng thái bài báo | 0,50 | Ca TC-05, TC-09, TC-10 |
| Quy đổi và đánh giá cuối năm | 0,50 | Ca TC-12 |
| Dashboard giám sát | 0,50 | Ca TC-08, TC-11 |
| Thống kê và Excel/PDF | 0,50 | Ca TC-13 |
| Mobile: hồ sơ | 0,25 | Ca TC-14 |
| Mobile: cập nhật trạng thái bài viết | 0,25 | Ca TC-15 |
| Mobile: nhắc hạn/phản hồi | 0,25 | Ca TC-16 |
| Mobile: tiến độ cá nhân/đơn vị | 0,25 | Ca TC-17 |
| Kiểm thử và triển khai | 0,75 | Ca TC-18 và lệnh kiểm thử tự động |

Các mục nội dung báo cáo, định dạng báo cáo, thái độ và phong cách trình bày do hội đồng đánh giá trực tiếp; phần mềm không thể tự chứng minh các điểm này.

## 3. Kịch bản kiểm thử chi tiết

### TC-01 - Đăng nhập, đăng xuất và kiểm soát phiên

**Mục điểm:** Chức năng cơ bản 0,50.

1. Mở `http://127.0.0.1:5173`.
2. Nhập sai mật khẩu cho `demo.gv2026`.
3. Kiểm tra hệ thống báo sai thông tin đăng nhập và không vào trang chính.
4. Đăng nhập đúng bằng `demo.gv2026 / Huit@2026!`.
5. Kiểm tra sidebar chỉ có chức năng giảng viên.
6. Bấm Đăng xuất; tải lại trang.

**Kỳ vọng:** đăng nhập sai bị từ chối, đăng nhập đúng tạo phiên, đăng xuất xóa phiên và API bảo vệ trả 401 khi không đăng nhập.

### TC-02 - Quản trị tài khoản và phân quyền

**Mục điểm:** Chức năng cơ bản 0,50; thiết kế giao diện 0,50.

1. Đăng nhập `admin`.
2. Vào **Quản trị → Dữ liệu nền → Tài khoản**.
3. Dùng ô tìm kiếm tìm `demo.gv2026`.
4. Mở **Phân quyền**, kiểm tra vai trò `GIANG_VIEN`, phạm vi `CA_NHAN`.
5. Không cần lưu nếu chỉ demo.
6. Đăng nhập `demo.gv2026`, thử mở URL/API quản trị.

**Kỳ vọng:** Quản trị viên tìm kiếm/phân trang được tài khoản; giảng viên bị chặn quyền quản trị.

### TC-03 - Định mức giờ theo năm học

**Mục điểm:** Quản trị dữ liệu nền, đánh giá cuối năm.

1. Dùng `admin`, vào **Quản trị → Năm học**.
2. Kiểm tra cột thông tin hiển thị `586 giờ NCKH` cho năm `2026-2027`.
3. Mở năm chưa có kết quả chốt: trường **Định mức giờ NCKH chung** được phép sửa.
4. Mở năm đã có kết quả chốt: trường định mức bị khóa và có thông báo lý do.

**Kỳ vọng:** định mức lưu theo từng năm, không lưu riêng từng giảng viên; kết quả lịch sử khóa căn cứ đã dùng.

### TC-04 - Hồ sơ và lý lịch khoa học

**Mục điểm:** Hồ sơ NCKH 0,25; Mobile hồ sơ 0,25.

1. Đăng nhập `demo.gv2026`.
2. Vào **Hồ sơ của tôi**.
3. Kiểm tra thông tin cá nhân và lý lịch khoa học.
4. Kiểm tra bốn ô sản phẩm: Đề tài, Bài báo, Hội nghị/hội thảo, Sách/giáo trình.
5. Với một giảng viên có sản phẩm, thử **Xuất Excel** và **Xuất PDF**.

**Kỳ vọng:** sản phẩm được công nhận tự động xuất hiện đúng nhóm; file tải được và mở được.

### TC-05 - Giảng viên lập kế hoạch đầu năm

**Mục điểm:** Kế hoạch đầu năm 0,50.

1. Đăng nhập `demo.gv2026`.
2. Vào **Nghiên cứu của tôi → Kế hoạch**, chọn `2026-2027`.
3. Tạo kế hoạch:
   - Số sản phẩm cam kết: `1`.
   - Ngày lập: `16/11/2026`.
   - Hạn: `30/06/2027`.
4. Thêm nội dung:
   - Loại: `Bài báo`.
   - Tên: `Bài báo ứng dụng AI giám sát tiến độ nghiên cứu khoa học`.
   - Số lượng: `1`.
   - Hạn: `31/05/2027`.
5. Bấm **Đăng ký kế hoạch**.
6. Thử sửa kế hoạch sau đăng ký.

**Kỳ vọng:** tổng nội dung bằng cam kết; kế hoạch chuyển sang Đã đăng ký; kế hoạch đã đăng ký không được sửa trực tiếp.

### TC-06 - Trưởng khoa tạo chỉ tiêu riêng để demo

**Mục điểm:** Phân bổ chỉ tiêu 0,25.

1. Đăng nhập `truongkhoa`.
2. Vào **Chỉ tiêu và phân bổ NCKH → Thêm chỉ tiêu**.
3. Nhập:
   - Khoa: Công nghệ Thông tin.
   - Năm: `2026-2027`.
   - Loại: `Bài báo`.
   - Nơi công bố liên kết: `Journal of Educational Data`.
   - Số sản phẩm: `1`.
   - Hạn: `30/06/2027`.
4. Lưu.

**Kỳ vọng:** chỉ tiêu mới không trùng chỉ tiêu bài báo chung vì có nơi công bố cụ thể; trạng thái sẵn sàng mở phân bổ.

### TC-07 - Trưởng khoa phân bổ xuống bộ môn

**Mục điểm:** Phân bổ chỉ tiêu 0,25; dashboard 0,50.

1. Mở chỉ tiêu vừa tạo.
2. Bấm **Mở phân bổ cho bộ môn**.
3. Phân bổ `1` cho Bộ môn Công nghệ phần mềm.
4. Kiểm tra tổng `1/1`, còn lại `0`.
5. Bấm **Khóa và chuyển cho bộ môn**.
6. Thử sửa phân bổ sau khi khóa.

**Kỳ vọng:** chỉ khóa được khi phân bổ đủ; sau khóa Trưởng khoa không sửa số phân bổ.

### TC-08 - Trưởng bộ môn giao chỉ tiêu cá nhân

**Mục điểm:** Phân bổ chỉ tiêu 0,25; dashboard 0,50.

1. Đăng nhập `tbm.cnpm`.
2. Mở chỉ tiêu bộ môn vừa nhận.
3. Chọn `DEMO2026 - ThS. Nguyễn Hoài Phương`.
4. Giao `1` sản phẩm.
5. Kiểm tra Trưởng bộ môn chỉ thấy phần phân bổ của CNPM, không thấy số toàn khoa.
6. Mở dashboard, kiểm tra tiến độ `0/1 = 0%`.

**Kỳ vọng:** giảng viên nhận thông báo; tổng phân công không vượt phần bộ môn nhận.

**Ca bổ sung:** mở dữ liệu `gv.danghaiyen` để chứng minh giảng viên không có cam kết vẫn được giao chỉ tiêu.

### TC-09 - Giảng viên tạo bài báo và tải minh chứng

**Mục điểm:** Hồ sơ NCKH 0,25; kế hoạch/trạng thái 0,50.

1. Đăng nhập `demo.gv2026`.
2. Mở **Nghiên cứu của tôi → Chỉ tiêu**; kiểm tra `0/1 = 0%`.
3. Mở **Sản phẩm → Thêm**.
4. Chọn nội dung kế hoạch bài báo.
5. Chọn chỉ tiêu `Journal of Educational Data` vừa được giao.
6. Nhập:
   - Tên: `Mô hình AI cảnh báo chậm tiến độ nghiên cứu khoa học`.
   - DOI/mã: `DEMO-AI-NCKH-2026-001`.
   - Nơi công bố: tự khóa theo chỉ tiêu.
   - Mô tả: `Mô hình hỗ trợ theo dõi và cảnh báo nguy cơ chậm tiến độ NCKH.`
7. Lưu.
8. Tải tệp `gritnet-student-performance.pdf`, mô tả `Minh chứng gửi bài và công bố`.

**Kỳ vọng:** bài báo bắt đầu ở Đang viết; chỉ tiêu vẫn 0%; minh chứng được lưu MinIO và tải lại được.

### TC-10 - Đề nghị cập nhật trạng thái bài báo

**Mục điểm:** Web/Mobile cập nhật trạng thái 0,50 + 0,25.

#### Bước 1: Đang viết → Đang phản biện

1. `demo.gv2026` mở sản phẩm, chọn **Đề nghị cập nhật trạng thái**.
2. Chọn minh chứng và gửi đề nghị.
3. Kiểm tra trạng thái vẫn là **Đang viết** khi đang chờ.
4. Đăng nhập `tbm.cnpm`, vào **Sản phẩm chờ duyệt**, mở đề nghị và xác nhận.
5. Kiểm tra trạng thái thành **Đang phản biện**.

#### Bước 2: Đang phản biện → Đã nhận xét

1. Giảng viên gửi đề nghị tiếp theo.
2. Đăng nhập `truongkhoa` và xác nhận.
3. Kiểm tra trạng thái **Đã nhận xét**.

#### Bước 3: Đã nhận xét → Đã xuất bản

1. Giảng viên gửi đề nghị cuối:
   - Ngày công bố: `15/11/2026`.
   - Nơi công bố: `Journal of Educational Data`.
2. Trưởng khoa xác nhận.
3. Kiểm tra trạng thái **Đã xuất bản**.

**Kỳ vọng:** không bỏ bước, không lùi bước, mỗi bài chỉ có một đề nghị chờ, có lịch sử người đề nghị/người duyệt/lý do.

### TC-11 - Gửi duyệt và công nhận sản phẩm

**Mục điểm:** Quản lý sản phẩm, dashboard, đánh giá.

1. `demo.gv2026` bấm **Gửi duyệt**.
2. `tbm.cnpm` vào **Sản phẩm chờ duyệt**, kiểm tra tác giả/minh chứng, bấm Phê duyệt.
3. `truongkhoa` mở sản phẩm:
   - Chọn phân loại `Bài báo quốc tế Scopus`.
   - Kiểm tra trạng thái Đã xuất bản, ngày và minh chứng.
   - Bấm công nhận.
4. `demo.gv2026` mở chỉ tiêu.

**Kỳ vọng:** sản phẩm thành Đã công nhận; tiến độ từ `0/1 = 0%` thành `1/1 = 100%`; giờ NCKH được quy đổi theo QD2023.

### TC-12 - Đánh giá cuối năm và đề xuất danh hiệu

**Mục điểm:** Đánh giá thi đua 0,50.

1. Đăng nhập `truongkhoa`.
2. Vào **Đánh giá thi đua**, chọn `2026-2027`.
3. Tìm `DEMO2026`.
4. Kiểm tra:
   - Tổng giờ.
   - Định mức chung của năm học.
   - Chỉ tiêu hoàn thành/được giao.
   - Kết quả Hoàn thành/Chưa hoàn thành.
5. Bấm **Chi tiết**, kiểm tra sản phẩm, số giờ và căn cứ quy đổi.
6. Quay lại danh sách, chọn khoa và bấm **Xem danh sách đề xuất**.

**Kỳ vọng:** đánh giá tạm tính tự động; top 10% hiển thị Lao động tiên tiến, nhóm tiếp theo đến hết top 30% hiển thị Chiến sĩ thi đua; danh sách này không được lưu.

### TC-13 - Dashboard, thống kê và xuất báo cáo

**Mục điểm:** Dashboard 0,50; thống kê/Excel/PDF 0,50.

1. `truongkhoa` mở Tổng quan:
   - Xem số chỉ tiêu đang quản lý.
   - Xem đã công nhận/được giao và biểu đồ theo bộ môn.
2. Đăng nhập `tbm.cnpm`, kiểm tra biểu đồ chỉ có phạm vi CNPM.
3. Vào **Thống kê kết quả**.
4. Lọc theo năm, bộ môn, học hàm, học vị.
5. Chuyển chiều tổng hợp: Bộ môn/Học hàm/Học vị.
6. Tải Excel và PDF.

**Kỳ vọng:** số liệu trên màn hình và file xuất thống nhất với bộ lọc; file mở được, có tiêu đề/cột dữ liệu chuẩn.

### TC-14 - Mobile tra cứu hồ sơ

**Mục điểm:** Mobile 0,25.

1. Chạy mobile với API của máy tính.
2. Đăng nhập `demo.gv2026`.
3. Mở Hồ sơ, kiểm tra lý lịch và sản phẩm đã công nhận.

**Kỳ vọng:** dữ liệu giống Web vì dùng chung API/PostgreSQL.

### TC-15 - Mobile đề nghị trạng thái bài báo

**Mục điểm:** Mobile 0,25.

1. Mở bài báo chưa ở bước cuối.
2. Bấm **Đề nghị cập nhật trạng thái**.
3. Chọn đúng bước kế tiếp và minh chứng.
4. Gửi đề nghị.
5. Đăng nhập đúng tài khoản quản lý để xác nhận hoặc trả lại.

**Kỳ vọng:** trạng thái chỉ đổi sau khi xác nhận; lịch sử và lý do trả lại hiển thị trên Mobile/Web.

### TC-16 - Mobile nhắc hạn và phản hồi

**Mục điểm:** Mobile 0,25.

1. Mở tab Thông báo.
2. Kiểm tra thông báo giao chỉ tiêu, phản hồi trả lại và nhắc hạn.
3. Chạm thông báo để mở đúng sản phẩm/chỉ tiêu.
4. Đánh dấu đã đọc và kiểm tra badge giảm.

**Kỳ vọng:** ứng dụng đang mở cập nhật badge mỗi 60 giây. Phải trình bày rõ phiên bản đồ án chưa có Firebase push khi ứng dụng tắt hoàn toàn.

### TC-17 - Mobile tiến độ cá nhân và đơn vị

**Mục điểm:** Mobile 0,25.

1. Chọn năm `2026-2027`.
2. Kiểm tra chỉ tiêu cá nhân, số đã liên kết, số hoàn thành và phần trăm.
3. Kiểm tra biểu đồ rút gọn cá nhân/bộ môn.
4. Kiểm tra tổng giờ và định mức năm học.

**Kỳ vọng:** tỷ lệ giống Web; số giờ và định mức lấy từ cùng API.

### TC-18 - Kiểm thử tự động và triển khai

**Mục điểm:** Kiểm thử và triển khai 0,75.

Chạy trước hội đồng hoặc chụp kết quả vào slide:

```powershell
dotnet test backend/tests/ResearchManagement.Api.Tests --no-restore
cd web
npm run build
cd ..\mobile
flutter analyze --no-pub
flutter test --no-pub
flutter build apk --debug --no-pub
```

Kiểm tra container:

```powershell
docker compose --env-file .env -f infra/compose.yaml ps
```

**Kỳ vọng:** backend test đạt; Web build thành công; Flutter analyze/test/build đạt; PostgreSQL, MinIO và API healthy/running.

## 4. Ca lỗi bắt buộc nên trình diễn

Chỉ cần chọn 4-6 ca tiêu biểu để tránh kéo dài buổi demo.

1. Đăng nhập sai mật khẩu → 401.
2. Giảng viên mở màn Quản trị → 403.
3. Tạo kế hoạch thứ hai cùng năm → bị từ chối.
4. Đăng ký kế hoạch chưa có nội dung hoặc tổng nội dung không khớp cam kết → bị từ chối.
5. Phân bổ vượt chỉ tiêu khoa hoặc giao vượt phần bộ môn → bị từ chối.
6. Sản phẩm liên kết chỉ tiêu khác loại/nơi công bố → bị từ chối hoặc không xuất hiện trong danh sách chọn.
7. Bài báo bỏ qua bước trạng thái hoặc gửi hai đề nghị cùng lúc → bị từ chối.
8. Xóa minh chứng đã dùng trong đề nghị → bị từ chối.
9. Tác giả tự duyệt sản phẩm/đề nghị của mình → bị từ chối.
10. Công nhận sản phẩm thiếu ngày, minh chứng, phân loại hoặc bài báo chưa xuất bản → bị từ chối.
11. DOI/mã công trình trùng → bị từ chối.
12. Sửa định mức năm học đã có kết quả chốt → bị khóa.

## 5. Thứ tự demo rút gọn 15 phút

| Thời gian | Thao tác |
|---:|---|
| 1 phút | Giới thiệu mục tiêu, kiến trúc Web-Mobile-API-PostgreSQL-MinIO |
| 1 phút | Admin: tài khoản, phân quyền, năm học và định mức |
| 2 phút | Giảng viên: hồ sơ và đăng ký kế hoạch |
| 2 phút | Trưởng khoa tạo/phân bổ/khóa chỉ tiêu |
| 1 phút | Trưởng bộ môn giao chỉ tiêu cá nhân |
| 3 phút | Giảng viên tạo bài, minh chứng và đề nghị trạng thái; quản lý xác nhận |
| 2 phút | Duyệt sản phẩm qua Trưởng bộ môn và Trưởng khoa; xem tiến độ 100% |
| 1,5 phút | Đánh giá năm, định mức và đề xuất danh hiệu |
| 1 phút | Dashboard, bộ lọc, Excel/PDF |
| 0,5 phút | Mobile và kết quả kiểm thử tự động |

## 6. Checklist trước ngày bảo vệ

- [ ] Docker Desktop, PostgreSQL, MinIO và API đang chạy.
- [ ] Vite đang chạy tại `http://127.0.0.1:5173`.
- [ ] Điện thoại/emulator truy cập được API.
- [ ] `demo.gv2026` đang sạch hoặc đã chuẩn bị sẵn đúng bước cần demo.
- [ ] Có tệp PDF minh chứng dưới 20 MB.
- [ ] Ghi nhớ bốn tài khoản chuyển vai trò.
- [ ] Excel, PDF và APK mở được.
- [ ] Slide có Use Case, Activity, lớp phân tích, lớp thiết kế và ERD.
- [ ] Slide cuối có ảnh kết quả test/build.
- [ ] Có phương án dự phòng: video quay luồng chính và database backup.

## 7. Lưu ý khi trình bày khác biệt so với đề cương gốc

- Đề cương ghi giảng viên cập nhật trạng thái bài viết. Hệ thống cho giảng viên gửi đề nghị trên Web/Mobile; trạng thái chỉ đổi sau khi quản lý xác nhận minh chứng. Đây là kiểm soát nghiệp vụ bổ sung.
- Đề cương có Phòng QLKH; quy trình khảo sát cập nhật dùng Trưởng khoa là cấp nghiệp vụ cao nhất.
- Đánh giá hiện tại dùng giờ NCKH và định mức chung theo năm học; năm `2026-2027` mặc định 586 giờ.
- Mobile nhắc hạn khi ứng dụng hoạt động; chưa có push hệ điều hành khi ứng dụng bị tắt hoàn toàn.

