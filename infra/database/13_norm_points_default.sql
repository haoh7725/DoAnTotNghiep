-- Điểm định mức là cấu hình nghiệp vụ, không tự gán 100 khi người dùng bỏ trống.
-- Không cập nhật các dòng hiện có vì giá trị 100 có thể đã được cấu hình chủ đích.
ALTER TABLE nckh.dinh_muc_nckh
    ALTER COLUMN so_diem_dinh_muc SET DEFAULT 0;
