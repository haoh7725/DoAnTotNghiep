-- Đồng bộ câu chữ hiển thị sau khi Trưởng khoa tiếp nhận vai trò nghiệp vụ cao nhất.
-- Không xóa vai trò hoặc trạng thái legacy vì chúng vẫn cần để đọc dữ liệu cũ.

UPDATE nckh.thong_bao
SET noi_dung = replace(
    replace(
        replace(noi_dung, 'Phòng Quản lý khoa học', 'Trưởng khoa'),
        'Phòng QLKH', 'Trưởng khoa'),
    'QLKH', 'Trưởng khoa')
WHERE noi_dung ILIKE '%QLKH%'
   OR noi_dung ILIKE '%Phòng Quản lý khoa học%';

UPDATE nckh.lich_su_xet_duyet_san_pham
SET ghi_chu = replace(
    replace(
        replace(ghi_chu, 'Phòng Quản lý khoa học', 'Trưởng khoa'),
        'Phòng QLKH', 'Trưởng khoa'),
    'QLKH', 'Trưởng khoa')
WHERE ghi_chu ILIKE '%QLKH%'
   OR ghi_chu ILIKE '%Phòng Quản lý khoa học%';

UPDATE nckh.phan_hoi_san_pham_ung_dung
SET noi_dung = replace(
    replace(
        replace(noi_dung, 'Phòng Quản lý khoa học', 'Trưởng khoa'),
        'Phòng QLKH', 'Trưởng khoa'),
    'QLKH', 'Trưởng khoa')
WHERE noi_dung ILIKE '%QLKH%'
   OR noi_dung ILIKE '%Phòng Quản lý khoa học%';

UPDATE nckh.quy_dinh_quy_doi
SET mo_ta_dieu_kien = replace(
    replace(
        replace(mo_ta_dieu_kien, 'Phòng Quản lý khoa học', 'Trưởng khoa'),
        'Phòng QLKH', 'Trưởng khoa'),
    'QLKH', 'Trưởng khoa')
WHERE mo_ta_dieu_kien ILIKE '%QLKH%'
   OR mo_ta_dieu_kien ILIKE '%Phòng Quản lý khoa học%';
