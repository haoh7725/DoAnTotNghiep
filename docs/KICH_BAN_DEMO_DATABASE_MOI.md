# KỊCH BẢN DEMO CHI TIẾT – DATABASE VẬN HÀNH MỚI

## 1. Mục tiêu kịch bản

Kịch bản này dùng cho database mới đã được reset ngày 10/11/2026. Dữ liệu hiện có:

- 2 khoa, 8 bộ môn.
- 45 tài khoản, 32 giảng viên.
- 3 năm học: 2024–2025, 2025–2026 và 2026–2027.
- 80 kế hoạch nghiên cứu, 155 sản phẩm khoa học.
- 6 chỉ tiêu cấp khoa, 24 phân bổ bộ môn, 80 phân công giảng viên.
- 54 kết quả thi đua đã chốt, 138 thông báo.
- Có đủ trạng thái sản phẩm: Nháp, Đã nộp, Trưởng bộ môn duyệt, Trưởng khoa duyệt, Phòng QLKH duyệt, Trả lại và Đã duyệt.
- Có đủ trạng thái bài báo: Đang viết, Đang phản biện, Đã nhận đăng và Đã xuất bản.

Database mới mô phỏng một hệ thống đã vận hành nhiều năm. Vì vậy **không tạo thêm chỉ tiêu 2026–2027**, do cả hai khoa đã có chỉ tiêu và đã khóa phân bổ. Kịch bản sẽ trình bày dữ liệu hiện hữu và thực hiện một luồng xét duyệt thật trên sản phẩm của giảng viên Lê Thanh Sơn.

---

## 2. Tài khoản sử dụng

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Quản trị viên | `admin` | `change_me` |
| Phòng QLKH | `qlkh` | `Huit@2026!` |
| Ban giám hiệu | `bgh` | `Huit@2026!` |
| Trưởng khoa CNTT | `truongkhoa` | `Huit@2026!` |
| Trưởng khoa Khoa học Dữ liệu | `truongkhoa.khdl` | `Huit@2026!` |
| Trưởng bộ môn CNPM | `tbm.cnpm` | `Huit@2026!` |
| Trưởng bộ môn HTTT | `tbm.httt` | `Huit@2026!` |
| Giảng viên Nguyễn Minh Anh | `gv.nguyenminhanh` | `Huit@2026!` |
| Giảng viên Lê Thanh Sơn | `gv.lethanhson` | `Huit@2026!` |

Ngoài ra còn có các Trưởng bộ môn `tbm.attt`, `tbm.khmt`, `tbm.mmt`, `tbm.kpdl`, `tbm.ptdl`, `tbm.ttnt` và các tài khoản giảng viên `seed.gv09` đến `seed.gv32`, tất cả dùng mật khẩu `Huit@2026!`.

### Tài khoản chính của kịch bản

**Nguyễn Minh Anh** dùng để demo dữ liệu ổn định:

- Thuộc Bộ môn Công nghệ phần mềm.
- Cam kết 2 sản phẩm.
- Được giao 2 bài báo.
- Có 1 bài báo đã duyệt và xuất bản.
- Tiến độ chỉ tiêu hiện tại: 1/2, tương đương 50%.
- Có lý lịch khoa học chi tiết để demo web và mobile.

**Lê Thanh Sơn** dùng để thao tác trực tiếp:

- Thuộc Bộ môn Công nghệ phần mềm.
- Cam kết 2 sản phẩm.
- Được giao 1 bài báo.
- Bài báo đã liên kết chỉ tiêu và đã được Trưởng bộ môn duyệt.
- Chưa có kết quả đánh giá thi đua năm 2026–2027.
- Sau khi hoàn tất kịch bản, tiến độ sẽ chuyển từ 0/1 sang 1/1, đạt 100%.

---

## 3. Bước 0 – Chuẩn bị trước khi demo

1. Mở Docker Desktop và bảo đảm Docker đang chạy.
2. Mở web tại: `http://localhost:5173`.
3. Mở Swagger khi cần chứng minh API: `http://localhost:8081/swagger`.
4. Nhấn `Ctrl + F5` để tải lại giao diện và xóa dữ liệu bộ nhớ đệm cũ.
5. Chuẩn bị file minh chứng:
   `C:\Users\PC\Downloads\1.docx`.
