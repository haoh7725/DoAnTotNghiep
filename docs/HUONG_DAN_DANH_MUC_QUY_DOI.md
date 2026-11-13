# Danh mục nhóm, phân loại và quy đổi NCKH

## Sử dụng

1. Đăng nhập Admin hoặc Phòng QLKH, mở **Quy định quy đổi**.
2. Mở tab **Danh mục nhóm và phân loại**. Chọn nhóm hiện có hoặc thêm nhóm mới bằng mã ổn định và tên hiển thị.
3. Thêm phân loại trong nhóm, ví dụ nhóm Bài báo có phân loại “Bài tạp chí Scopus”. Mã phân loại không đổi sau khi tạo.
4. Mở tab **Quy định quy đổi**, chọn nhóm và phân loại; nhập mức gốc của một công trình, đơn vị Giờ hoặc Điểm, văn bản căn cứ và thời gian hiệu lực.
5. Chọn cách chia: hệ số từng vai trò, hoặc công thức 1/3 tác giả chính và 2/3 chia đều cho mọi tác giả.
6. Nhập điều kiện bổ sung nếu cần. Tất cả điều kiện phải đồng thời thỏa; phần mô tả và minh chứng là nội dung để người duyệt kiểm tra, không tự biến thành điều kiện tính.
7. Giảng viên chọn nội dung kế hoạch và phân loại khi khai báo sản phẩm. Khai báo số tác giả ngoài trường riêng; đồng tác giả trong hệ thống được chọn ở danh sách đồng tác giả.
8. Sau khi công trình được công nhận, Phòng QLKH chọn đánh giá theo **Giờ NCKH** hoặc **Điểm NCKH**, xem mức quy đổi, hệ số và căn cứ trước khi chốt. Mỗi lần đánh giá dùng một phương thức.

Danh mục tạp chí/hội nghị là danh mục **nơi công bố cụ thể**, không phải nhóm hay phân loại quy đổi. Admin/QLKH tự nhập tên nơi công bố và chọn hình thức Tạp chí/Hội nghị.

## Giờ và điểm

- Giờ và điểm là hai quy định riêng, không có phép chuyển đổi tự động giữa hai đơn vị.
- Tài liệu QĐ 136/2019 chỉ là căn cứ tham khảo cho giờ trong phạm vi đã đối chiếu. Không tự thiết lập thời gian hiệu lực 2026–2027 hoặc điểm NCKH nếu chưa có căn cứ được trường xác nhận.
- Dữ liệu mẫu cũ và mức điểm mẫu được giữ để bảo toàn các kết quả cũ; không phải khẳng định đó là quy chế hiện hành.
- Ưu tiên quy định phù hợp cùng phân loại. Nếu không có mức riêng phù hợp, dùng quy định chung của nhóm với đúng đơn vị đang đánh giá. Không có cả mức riêng lẫn mức chung phù hợp thì báo cần cấu hình, không tự gán mức giờ/điểm.
- Mỗi công trình chỉ chọn một mức cho mỗi đơn vị. Trong cùng phạm vi: ưu tiên cao hơn, rồi phiên bản cao hơn. Nếu vẫn có nhiều mức đồng hạng, hệ thống báo cấu hình xung đột.
- Không có quy định phù hợp cho đơn vị đã chọn sẽ chặn tạo đánh giá và nêu tên sản phẩm. Chứng nhận chưa có quy định phù hợp chỉ được lưu trong hồ sơ, không tự tính giờ/điểm và không cản đánh giá các sản phẩm khác.
- Định mức giờ và điểm có thể cùng được lưu, nhưng xếp loại chỉ dùng phương thức được chọn cho lần đánh giá. Thiếu điểm không cản một lần đánh giá theo giờ, và ngược lại.

## Bảo toàn lịch sử

- Nhóm và phân loại chỉ ngừng dùng, có thể dùng lại. Dữ liệu cũ tiếp tục hiển thị.
- Quy định đã được sử dụng trong bất kỳ lần đánh giá nào không được sửa/xóa. Dùng **Tạo phiên bản mới** khi thay đổi quy chế.
- Nên đặt khoảng hiệu lực của từng phiên bản rõ ràng và kiểm tra các khoảng chồng nhau.
- Các đánh giá đã chốt không được tự tính lại.

## Phạm vi hiện tại

Mức cấu hình mới là mức cho **một công trình**. Chưa tự nhân số năm của đề tài nhiều năm, chưa tự áp dụng ngoại lệ bài chờ đăng hoặc suy luận điều kiện như nhà xuất bản uy tín. Những nghiệp vụ này cần hồ sơ và quy trình xác nhận riêng trước khi triển khai; không nhập mức giờ/năm rồi coi là mức một lần cho đề tài.

API tự bổ sung schema quy đổi và các migration nghiệp vụ 17, 18 khi khởi động. Migration 18 khôi phục nhóm chứng nhận một lần và lưu phương thức đánh giá; kết quả cũ được đánh dấu là quy chế trước đây và giữ nguyên căn cứ. Không cần reset Docker/database. Khi cập nhật máy khác, build lại API và web.

## Kiểm thử đã thực hiện

- Bộ kiểm thử backend bao gồm chọn phương thức giờ/điểm, ưu tiên quy định riêng trước quy định chung, quản lý chứng nhận và bảo toàn dữ liệu. Công thức chia tác giả là lựa chọn cấu hình, không phải yêu cầu bắt buộc từ file Word tham khảo.
- Trên bản sao dữ liệu: QLKH tạo nhóm/phân loại/quy định thành công; giảng viên ghi danh mục bị chặn 403.
- Quy định cùng phạm vi bị chặn 409; quy định đã dùng đánh giá bị chặn sửa/xóa 409.
- Ngừng dùng phân loại thành công; sản phẩm mới dùng phân loại đó bị chặn 422.
- Phân loại chưa có quy định riêng được dùng quy định chung phù hợp; thiếu cả hai thì bị chặn đánh giá với thông báo cụ thể.
- Công trình 881 giờ, tác giả chính và 2 tác giả ngoài trường: tác giả chính được 489,4444 giờ; không sinh dòng điểm cho công trình khi chưa có quy định điểm.
