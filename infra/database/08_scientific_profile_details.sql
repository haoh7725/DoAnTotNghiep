-- Bổ sung lý lịch theo Phụ lục V. Chạy lặp lại an toàn, không đổi dữ liệu cũ.
BEGIN;
ALTER TABLE nckh.ly_lich_khoa_hoc
    ADD COLUMN IF NOT EXISTS chi_tiet jsonb NOT NULL DEFAULT '{}'::jsonb;
COMMIT;