6. Đăng xuất tài khoản đang mở trước khi bắt đầu.

### Kiểm tra nhanh dữ liệu trước khi trình bày

- Khoa CNTT năm 2026–2027: 27 bài, trạng thái **Đã khóa**.
- Khoa Khoa học Dữ liệu năm 2026–2027: 9 bài, trạng thái **Đã khóa**.
- Bộ môn Công nghệ phần mềm: 7 bài, đã giao đủ 7 bài cho 6 giảng viên.
- `gv.nguyenminhanh`: 2 bài được giao, hoàn thành 1 bài, tiến độ 50%.
- Sản phẩm của `gv.lethanhson`: **DevOps Adoption Model for University Software Projects**, trạng thái **Trưởng bộ môn đã duyệt**.

---

## 4. Bước 1 – Quản lý tài khoản và phân quyền 6 vai trò

### Đăng nhập

- Tài khoản: `admin`
- Mật khẩu: `change_me`

### Thao tác

1. Chọn **Quản trị dữ liệu nền**.
2. Mở tab **Tài khoản**.
3. Cho hội đồng xem danh sách 45 tài khoản.
4. Chọn một tài khoản và nhấn **Phân quyền**.
5. Mở danh sách vai trò để chỉ ra đủ 6 vai trò:
   - Quản trị viên.
   - Giảng viên.
   - Trưởng bộ môn.
   - Trưởng khoa.
   - Phòng QLKH.
   - Ban giám hiệu.
6. Giải thích phạm vi dữ liệu:
   - Giảng viên: phạm vi cá nhân.
   - Trưởng bộ môn: phạm vi một bộ môn.
   - Trưởng khoa: phạm vi một khoa.
   - Phòng QLKH, Ban giám hiệu và Quản trị viên: phạm vi toàn trường.
7. Không lưu thay đổi để tránh làm sai dữ liệu trình bày.

### Kết quả cần nói

> Hệ thống kiểm tra đồng thời vai trò và phạm vi đơn vị. Có quyền Trưởng bộ môn không có nghĩa là được xem dữ liệu của bộ môn khác; tương tự, Trưởng khoa chỉ quản lý khoa được phân công.

Đăng xuất.

---

## 5. Bước 2 – Phòng QLKH xem dữ liệu điều hành toàn trường

### Đăng nhập

- Tài khoản: `qlkh`
- Mật khẩu: `Huit@2026!`

### 2.1. Danh mục tạp chí/hội nghị

1. Chọn **Danh mục tạp chí/hội nghị**.
2. Cho xem bốn danh mục:
   - Journal of Educational Data – Tạp chí – SCOPUS – Q2.
   - Tạp chí Khoa học Giáo dục Việt Nam – Tạp chí – Trong nước – Uy tín.
   - Hội nghị FAIR – Hội nghị – Trong nước – Quốc gia.
   - International Conference on ICT – Hội nghị – SCOPUS – Quốc tế.
3. Mở form thêm hoặc sửa để trình bày các trường thông tin, nhưng không lưu.

### Giải thích

> Danh mục này quản lý nơi công bố bài báo. Đề tài, sách và chứng nhận là loại sản phẩm khoa học, được quản lý trong kế hoạch và sản phẩm, không phải một loại tạp chí/hội nghị.

### 2.2. Xem chỉ tiêu toàn trường

1. Chọn **Chỉ tiêu và phân bổ NCKH**.
2. Chọn năm học **2026–2027**.
3. Cho xem hai chỉ tiêu:
   - Khoa Công nghệ Thông tin: 27 bài, hạn 01/07/2027, đã khóa.
   - Khoa Khoa học Dữ liệu: 9 bài, hạn 01/07/2027, đã khóa.
4. Mở chỉ tiêu Khoa CNTT.
5. Chọn **Tổng quan tiến độ** để xem tỷ lệ hoàn thành toàn khoa và từng bộ môn.
6. Chọn **Phân bổ theo bộ môn** để xem:
   - An toàn thông tin: 3 bài.
   - Công nghệ phần mềm: 7 bài.
   - Hệ thống thông tin: 7 bài.
   - Khoa học máy tính: 7 bài.
   - Mạng máy tính: 3 bài.
