SET search_path TO nckh, public;

-- Danh mục tham khảo. Chứng nhận tiếp tục được quản lý theo yêu cầu đồ án.
INSERT INTO loai_san_pham(ma_loai, ten_loai)
VALUES ('HOI_NGHI_HOI_THAO', 'Bài/kỷ yếu/báo cáo hội nghị, hội thảo')
ON CONFLICT (ma_loai) DO UPDATE
SET ten_loai = EXCLUDED.ten_loai;

WITH types AS (
    SELECT id, ma_loai FROM loai_san_pham
    WHERE ma_loai IN ('BAI_BAO', 'DE_TAI', 'SACH', 'HOI_NGHI_HOI_THAO')
), classifications(ma_loai, ma_phan_loai, ten_phan_loai) AS (
    VALUES
      ('BAI_BAO', 'SCI', 'Tạp chí quốc tế thuộc SCI'),
      ('BAI_BAO', 'SCI_E_SSCI_AHCI_ESCI', 'Tạp chí quốc tế thuộc SCI-E, SSCI, A&HCI hoặc ESCI'),
      ('BAI_BAO', 'SCOPUS', 'Tạp chí quốc tế thuộc Scopus'),
      ('BAI_BAO', 'ISSN_HOIGS', 'Tạp chí quốc gia có ISSN thuộc danh mục HĐGS'),
      ('BAI_BAO', 'ISSN_PHAN_BIEN', 'Tạp chí có ISSN và phản biện khoa học'),
      ('DE_TAI', 'CAP_NHA_NUOC', 'Đề tài cấp Nhà nước và tương đương'),
      ('DE_TAI', 'CAP_BO_TINH', 'Đề tài cấp Bộ, tỉnh, thành phố và tương đương'),
      ('DE_TAI', 'CAP_TRUONG_AP_DUNG', 'Đề tài cấp trường có đơn vị áp dụng thực tế'),
      ('DE_TAI', 'CAP_TRUONG_KHONG_AP_DUNG', 'Đề tài cấp trường không có địa chỉ ứng dụng'),
      ('HOI_NGHI_HOI_THAO', 'KY_YEU_QUOC_TE_ISI', 'Kỷ yếu hội thảo quốc tế thuộc ISI'),
      ('HOI_NGHI_HOI_THAO', 'KY_YEU_QUOC_TE_SCOPUS', 'Kỷ yếu hội thảo quốc tế thuộc Scopus'),
      ('HOI_NGHI_HOI_THAO', 'KY_YEU_QUOC_GIA_HOIGS', 'Kỷ yếu hội thảo quốc gia thuộc danh mục HĐGS'),
      ('HOI_NGHI_HOI_THAO', 'BAO_CAO_CO_PHAN_BIEN', 'Báo cáo hội thảo có phản biện'),
      ('SACH', 'SACH_CHUYEN_KHAO', 'Sách chuyên khảo được nhà xuất bản ấn hành'),
      ('SACH', 'SACH_THAM_KHAO', 'Sách tham khảo được nhà xuất bản ấn hành'),
      ('SACH', 'GIAO_TRINH_CAP_TRUONG', 'Giáo trình được hội đồng thông qua và nghiệm thu cấp trường'),
      ('SACH', 'GIAO_TRINH_NOI_BO', 'Giáo trình lưu hành nội bộ'),
      ('SACH', 'GIAO_TRINH_THUC_HANH', 'Giáo trình thực hành')
)
INSERT INTO phan_loai_san_pham(loai_san_pham_id, ma_phan_loai, ten_phan_loai)
SELECT types.id, classifications.ma_phan_loai, classifications.ten_phan_loai
FROM classifications JOIN types USING (ma_loai)
ON CONFLICT (loai_san_pham_id, ma_phan_loai) DO UPDATE
SET ten_phan_loai = EXCLUDED.ten_phan_loai;
