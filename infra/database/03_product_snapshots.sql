CREATE TABLE IF NOT EXISTS nckh.san_pham_phien_ban_ung_dung (
    san_pham_id BIGINT NOT NULL REFERENCES nckh.san_pham(id) ON DELETE CASCADE,
    phien_ban INTEGER NOT NULL CHECK (phien_ban > 0),
    nguoi_cap_nhat_id BIGINT NOT NULL REFERENCES nckh.tai_khoan(id),
    ghi_luc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    tieu_de VARCHAR(500) NOT NULL,
    mo_ta TEXT,
    thong_tin_xuat_ban VARCHAR(500),
    ngay_cong_bo DATE,
    trang_thai_bai_bao VARCHAR(30),
    chi_so_tap_chi VARCHAR(100),
    phan_loai_tap_chi VARCHAR(100),
    cap_de_tai VARCHAR(100),
    trang_thai VARCHAR(30) NOT NULL,
    PRIMARY KEY (san_pham_id, phien_ban)
);