7. Chọn **Theo dõi phân công giảng viên** để xem phân công cá nhân.

### Kết quả cần nói

> Dữ liệu hiện tại đã đi hết luồng: Phòng QLKH giao cho khoa, Trưởng khoa tiếp nhận và chia xuống bộ môn, sau đó khóa để Trưởng bộ môn giao cho giảng viên. Khi đã khóa, không cấp nào được tùy ý sửa lại số lượng, bảo đảm dữ liệu lịch sử không thay đổi.

Đăng xuất.

---

## 6. Bước 3 – Trưởng khoa giám sát chỉ tiêu và phạm vi khoa

### Đăng nhập

- Tài khoản: `truongkhoa`
- Mật khẩu: `Huit@2026!`

### Thao tác

1. Chọn **Chỉ tiêu và phân bổ NCKH**.
2. Mở chỉ tiêu Khoa CNTT năm 2026–2027.
3. Tại **Tổng quan tiến độ**, chỉ ra:
   - Chỉ tiêu khoa: 27 bài.
   - Đã phân bổ: 27 bài.
   - Còn chưa phân bổ: 0 bài.
   - Trạng thái: Đã khóa – Bộ môn thực hiện.
4. Mở **Phân bổ theo bộ môn** và cho xem năm bộ môn thuộc CNTT.
5. Mở **Theo dõi phân công giảng viên**.
6. Nhấn xem Bộ môn Công nghệ phần mềm:
   - Chỉ tiêu bộ môn: 7 bài.
   - Đã giao cá nhân: 7 bài.
   - Sáu giảng viên được phân công.
7. Giải thích Trưởng khoa chỉ theo dõi phân công cá nhân, không giao thay Trưởng bộ môn.

### Kiểm chứng phạm vi

1. Quan sát danh sách chỉ tiêu.
2. Chỉ tiêu của Khoa Khoa học Dữ liệu không được phép chỉnh sửa bởi Trưởng khoa CNTT.

### Kết quả cần nói

> Trưởng khoa chịu trách nhiệm nhận chỉ tiêu và phân bổ xuống bộ môn. Sau khi khóa, Trưởng bộ môn mới được giao cho giảng viên; Trưởng khoa chuyển sang vai trò giám sát.

Đăng xuất.

---

## 7. Bước 4 – Trưởng bộ môn xem phân công theo cam kết

### Đăng nhập

- Tài khoản: `tbm.cnpm`
- Mật khẩu: `Huit@2026!`

### Thao tác

1. Chọn **Chỉ tiêu và phân bổ NCKH**.
2. Mở chỉ tiêu CNTT năm 2026–2027.
3. Chọn **Chỉ tiêu bộ môn được nhận**.
4. Tại Bộ môn Công nghệ phần mềm, nhấn **Xem / phân công**.
5. Mở phần **Phân công giảng viên**.
6. Cho hội đồng xem bảng căn cứ phân công:
   - Cam kết bài báo.
   - Đã giao.
   - Có thể giao thêm.
7. Chỉ ra các dòng quan trọng:
   - Nguyễn Minh Anh: đã giao 2 bài trên cam kết 2, không thể giao thêm.
   - Trần Ngọc Lan: đã giao 1 bài.
   - Lê Thanh Sơn: đã giao 1 bài.
8. Chỉ ra tổng bộ môn: 7/7 bài, vì vậy không thể giao thêm.

### Kết quả cần nói

> Hệ thống kiểm soát đồng thời hai giới hạn: tổng phân công không vượt chỉ tiêu bộ môn và số giao cho mỗi giảng viên không vượt cam kết bài báo. Ngày phân công được hệ thống ghi tự động.

Đăng xuất.

---

## 8. Bước 5 – Giảng viên xem hồ sơ, kế hoạch, sản phẩm và chỉ tiêu

### Đăng nhập

- Tài khoản: `gv.nguyenminhanh`
- Mật khẩu: `Huit@2026!`

### 5.1. Hồ sơ khoa học

1. Chọn **Hồ sơ của tôi**.
2. Cho xem:
   - Thông tin cá nhân, học hàm và học vị.
   - Lĩnh vực chuyên môn.
   - Hướng nghiên cứu.
   - Quá trình đào tạo.
   - Quá trình công tác.
   - Ngoại ngữ.
   - Đề tài và công bố tiêu biểu.
