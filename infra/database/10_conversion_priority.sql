ALTER TABLE nckh.quy_dinh_quy_doi
    ADD COLUMN IF NOT EXISTS do_uu_tien integer NOT NULL DEFAULT 100;

ALTER TABLE nckh.quy_dinh_quy_doi DROP CONSTRAINT IF EXISTS ck_quy_dinh_uu_tien;
ALTER TABLE nckh.quy_dinh_quy_doi
    ADD CONSTRAINT ck_quy_dinh_uu_tien CHECK (do_uu_tien >= 0);
