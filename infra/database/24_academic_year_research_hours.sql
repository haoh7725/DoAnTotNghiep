BEGIN;
ALTER TABLE nckh.nam_hoc
    ADD COLUMN IF NOT EXISTS dinh_muc_gio_nckh numeric(14,4) NOT NULL DEFAULT 586;
ALTER TABLE nckh.nam_hoc DROP CONSTRAINT IF EXISTS ck_nam_hoc_dinh_muc_gio;
ALTER TABLE nckh.nam_hoc ADD CONSTRAINT ck_nam_hoc_dinh_muc_gio CHECK (dinh_muc_gio_nckh > 0);
UPDATE nckh.nam_hoc SET dinh_muc_gio_nckh = 586 WHERE dinh_muc_gio_nckh IS NULL OR dinh_muc_gio_nckh <= 0;
-- Bộ dữ liệu v4 chỉ chốt năm lịch sử. Xóa riêng kết quả seed của năm hiện tại
-- để Quản trị viên còn có thể điều chỉnh định mức trước khi chốt thật.
DO $$
BEGIN
    CREATE TABLE IF NOT EXISTS nckh.ung_dung_migration (
        ma varchar(100) PRIMARY KEY,
        ap_dung_luc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
    );
    IF NOT EXISTS (SELECT 1 FROM nckh.ung_dung_migration WHERE ma='24_remove_current_seed_evaluations') THEN
        DELETE FROM nckh.ket_qua_danh_gia e
        USING nckh.nam_hoc y
        WHERE e.nam_hoc_id=y.id AND y.ma_nam_hoc='2026-2027'
          AND EXISTS (SELECT 1 FROM nckh.chi_tiet_quy_doi_ung_dung d
              WHERE d.ket_qua_id=e.id AND d.can_cu_tinh LIKE '%dữ liệu seed v4%')
          AND NOT EXISTS (SELECT 1 FROM nckh.chi_tiet_quy_doi_ung_dung d
              WHERE d.ket_qua_id=e.id AND d.can_cu_tinh NOT LIKE '%dữ liệu seed v4%');
        INSERT INTO nckh.ung_dung_migration(ma) VALUES ('24_remove_current_seed_evaluations');
    END IF;
END $$;
COMMIT;