3. Giải thích cùng một hồ sơ được dùng trên web và mobile.

### 5.2. Kế hoạch đầu năm

1. Chọn **Kế hoạch & Tiến độ**.
2. Chọn năm học **2026–2027**.
3. Cho xem hai nội dung cam kết:
   - Bài báo về dự báo kết quả học tập.
   - Đề tài xây dựng trợ lý học tập thông minh.
4. Chỉ ra kế hoạch có tổng cam kết 2 sản phẩm.
5. Giải thích trạng thái kế hoạch được suy ra từ sản phẩm thực tế, giảng viên không tự chọn hoàn thành.

### 5.3. Sản phẩm khoa học

1. Chọn **Sản phẩm khoa học**.
2. Mở bài **Mô hình dự báo nguy cơ học vụ bằng học máy**.
3. Cho xem:
   - Loại: Bài báo.
   - Trạng thái nghiệp vụ: Đã duyệt.
   - Trạng thái bài viết: Đã xuất bản.
   - Ngày công bố: 15/10/2026.
   - Đã liên kết chỉ tiêu.
   - Lịch sử xét duyệt nhiều cấp.
   - Thông tin đồng tác giả và minh chứng nếu có.

### 5.4. Chỉ tiêu cá nhân

1. Chọn **Chỉ tiêu của tôi**.
2. Kết quả phải thấy:
   - Được giao: 2 bài.
   - Đã hoàn thành: 1 bài.
   - Còn lại: 1 bài.
   - Tiến độ: 50%.
3. Nhấn vào sản phẩm đã liên kết để chuyển đến chi tiết sản phẩm.

### Câu giải thích

> Chỉ bài báo đã liên kết chỉ tiêu, đã được duyệt và có trạng thái đã xuất bản mới được tính hoàn thành. Vì vậy sản phẩm nháp hoặc đang xét duyệt không làm tăng tỷ lệ.

Đăng xuất.

---

## 9. Bước 6 – Luồng thao tác trực tiếp: trả lại và hoàn thiện bài báo

Luồng này dùng sản phẩm của Lê Thanh Sơn để tạo thay đổi thật ngay trong buổi demo.

### Sản phẩm sử dụng

- Giảng viên: CNTT005 — TS. Lê Thanh Sơn.
- Tên đăng nhập: `gv.lethanhson`.
- Tên sản phẩm: **DevOps Adoption Model for University Software Projects**.
- Trạng thái ban đầu: **Trưởng bộ môn đã duyệt**.
- Trạng thái bài viết ban đầu: **Đang phản biện**.
- Sản phẩm đã liên kết với chỉ tiêu CNTT 2026–2027.

### 6.1. Trưởng khoa yêu cầu bổ sung

Đăng nhập `truongkhoa` / `Huit@2026!`.

1. Chọn **Sản phẩm chờ duyệt**.
2. Tìm sản phẩm **DevOps Adoption Model for University Software Projects**.
3. Nhấn **Xem và xét duyệt**.
4. Cho xem thông tin, lịch sử xét duyệt và trạng thái hiện tại.
5. Nhấn **Thực hiện xét duyệt**.
6. Chọn **Yêu cầu bổ sung** hoặc **Trả lại**.
7. Nhập lý do:

   `Vui lòng cập nhật trạng thái đã xuất bản, ngày công bố và bổ sung minh chứng toàn văn.`

8. Xác nhận trả lại.

Kết quả:

- Sản phẩm chuyển thành **Trả lại**.
- Giảng viên nhận thông báo kèm lý do.
- Sản phẩm được mở lại quyền chỉnh sửa cho chủ sở hữu.

Đăng xuất.

### 6.2. Giảng viên cập nhật sản phẩm

Đăng nhập `gv.lethanhson` / `Huit@2026!`.

1. Mở **Thông báo**.
2. Đọc thông báo sản phẩm bị trả lại.
3. Chọn **Sản phẩm khoa học**.
4. Mở **DevOps Adoption Model for University Software Projects**.
5. Nhấn **Chỉnh sửa**.
6. Cập nhật:

