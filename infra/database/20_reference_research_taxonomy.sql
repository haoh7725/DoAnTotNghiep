SET search_path TO nckh, public;

-- Taxonomy aligned with the reference Decision 136/2019 summary.
WITH types AS (SELECT id, ma_loai FROM loai_san_pham),
items(ma_loai, ma_phan_loai, ten_phan_loai) AS (VALUES
 ('BAI_BAO','ISSN_HOIGS_1_00','Tạp chí ISSN thuộc HĐGS, điểm tối đa 1,0'),
 ('BAI_BAO','ISSN_HOIGS_0_75','Tạp chí ISSN thuộc HĐGS, điểm tối đa 0,75'),
 ('BAI_BAO','ISSN_HOIGS_0_50','Tạp chí ISSN thuộc HĐGS, điểm tối đa 0,5'),
 ('BAI_BAO','ISSN_HOIGS_0_25','Tạp chí ISSN thuộc HĐGS, điểm tối đa 0,25'),
 ('BAI_BAO','TAP_CHI_NUOC_NGOAI_PHAN_BIEN','Tạp chí nước ngoài có phản biện khoa học'),
 ('BAI_BAO','TAP_CHI_TRONG_NUOC_TOAN_VAN_PHAN_BIEN','Tạp chí trong nước đăng toàn văn, có phản biện khoa học'),
 ('HOI_NGHI_HOI_THAO','KY_YEU_QUOC_TE_PHAN_BIEN_NXB','Kỷ yếu hội thảo quốc tế có phản biện và nhà xuất bản uy tín'),
 ('HOI_NGHI_HOI_THAO','KY_YEU_TRONG_NUOC_PHAN_BIEN_NXB','Kỷ yếu hội thảo trong nước có phản biện và nhà xuất bản uy tín'),
 ('HOI_NGHI_HOI_THAO','BAO_CAO_QUOC_TE_PHAN_BIEN','Báo cáo hội thảo quốc tế có phản biện, không có kỷ yếu toàn văn'),
 ('HOI_NGHI_HOI_THAO','BAO_CAO_TRONG_NUOC_PHAN_BIEN','Báo cáo hội thảo trong nước có phản biện, không có kỷ yếu toàn văn'),
 ('SACH','GIAO_TRINH_NOI_BO_BIEN_SOAN_MOI','Giáo trình nội bộ, biên soạn mới'),
 ('SACH','GIAO_TRINH_NOI_BO_SUA_30','Giáo trình nội bộ, chỉnh sửa bổ sung tối thiểu 30%'),
 ('SACH','GIAO_TRINH_THUC_HANH_BIEN_SOAN_MOI','Giáo trình thực hành, biên soạn mới'),
 ('SACH','GIAO_TRINH_THUC_HANH_SUA_30','Giáo trình thực hành, chỉnh sửa bổ sung tối thiểu 30%')
)
INSERT INTO phan_loai_san_pham(loai_san_pham_id, ma_phan_loai, ten_phan_loai)
SELECT types.id, items.ma_phan_loai, items.ten_phan_loai
FROM items JOIN types USING (ma_loai)
ON CONFLICT (loai_san_pham_id, ma_phan_loai) DO UPDATE
SET ten_phan_loai = EXCLUDED.ten_phan_loai, dang_su_dung = true;

-- These generic legacy labels are retained for old records but are no longer offered for new data.
UPDATE phan_loai_san_pham
SET dang_su_dung = false
WHERE ma_phan_loai IN ('BAO_CAO_CO_PHAN_BIEN','GIAO_TRINH_NOI_BO','GIAO_TRINH_THUC_HANH');
