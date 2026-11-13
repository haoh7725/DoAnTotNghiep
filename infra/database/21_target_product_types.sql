SET search_path TO nckh, public;

ALTER TABLE chi_tieu_nckh ADD COLUMN IF NOT EXISTS loai_san_pham_id bigint;
UPDATE chi_tieu_nckh t
SET loai_san_pham_id = l.id
FROM loai_san_pham l
WHERE t.loai_san_pham_id IS NULL AND l.ma_loai = 'BAI_BAO';
ALTER TABLE chi_tieu_nckh ALTER COLUMN loai_san_pham_id SET NOT NULL;
DO $$ BEGIN
    ALTER TABLE chi_tieu_nckh ADD CONSTRAINT fk_chi_tieu_loai_san_pham
        FOREIGN KEY (loai_san_pham_id) REFERENCES loai_san_pham(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;
DROP INDEX IF EXISTS ux_chi_tieu_khoa_nam_danh_muc_active;
CREATE UNIQUE INDEX IF NOT EXISTS ux_chi_tieu_khoa_nam_loai_danh_muc_active
    ON chi_tieu_nckh(khoa_id, nam_hoc_id, loai_san_pham_id, COALESCE(tap_chi_hoi_nghi_id, 0))
    WHERE trang_thai <> 'DA_HUY';