| Trường | Giá trị |
|---|---|
| Trạng thái bài viết | Đã xuất bản |
| Ngày công bố | 05/11/2026 |
| Nơi công bố | International Conference on ICT 2026 |
| Chỉ số tạp chí/hội nghị | SCOPUS |
| Phân loại | QUOC_TE |
| Mô tả | Nghiên cứu đề xuất mô hình áp dụng DevOps cho các dự án phần mềm trong trường đại học và đánh giá trên dữ liệu thực nghiệm. |

7. Lưu sản phẩm.
8. Tại mục **Minh chứng**:
   - Chọn file `C:\Users\PC\Downloads\1.docx`.
   - Mô tả: `Bản toàn văn và xác nhận công bố tại hội nghị`.
   - Nhấn **Tải lên**.
9. Nhấn **Gửi duyệt**.

Kết quả:

- Sản phẩm chuyển từ Trả lại sang Đã nộp/Chờ Trưởng bộ môn.
- Phiên bản mới được lưu.
- Lịch sử vẫn giữ lần trả lại trước đó.
- Chỉ tiêu cá nhân vẫn 0/1 vì sản phẩm chưa duyệt hoàn tất.

Đăng xuất.

### 6.3. Trưởng bộ môn duyệt lại

Đăng nhập `tbm.cnpm` / `Huit@2026!`.

1. Chọn **Sản phẩm chờ duyệt**.
2. Mở đúng sản phẩm.
3. Kiểm tra trạng thái đã xuất bản, ngày công bố và minh chứng mới.
4. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
5. Ghi chú:

   `Sản phẩm đã bổ sung đầy đủ thông tin công bố và phù hợp kế hoạch của bộ môn.`

6. Xác nhận.

Kết quả: sản phẩm chuyển sang chờ Trưởng khoa.

Đăng xuất.

### 6.4. Trưởng khoa phê duyệt

Đăng nhập `truongkhoa` / `Huit@2026!`.

1. Chọn **Sản phẩm chờ duyệt**.
2. Mở sản phẩm vừa được Bộ môn duyệt.
3. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
4. Ghi chú:

   `Thông tin bổ sung đạt yêu cầu, đồng ý chuyển Phòng QLKH thẩm định.`

5. Xác nhận.

Kết quả: sản phẩm chuyển sang Phòng QLKH.

Đăng xuất.

### 6.5. Phòng QLKH duyệt hai bước

Đăng nhập `qlkh` / `Huit@2026!`.

**Lần duyệt thứ nhất – thẩm định:**

1. Chọn **Sản phẩm chờ duyệt**.
2. Mở sản phẩm.
3. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
4. Ghi chú:

   `Minh chứng và thông tin công bố hợp lệ, chuyển bước công nhận kết quả.`

5. Xác nhận.

**Lần duyệt thứ hai – công nhận hoàn tất:**

1. Mở lại sản phẩm vừa thẩm định.
2. Nhấn **Thực hiện xét duyệt** → **Phê duyệt**.
3. Nếu giao diện yêu cầu điểm quy đổi, nhập `20`.
4. Ghi chú:

   `Công nhận sản phẩm bài báo năm học 2026-2027.`

5. Xác nhận.

Kết quả:

- Sản phẩm chuyển thành **Đã duyệt**.
- Trạng thái bài viết vẫn là **Đã xuất bản**.
- Ngày công bố nằm trong năm học 2026–2027.
- Sản phẩm đủ điều kiện tính hoàn thành chỉ tiêu và quy đổi thi đua.

---

## 10. Bước 7 – Kiểm chứng đồng bộ sau khi duyệt

Đăng nhập lại `gv.lethanhson` / `Huit@2026!`.

### 7.1. Sản phẩm khoa học

1. Mở **Sản phẩm khoa học**.
2. Sản phẩm phải hiển thị **Đã duyệt**.
3. Mở chi tiết để xem toàn bộ lịch sử:
   - Bộ môn duyệt lần đầu.
   - Trưởng khoa trả lại.
   - Giảng viên gửi lại.
   - Bộ môn duyệt lại.
   - Trưởng khoa duyệt.
   - Phòng QLKH thẩm định và công nhận.

### 7.2. Kế hoạch & Tiến độ

