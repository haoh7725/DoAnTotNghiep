# Hướng dẫn chức năng kiểm tra tương đồng Copyleaks

Tài liệu này giúp thành viên trong nhóm hiểu kiến trúc, cấu hình môi trường và chạy
chức năng kiểm tra tương đồng trên máy cá nhân.

## 1. Chức năng làm gì?

Giảng viên chọn một tệp minh chứng PDF, DOC, DOCX hoặc TXT trong chi tiết sản
phẩm, xác nhận đồng ý gửi tệp cho Copyleaks và bấm **Kiểm tra tương đồng**.

Luồng xử lý:

1. Web gọi API của Research Hub.
2. Backend đọc tệp từ MinIO và tính SHA-256.
3. Backend tạo bản ghi `nckh.kiem_tra_tuong_dong`.
4. Backend gửi tệp sang Copyleaks bằng API bất đồng bộ.
5. Copyleaks xử lý và gọi webhook HTTPS về Research Hub.
6. Backend xác thực webhook bằng secret, sau đó lưu tỷ lệ tương đồng, số nguồn,
   số từ và credit.
7. Web tự tải lại trạng thái mỗi 5 giây khi tác vụ đang xử lý.

Kết quả là **tỷ lệ tương đồng**, không phải kết luận tự động rằng tài liệu đạo văn.

## 2. Thành phần mã nguồn

- `infra/database/07_plagiarism_checks.sql`: bảng, ràng buộc và index.
- `Domain/Entities/PlagiarismCheck.cs`: entity và chuyển trạng thái.
- `Application/Plagiarism/PlagiarismCheckService.cs`: nghiệp vụ, SHA-256 và phân quyền.
- `Infrastructure/Plagiarism/CopyleaksPlagiarismProvider.cs`: xác thực và gửi Copyleaks.
- `Api/Controllers/PlagiarismChecksController.cs`: API cho Web.
- `Api/Controllers/CopyleaksWebhooksController.cs`: nhận kết quả từ Copyleaks.
- `web/src/Products.tsx`: tab **Kiểm tra tương đồng** và lịch sử.

Các trạng thái:

| Mã | Ý nghĩa |
|---|---|
| `CHO_XU_LY` | Yêu cầu vừa được tạo |
| `DANG_GUI` | Backend đang gửi tệp |
| `DANG_KIEM_TRA` | Copyleaks đang xử lý |
| `HOAN_THANH` | Đã nhận kết quả |
| `THAT_BAI` | Gửi hoặc xử lý thất bại |
| `DA_HUY` | Tác vụ bị hủy |

## 3. Yêu cầu trên máy thành viên

- Docker Desktop đang chạy.
- Node.js và npm để chạy Web.
- Một tài khoản Copyleaks có API key đã bật.
- Không chia sẻ hoặc commit API key.

Sandbox vẫn cần email và API key hợp lệ nhưng trả kết quả mô phỏng và không dùng
credit thật.

## 4. Cấu hình `.env`

Sao chép `.env.example` thành `.env` nếu máy chưa có file này, sau đó điền:

```env
COPYLEAKS_EMAIL=email_dang_nhap_copyleaks
COPYLEAKS_API_KEY=api_key_cua_rieng_ban
COPYLEAKS_PUBLIC_BASE_URL=https://ten-tunnel.trycloudflare.com
COPYLEAKS_WEBHOOK_SECRET=chuoi-ngau-nhien-dai-it-nhat-32-ky-tu
COPYLEAKS_SANDBOX=true
```

Lấy API key tại `https://api.copyleaks.com/dashboard`, mục
**API Access Credentials**. Bật key trước khi sao chép.

Nếu API key từng xuất hiện trong chat, ảnh chụp hoặc Git, phải disable key đó và
tạo key mới.

## 5. Khởi động database, MinIO và API

Từ thư mục gốc dự án:

```powershell
docker compose --env-file .env -f infra/compose.yaml up -d --build
```

Kiểm tra:

```powershell
docker compose --env-file .env -f infra/compose.yaml ps
```

API chạy tại `http://localhost:8081`; MinIO Console chạy tại
`http://localhost:9001`.

### Database mới

Các script trong `infra/database` được chạy tự động khi PostgreSQL tạo volume lần
đầu.

### Database đã tồn tại

Áp dụng migration một lần:

```powershell
Get-Content -Raw infra/database/07_plagiarism_checks.sql |
  docker exec -i research-management-postgres psql -U postgres -d research_management -v ON_ERROR_STOP=1
```

Script có `CREATE ... IF NOT EXISTS`, nên có thể chạy lại an toàn.

## 6. Tạo HTTPS webhook khi chạy local

Copyleaks không gọi được `localhost`. Khi demo local, dùng Cloudflare Quick Tunnel.

Xác định network của Compose (thường là `infra_default`):

```powershell
docker network ls
```

Tạo tunnel:

```powershell
docker run -d --name research-management-tunnel `
  --network infra_default `
  cloudflare/cloudflared:latest tunnel --no-autoupdate --url http://api:8080
```

Lấy URL HTTPS:

```powershell
docker logs research-management-tunnel
```

Tìm dòng dạng:

```text
https://random-words.trycloudflare.com
```

Gán URL đó cho `COPYLEAKS_PUBLIC_BASE_URL` trong `.env`, rồi nạp lại cấu hình:

```powershell
docker compose --env-file .env -f infra/compose.yaml up -d api
```

Kiểm tra tunnel:

```powershell
Invoke-RestMethod https://random-words.trycloudflare.com/api/system/info
```

Kết quả phải có `status = running`.

Quick Tunnel chỉ dùng cho phát triển, không có cam kết uptime. Nếu log có
`Tunnel not found`, xóa container và tạo lại; sau đó cập nhật URL trong `.env` và
restart API:

```powershell
docker rm -f research-management-tunnel
```

## 7. Chạy Web

```powershell
cd web
npm install
npm run dev
```

Mở `http://localhost:5173`, đăng nhập, vào **Sản phẩm của tôi** → chọn một sản
phẩm → tab **Kiểm tra tương đồng**.

Tài khoản demo:

```text
demo.gv01 / Demo@12345678
```

Chọn file PDF/DOC/DOCX/TXT, đánh dấu đồng ý và bấm **Kiểm tra tương đồng**.

## 8. API nội bộ

```http
GET  /api/products/{productId}/plagiarism-checks
POST /api/products/{productId}/plagiarism-checks
```

Body tạo yêu cầu:

```json
{
  "evidenceId": 49,
  "consent": true
}
```

Webhook Copyleaks:

```http
POST /api/integrations/copyleaks/webhooks/{scanId}/{status}
X-Copyleaks-Webhook-Secret: <secret>
```

Webhook được miễn header nội bộ `X-Requested-With`, nhưng bắt buộc webhook secret.

## 9. Quy tắc và bảo mật

- Chỉ tác giả chính hoặc quản trị viên được tạo yêu cầu kiểm tra.
- Tệp phải thuộc đúng sản phẩm.
- Chỉ hỗ trợ PDF, DOC, DOCX và TXT.
- Không tạo lượt mới khi cùng phiên bản tệp đang được xử lý.
- Không đưa tài liệu vào Copyleaks Shared Data Hub.
- Loại trừ trích dẫn, tài liệu tham khảo và mục lục để giảm trùng giả.
- API key chỉ nằm trong `.env`; `.env` đã được Git ignore.
- Không log API key hoặc access token.

## 10. Lỗi thường gặp

### `Copyleaks chưa được cấu hình đầy đủ`

Kiểm tra đủ bốn biến email, API key, public URL và webhook secret; restart API.

### `401` khi xác thực Copyleaks

Key chưa bật, key sai hoặc email không đúng tài khoản tạo key.

### Trạng thái kẹt ở `DANG_KIEM_TRA`

Kiểm tra container tunnel, URL trong `.env` và log API:

```powershell
docker logs research-management-tunnel
docker logs research-management-api
```

### `Tunnel not found`

Quick Tunnel đã hết hạn. Tạo tunnel mới, cập nhật `.env`, restart API và gửi lượt
kiểm tra mới.

### Không thấy tab hoặc endpoint trả lỗi bảng không tồn tại

Đảm bảo đã pull đúng nhánh, rebuild Web/API và chạy migration 07.

## 11. Chuyển sang production

Khi deploy thật:

1. Dùng domain HTTPS cố định thay Quick Tunnel.
2. Đặt `COPYLEAKS_SANDBOX=false`.
3. Lưu secret bằng secret manager của môi trường triển khai.
4. Cấu hình giới hạn số lượt/credit theo người dùng.
5. Theo dõi các tác vụ bị treo và chi phí Copyleaks.
