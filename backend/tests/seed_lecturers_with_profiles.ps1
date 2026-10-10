# Wrapper script for seeding lecturers with profiles and scientific CVs
Write-Host "Đang chạy script khởi tạo dữ liệu giảng viên..." -ForegroundColor Cyan
node (Join-Path $PSScriptRoot "seed_lecturers.mjs")