1. Mở **Kế hoạch & Tiến độ**.
2. Chọn năm học 2026–2027.
3. Nội dung bài báo phải được tính từ sản phẩm thực tế.
4. Kết quả đã hoàn thành phải tăng tương ứng.

### 7.3. Chỉ tiêu của tôi

1. Mở **Chỉ tiêu của tôi**.
2. Kết quả phải thấy:
   - Được giao: 1 bài.
   - Đã hoàn thành: 1 bài.
   - Còn lại: 0 bài.
   - Tiến độ: 100%.
3. Nút sản phẩm liên kết phải dẫn đến đúng sản phẩm vừa duyệt.

### Phần cần nhấn mạnh

```text
Cam kết đầu năm
      ↓
Sản phẩm thực tế
      ↓
Liên kết chỉ tiêu được giao
      ↓
Duyệt nhiều cấp + đã xuất bản
      ↓
Kế hoạch hoàn thành và chỉ tiêu đạt 100%
```

Đăng xuất.

---

## 11. Bước 8 – Quy đổi và đánh giá thi đua cuối năm

### Đăng nhập

- Tài khoản: `qlkh`
- Mật khẩu: `Huit@2026!`

### 8.1. Xem quy định quy đổi

1. Chọn **Quy định quy đổi**.
2. Cho xem các quy định:
   - Bài báo: 120 giờ.
   - Bài báo: 20 điểm.
   - Đề tài: 200 giờ.
   - Sách: 100 giờ.
   - Chứng nhận: 50 giờ.
3. Chỉ ra:
   - Loại sản phẩm.
   - Phiên bản quy định.
   - Mức quy đổi.
   - Đơn vị giờ hoặc điểm.
   - Độ ưu tiên.
   - Khoảng thời gian hiệu lực.
   - Tiêu chí áp dụng.
   - Hệ số tác giả chính và đồng tác giả.

### Giải thích

> Hệ thống chọn quy định còn hiệu lực theo loại sản phẩm, điều kiện và độ ưu tiên. Giờ và điểm là hai đơn vị độc lập, được tổng hợp riêng trong đánh giá và báo cáo.

### 8.2. Tạo đánh giá cho Lê Thanh Sơn

1. Chọn **Đánh giá thi đua**.
2. Chọn năm học **2026–2027**.
3. Chọn giảng viên **CNTT005 — TS. Lê Thanh Sơn**.
4. Nhấn **Tạo đánh giá**.
5. Mở kết quả vừa tạo.

Kết quả cần thấy:

- Sản phẩm vừa duyệt xuất hiện trong chi tiết quy đổi.
- Có dòng quy đổi giờ và dòng quy đổi điểm.
- Có mã quy định, phiên bản, hệ số tác giả và căn cứ tính.
- Thông tin giảng viên, học hàm, học vị và bộ môn được lưu dạng ảnh chụp để báo cáo quá khứ không thay đổi.

### 8.3. Chốt đánh giá

1. Kiểm tra tổng giờ và tổng điểm.
2. Nhấn **Tự động xếp loại và chốt**.
3. Xác nhận thao tác.

Kết quả:

- Trạng thái chuyển từ Nháp sang Đã chốt.
- Xếp loại được suy ra theo tỷ lệ tổng giờ trên định mức.
- Kết quả sau chốt chỉ đọc.
- Trưởng khoa, Trưởng bộ môn và Ban giám hiệu chỉ xem theo phạm vi, không được sửa hoặc chốt thay Phòng QLKH.

---

## 12. Bước 9 – Dashboard, thống kê và xuất báo cáo

Vẫn dùng tài khoản `qlkh`.

### Dữ liệu nền trước khi thực hiện luồng Lê Thanh Sơn

| Năm học | Kết quả đã chốt | Tổng giờ | Tổng điểm |
|---|---:|---:|---:|
| 2024–2025 | 22 | 2.640 | 440 |
| 2025–2026 | 18 | 2.160 | 360 |
| 2026–2027 | 14 | 1.680 | 280 |

Sau khi tạo và chốt đánh giá cho Lê Thanh Sơn, số kết quả năm 2026–2027 sẽ tăng thêm một và tổng giờ/điểm sẽ cập nhật theo quy định được áp dụng.

### Thao tác

1. Chọn **Thống kê kết quả**.
2. Chọn:

