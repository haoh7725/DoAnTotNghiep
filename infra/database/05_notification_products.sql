ALTER TABLE nckh.thong_bao
ADD COLUMN IF NOT EXISTS san_pham_ung_dung_id BIGINT
REFERENCES nckh.san_pham(id);

CREATE INDEX IF NOT EXISTS ix_thong_bao_san_pham_ung_dung
ON nckh.thong_bao(san_pham_ung_dung_id);