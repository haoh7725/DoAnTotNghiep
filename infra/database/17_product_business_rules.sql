ALTER TABLE nckh.san_pham ADD COLUMN IF NOT EXISTS ma_cong_trinh varchar(255);
ALTER TABLE nckh.san_pham_phien_ban_ung_dung ADD COLUMN IF NOT EXISTS ma_cong_trinh varchar(255);
CREATE UNIQUE INDEX IF NOT EXISTS uq_san_pham_app_ma_cong_trinh
    ON nckh.san_pham (lower(btrim(ma_cong_trinh))) WHERE ma_cong_trinh IS NOT NULL;