| Bộ lọc | Giá trị |
|---|---|
| Năm học | 2026–2027 |
| Khoa | Khoa Công nghệ Thông tin |
| Bộ môn | Tất cả |

3. Nhấn **Xem báo cáo** hoặc chờ hệ thống tải dữ liệu.
4. Cho xem:
   - Tổng giảng viên.
   - Tổng sản phẩm.
   - Tổng giờ.
   - Tổng điểm.
   - Sản phẩm theo bộ môn.
   - Tỷ lệ giảng viên theo học vị.
   - Tổng giờ quy đổi theo bộ môn.
   - Tổng điểm quy đổi theo học hàm.
   - Danh sách chi tiết giảng viên.
5. Thử bộ lọc theo:
   - Bộ môn Công nghệ phần mềm.
   - Học vị Tiến sĩ.
   - Học hàm PGS nếu có.
6. Nhấn **Tải Excel**.
7. Nhấn **Tải PDF**.
8. Mở nhanh file đã tải để chứng minh xuất báo cáo thành công.

### Kết quả cần nói

> Báo cáo chỉ sử dụng kết quả đã chốt. Dữ liệu nháp không được đưa vào số liệu chính thức. Các bộ lọc web và dữ liệu trong Excel/PDF phải thống nhất.

Đăng xuất.

---

## 13. Bước 10 – Ban giám hiệu xem tổng hợp toàn trường

### Đăng nhập

- Tài khoản: `bgh`
- Mật khẩu: `Huit@2026!`

### Thao tác

1. Mở **Tổng quan toàn trường**.
2. Chọn năm học **2026–2027**.
3. Cho xem số liệu của cả hai khoa.
4. Lọc lần lượt:
   - Tất cả các khoa.
   - Khoa Công nghệ Thông tin.
   - Khoa Khoa học Dữ liệu.
   - Một bộ môn cụ thể.
   - Học vị Tiến sĩ.
5. Mở **Kết quả thi đua toàn trường**.
6. Mở một kết quả đã chốt để xem chi tiết.
7. Chỉ ra BGH không có nút tạo đánh giá, sửa hoặc chốt.

### Kết quả cần nói

> Ban giám hiệu có quyền theo dõi toàn trường và xuất báo cáo, nhưng không tham gia cập nhật nghiệp vụ. Điều này bảo đảm tách biệt quyền quản lý và quyền phê duyệt.

Đăng xuất.

---

## 14. Bước 11 – Demo thông báo và nhắc hạn

Có thể trình bày các loại thông báo đã có trong database:

1. Giao chỉ tiêu cho khoa.
2. Khóa phân bổ và chuyển cho bộ môn.
3. Giao chỉ tiêu cá nhân cho giảng viên.
4. Sản phẩm bị trả lại kèm phản hồi.
5. Sản phẩm được duyệt qua các cấp.
6. Nội dung kế hoạch sắp đến hạn.
7. Nội dung hoặc chỉ tiêu quá hạn.

### Kiểm tra bằng luồng vừa thao tác

1. Đăng nhập `gv.lethanhson`.
2. Mở **Thông báo**.
3. Chỉ ra thông báo trả lại từ Trưởng khoa và lý do bổ sung.
4. Chọn tab **Chưa đọc**, **Đã đọc** và **Tất cả**.
5. Đánh dấu một thông báo đã đọc.
6. Kiểm tra số badge chưa đọc thay đổi.

### Giải thích

> Thông báo nghiệp vụ được phát sinh đúng thời điểm chuyển trạng thái. Nhắc hạn tự động do dịch vụ nền kiểm tra định kỳ, không cần người quản trị bấm thủ công.

---

## 15. Bước 12 – Demo mobile Flutter

### Tài khoản đề nghị

- Tài khoản: `gv.nguyenminhanh`
- Mật khẩu: `Huit@2026!`

Tài khoản này được chọn vì có hồ sơ đầy đủ, sản phẩm đã duyệt và tiến độ 50%, giúp các biểu đồ mobile có dữ liệu trực quan.

### 12.1. Hồ sơ

