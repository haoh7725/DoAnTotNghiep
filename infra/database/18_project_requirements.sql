-- Preserve the basis of previous results; new evaluations explicitly select GIO or DIEM.
ALTER TABLE nckh.ket_qua_danh_gia
    ADD COLUMN IF NOT EXISTS don_vi_danh_gia varchar(10) NOT NULL DEFAULT 'LEGACY';

-- Restore certificates once, without overriding later administrator choices on each restart.
DO $$
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext('nckh:application-migrations'));
    CREATE TABLE IF NOT EXISTS nckh.ung_dung_migration (
        ma varchar(100) PRIMARY KEY,
        ap_dung_luc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
    );
    IF NOT EXISTS (SELECT 1 FROM nckh.ung_dung_migration WHERE ma = '18_project_requirements') THEN
        UPDATE nckh.loai_san_pham SET dang_su_dung = true WHERE ma_loai = 'CHUNG_NHAN';
        INSERT INTO nckh.ung_dung_migration(ma) VALUES ('18_project_requirements');
    END IF;
END $$;
