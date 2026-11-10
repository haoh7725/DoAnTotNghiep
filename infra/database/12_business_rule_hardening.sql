ALTER TABLE nckh.dinh_muc_nckh
    ADD COLUMN IF NOT EXISTS so_diem_dinh_muc NUMERIC(12,4) NOT NULL DEFAULT 100;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_dinh_muc_so_diem'
          AND conrelid = 'nckh.dinh_muc_nckh'::regclass
    ) THEN
        ALTER TABLE nckh.dinh_muc_nckh
            ADD CONSTRAINT ck_dinh_muc_so_diem CHECK (so_diem_dinh_muc >= 0);
    END IF;
END $$;