1. Mở tab **Hồ sơ**.
2. Cho xem:
   - Thông tin cá nhân.
   - Học hàm, học vị.
   - Lĩnh vực và hướng nghiên cứu.
   - Quá trình đào tạo.
   - Công tác, ngoại ngữ, đề tài và công bố.
3. Đối chiếu với hồ sơ trên web.

### 12.2. Sản phẩm

1. Mở tab **Sản phẩm**.
2. Mở bài **Mô hình dự báo nguy cơ học vụ bằng học máy**.
3. Cho xem trạng thái Đã duyệt/Đã xuất bản.
4. Mở sản phẩm đang thực hiện nếu muốn trình bày cập nhật trạng thái bài viết.

### 12.3. Thông báo

1. Mở tab **Thông báo**.
2. Cho xem danh sách thông báo và badge chưa đọc.
3. Mở một thông báo nhắc hạn hoặc giao chỉ tiêu.
4. Đánh dấu đã đọc.

### 12.4. Tiến độ

1. Mở tab **Tiến độ**.
2. Chọn năm học 2026–2027.
3. Kết quả cá nhân:
   - Được giao: 2.
   - Hoàn thành: 1.
   - Còn lại: 1.
   - Tiến độ: 50%.
4. Cho xem biểu đồ so sánh tiến độ cá nhân với bộ môn.
5. Cho xem tiến độ sản phẩm theo kế hoạch.

### Kết quả cần nói

> Mobile dùng chung API và database với web. Dữ liệu giảng viên vừa xem trên web phải xuất hiện thống nhất trên điện thoại.

---

## 16. Đối chiếu nhanh 12 yêu cầu đồ án

| Yêu cầu | Phần demo chứng minh |
|---|---|
| 1. Người dùng và phân quyền | Bước 1 |
| 2. Hồ sơ NCKH | Bước 5.1 và Bước 12.1 |
| 3. Phân bổ chỉ tiêu | Bước 2, 3 và 4 |
| 4. Lập kế hoạch đầu năm | Bước 5.2 |
| 5. Đánh giá thi đua | Bước 8 |
| 6. Thông báo nhắc lịch | Bước 11 |
| 7. Dashboard giám sát | Bước 2, 3 và 9 |
| 8. Thống kê, Excel/PDF | Bước 9 |
| 9. Mobile tra cứu hồ sơ | Bước 12.1 |
| 10. Mobile cập nhật trạng thái bài viết | Bước 12.2 |
| 11. Mobile nhắc lịch | Bước 12.3 |
| 12. Mobile giám sát tiến độ | Bước 12.4 |

---

## 17. Thứ tự demo rút gọn trong 15–20 phút

Nếu thời gian bảo vệ ngắn, dùng thứ tự sau:

1. Admin: 6 vai trò và phạm vi – 1 phút.
2. QLKH: danh mục, chỉ tiêu hai khoa – 2 phút.
3. Trưởng khoa: dashboard và phân bổ CNTT – 2 phút.
4. Trưởng bộ môn: bảng cam kết/đã giao – 2 phút.
5. Nguyễn Minh Anh: hồ sơ, kế hoạch, sản phẩm, chỉ tiêu 50% – 3 phút.
6. QLKH: quy đổi, kết quả thi đua và báo cáo – 3 phút.
7. BGH: tổng quan toàn trường, quyền chỉ đọc – 2 phút.
8. Mobile: hồ sơ, sản phẩm, thông báo, biểu đồ tiến độ – 3 phút.

Luồng trả lại và duyệt sản phẩm của Lê Thanh Sơn cần thêm khoảng 7–10 phút. Chỉ thực hiện khi hội đồng yêu cầu xem thao tác thực tế.

---

## 18. Khôi phục database về trạng thái ban đầu sau khi tập demo

Chỉ thực hiện khi muốn xóa toàn bộ thay đổi phát sinh trong lúc tập. Thao tác này xóa database và file MinIO hiện tại rồi tạo lại dữ liệu chuẩn:

```powershell
docker compose --env-file .env -f infra/compose.yaml down -v
docker compose --env-file .env -f infra/compose.yaml up --build -d
```

Sau khi API hoàn tất khởi tạo, tải lại web bằng `Ctrl + F5` và đăng nhập lại. Không chạy lệnh reset ngay trước giờ bảo vệ nếu chưa kiểm tra lại web, API và mobile.
