BEGIN;
SET LOCAL search_path TO nckh, public;
ALTER TABLE san_pham ADD COLUMN IF NOT EXISTS so_luong_quy_doi numeric(12,4) NOT NULL DEFAULT 1 CHECK (so_luong_quy_doi > 0);
ALTER TABLE san_pham_phien_ban_ung_dung ADD COLUMN IF NOT EXISTS so_luong_quy_doi numeric(12,4) NOT NULL DEFAULT 1;
CREATE TABLE IF NOT EXISTS nckh.cau_hinh_nghiep_vu (ma text PRIMARY KEY, gia_tri text NOT NULL);
DO $$ BEGIN
IF EXISTS (SELECT 1 FROM nckh.cau_hinh_nghiep_vu WHERE ma='faculty_workflow_2023') THEN RETURN; END IF;
-- Preserve removed role assignments for recovery without granting old global access.
CREATE TABLE IF NOT EXISTS nckh.phan_quyen_luu_tru AS SELECT *, CURRENT_TIMESTAMP AS luu_luc FROM phan_quyen WHERE false;
INSERT INTO nckh.phan_quyen_luu_tru SELECT *,CURRENT_TIMESTAMP FROM phan_quyen WHERE vai_tro IN ('PHONG_QLKH','BAN_GIAM_HIEU');
DELETE FROM phan_quyen WHERE vai_tro IN ('PHONG_QLKH','BAN_GIAM_HIEU');
UPDATE chi_tieu_nckh SET trang_thai='DA_TIEP_NHAN', nguoi_tiep_nhan_id=nguoi_giao_id, tiep_nhan_luc=CURRENT_TIMESTAMP
WHERE trang_thai='CHO_TIEP_NHAN';
INSERT INTO nckh.cau_hinh_nghiep_vu VALUES ('faculty_workflow_2023','enabled');
END $$;
CREATE TEMP TABLE new_rates(type_code text, code text, name text, hours numeric, unit_note text) ON COMMIT DROP;
INSERT INTO new_rates VALUES
('DE_TAI','DT_NHA_NUOC','Đề tài, dự án cấp Nhà nước và tương đương',3516,'năm thực hiện được duyệt'),
('DE_TAI','DT_BO_TINH','Đề tài, dự án cấp Bộ/Tỉnh/Thành phố và tương đương',2344,'năm thực hiện được duyệt'),
('DE_TAI','DT_VUON_UOM','Đề tài thuộc Chương trình Vườn ươm Sáng tạo KH&CN trẻ TP.HCM',1758,'đề tài'),
('DE_TAI','DT_CAP_TRUONG','Đề tài cấp Trường và tương đương',1172,'đề tài'),
('DE_TAI','DT_NHOM_TRUONG','Nhóm nghiên cứu cấp Trường',1172,'năm thực hiện được duyệt'),
('DE_TAI','DT_SANG_KIEN','Sáng kiến kinh nghiệm cấp Trường được giao và nghiệm thu',586,'sáng kiến'),
('BAI_BAO','BB_ISI_Q1','Bài báo quốc tế ISI Q1',2051,'bài'),
('BAI_BAO','BB_ISI_Q2','Bài báo quốc tế ISI Q2',1758,'bài'),
('BAI_BAO','BB_ISI_KHAC','Bài báo quốc tế ISI còn lại',1465,'bài'),
('BAI_BAO','BB_SCOPUS','Bài báo quốc tế Scopus',1172,'bài'),
('BAI_BAO','BB_ISSN_HDGS','Tạp chí ISSN thuộc HĐGSNN, tạp chí nước ngoài ISSN hoặc Tạp chí Khoa học Công nghệ và Thực phẩm',879,'bài'),
('BAI_BAO','BB_ISSN','Tạp chí trong nước có ISSN',586,'bài'),
('HOI_NGHI_HOI_THAO','HN_ISI','Toàn văn kỷ yếu hội nghị quốc tế ISI',1465,'bài'),
('HOI_NGHI_HOI_THAO','HN_SCOPUS','Toàn văn kỷ yếu hội nghị quốc tế Scopus',1172,'bài'),
('HOI_NGHI_HOI_THAO','HN_ISBN','Toàn văn kỷ yếu hội nghị quốc gia/quốc tế có ISBN',879,'bài'),
('HOI_NGHI_HOI_THAO','HN_KHONG_TOAN_VAN','Báo cáo hội nghị quốc gia/quốc tế không có kỷ yếu toàn văn',293,'bài'),
('HOI_NGHI_HOI_THAO','HN_TRONG_NUOC','Toàn văn kỷ yếu hội nghị trong nước có ISBN',586,'bài'),
('HOI_NGHI_HOI_THAO','HN_CAP_TRUONG','Toàn văn kỷ yếu hội thảo cấp Trường không ISBN tại HUIT',176,'bài'),
('HOI_NGHI_HOI_THAO','HN_CAP_KHOA','Toàn văn kỷ yếu hội thảo cấp khoa không ISBN tại HUIT',88,'bài; tối đa 120 giờ/giảng viên/năm học'),
('SACH','SACH_CHUYEN_KHAO','Sách chuyên khảo được NXB ấn hành và Hội đồng cấp Trường xác nhận',2930,'quyển'),
('SACH','SACH_NUOC_NGOAI','Sách, giáo trình hoặc chương sách chuyên ngành bằng tiếng nước ngoài được Hội đồng cấp Trường xác nhận',2344,'quyển'),
('SACH','SACH_THAM_KHAO','Sách tham khảo được NXB ấn hành và Hội đồng cấp Trường xác nhận',586,'quyển'),
('SACH','SACH_GIAO_TRINH','Giáo trình nghiệm thu cấp Trường, xuất bản hoặc lưu hành nội bộ',1758,'quyển'),
('SACH','SACH_THUC_HANH','Tài liệu thực hành/bài tập nghiệm thu cấp Trường, xuất bản hoặc lưu hành nội bộ',879,'quyển'),
('SACH','SACH_DICH','Dịch sách, tài liệu do Trường giao nhiệm vụ',6,'trang bản gốc');
INSERT INTO phan_loai_san_pham(loai_san_pham_id,ma_phan_loai,ten_phan_loai)
SELECT l.id,'QD2023_'||n.code,n.name FROM new_rates n JOIN loai_san_pham l ON l.ma_loai=n.type_code
ON CONFLICT (loai_san_pham_id,ma_phan_loai) DO NOTHING;
INSERT INTO quy_dinh_quy_doi(loai_san_pham_id,ma_quy_dinh,phien_ban,ten_quy_dinh,mo_ta_dieu_kien,muc_quy_doi,don_vi,do_uu_tien,mo_ta_quy_tac_tac_gia,hieu_luc_tu,hieu_luc_den,van_ban_can_cu,phan_loai_id,cach_chia_tac_gia)
SELECT l.id,'QD2023_'||n.code,1,n.name,'Đơn vị: '||n.unit_note,n.hours,'GIO',300,
'Hệ số tác giả do khoa cấu hình; bảng ảnh chưa quy định cách chia tác giả.',DATE '2023-01-01',NULL,
'Bảng 8 quy đổi NCKH năm 2023 trong tài liệu chỉnh sửa do người dùng cung cấp',p.id,'HE_SO'
FROM new_rates n JOIN loai_san_pham l ON l.ma_loai=n.type_code JOIN phan_loai_san_pham p ON p.loai_san_pham_id=l.id AND p.ma_phan_loai='QD2023_'||n.code
ON CONFLICT DO NOTHING;
-- Keep old classifications for historical products; new entries use the 2023 categories.
UPDATE phan_loai_san_pham SET dang_su_dung=false WHERE ma_phan_loai NOT LIKE 'QD2023_%';
UPDATE loai_san_pham SET dang_su_dung=(ma_loai IN ('BAI_BAO','DE_TAI','SACH','HOI_NGHI_HOI_THAO'));
-- Carry existing configured author coefficients forward; the supplied table specifies base hours only.
INSERT INTO he_so_tac_gia_quy_doi(quy_dinh_id,vai_tro_tac_gia,he_so)
SELECT DISTINCT ON (new.id,c.vai_tro_tac_gia) new.id,c.vai_tro_tac_gia,c.he_so
FROM quy_dinh_quy_doi new JOIN quy_dinh_quy_doi old ON old.loai_san_pham_id=new.loai_san_pham_id
JOIN he_so_tac_gia_quy_doi c ON c.quy_dinh_id=old.id
WHERE new.ma_quy_dinh LIKE 'QD2023_%' AND old.ma_quy_dinh NOT LIKE 'QD2023_%'
ORDER BY new.id,c.vai_tro_tac_gia,old.do_uu_tien DESC,old.phien_ban DESC,old.id
ON CONFLICT DO NOTHING;
-- Only migrate unambiguous classifications. Ambiguous records remain visible as issues.
WITH mappings(old_code,new_code) AS (VALUES
 ('CAP_NHA_NUOC','DT_NHA_NUOC'),('CAP_BO_TINH','DT_BO_TINH'),
 ('CAP_TRUONG_AP_DUNG','DT_CAP_TRUONG'),('CAP_TRUONG_KHONG_AP_DUNG','DT_CAP_TRUONG'),
 ('SCOPUS','BB_SCOPUS'),('ISSN_HOIGS','BB_ISSN_HDGS'),
 ('KY_YEU_QUOC_TE_ISI','HN_ISI'),('KY_YEU_QUOC_TE_SCOPUS','HN_SCOPUS'),
 ('SACH_CHUYEN_KHAO','SACH_CHUYEN_KHAO'),('SACH_THAM_KHAO','SACH_THAM_KHAO'),
 ('GIAO_TRINH_CAP_TRUONG','SACH_GIAO_TRINH'),('GIAO_TRINH_THUC_HANH','SACH_THUC_HANH'))
UPDATE san_pham s SET phan_loai_id=pnew.id
FROM phan_loai_san_pham pold JOIN mappings m ON m.old_code=pold.ma_phan_loai
JOIN phan_loai_san_pham pnew ON pnew.ma_phan_loai='QD2023_'||m.new_code AND pnew.loai_san_pham_id=pold.loai_san_pham_id
WHERE s.phan_loai_id=pold.id AND s.ngay_cong_bo >= DATE '2023-01-01';
COMMIT;
