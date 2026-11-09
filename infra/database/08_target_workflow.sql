ALTER TABLE nckh.chi_tieu_nckh
    ADD COLUMN IF NOT EXISTS trang_thai varchar(30) NOT NULL DEFAULT 'CONG_BO',
    ADD COLUMN IF NOT EXISTS nguoi_giao_id bigint REFERENCES nckh.tai_khoan(id),
    ADD COLUMN IF NOT EXISTS nguoi_tiep_nhan_id bigint REFERENCES nckh.tai_khoan(id),
    ADD COLUMN IF NOT EXISTS tiep_nhan_luc timestamptz,
    ADD COLUMN IF NOT EXISTS cong_bo_luc timestamptz,
    ADD COLUMN IF NOT EXISTS khoa_luc timestamptz;

DO $$ BEGIN
    ALTER TABLE nckh.chi_tieu_nckh ADD CONSTRAINT ck_chi_tieu_trang_thai
        CHECK (trang_thai IN ('CHO_TIEP_NHAN','DA_TIEP_NHAN','CONG_BO','DA_KHOA','DA_HUY'));
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

ALTER TABLE nckh.san_pham
    ADD COLUMN IF NOT EXISTS tap_chi_hoi_nghi_id bigint REFERENCES nckh.danh_muc_tap_chi_hoi_nghi(id),
    ADD COLUMN IF NOT EXISTS chi_tieu_id bigint REFERENCES nckh.chi_tieu_nckh(id);

CREATE INDEX IF NOT EXISTS ix_san_pham_chi_tieu ON nckh.san_pham(chi_tieu_id);
CREATE INDEX IF NOT EXISTS ix_chi_tieu_trang_thai ON nckh.chi_tieu_nckh(trang_thai);
