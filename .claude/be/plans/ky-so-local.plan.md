# Plan: ký số local (lô tải lên, USB token qua plugin)

> **Trạng thái:** 🔶 làm dở — kiến trúc A–G chốt 2026-10-17; ✅ bước 2–8 (code + tài liệu, [../nghiep-vu/10-lo-ky.md](../nghiep-vu/10-lo-ky.md)); còn bước 1 (đo) và chạy thử
> luồng qua API với USB token thật — phần mật mã đã ký thật qua TSA, OpenSSL xác nhận. Đã xong: mẫu chữ ký
> ([../nghiep-vu/09-mau-chu-ky.md](../nghiep-vu/09-mau-chu-ky.md)), API plugin, `ICertificateValidatorExternal`
> ([../architecture/05-infrastructure-external.md](../architecture/05-infrastructure-external.md)).
> Nghiệp vụ chi tiết (BR-01…BR-35, G1–G8): `C:\Users\Admin\Downloads\bao-cao-nghiep-vu-ky-so-local-datn.md`.

## Input

- Entity `SigningBatch`, `SigningBatchFile` đã migrate; `IFileStorageExternal` (MinIO), plugin `3.0.0`, 5 CA ghim.
- Thiếu: repository / service / API lô, hàng đợi phiên, runner, phần mật mã PDF (port từ `kssm.be`).

## Nút cổ chai — vì sao chọn kiến trúc này

| Chặng | Chi phí | Song song |
|---|---|---|
| Chuẩn bị (tải MinIO, dựng PDF, hash) | CPU + mạng nội bộ, nhỏ | Được |
| Ký trên token | `T_token` / chữ ký (đo bằng plugin `ky-so/do-toc-do`) | **Không** — token ký tuần tự |
| TSA Ban Cơ yếu | **2–131 s / lượt** (đo ở kssm) | Được, trần kết nối |
| Hoàn tất (ghép CMS, kiểm, đẩy MinIO, ghi DB) | Nhỏ | Được |

Thời gian lô ≈ `N × max(T_token, T_TSA trung bình / số kết nối TSA)`. Mục tiêu: token không bao giờ chờ, TSA
luôn bận đủ trần, RAM và số luồng **không** tăng theo số file.

## Kiến trúc đã chốt

| # | Quyết định | Vì sao |
|---|---|---|
| A | 3 chặng `prepare → sign → finish` nối bằng `Channel` **bounded**; `prepare` chạy trước ~2 đợt (16 file) | Backpressure: RAM ≤ file đang bay × cỡ file, lô 10 hay 10.000 file như nhau |
| B | Worker pool **dùng chung toàn server** (`async` + `SemaphoreSlim`): prepare = số CPU, TSA = 4 tổng, finish = 4; trần lô chạy đồng thời = 3, lô sau xếp hàng | kssm 8+8 luồng **mỗi lô**: 3 lô = 48 luồng, 12 kết nối TSA |
| C | Cache **một lần mỗi lô**: ảnh dấu đỏ / chữ ký tay đã xử lý màu + độ đậm, font, chuỗi chứng thư, `signingCertificateV2` | Mỗi file chỉ còn ghép trang + hash |
| D | `POST signatures` trả luôn đợt yêu cầu kế; long-poll 25 s, đợt 8 | Bớt ~½ round-trip FE ↔ BE; không thêm SignalR |
| E | TSA qua `IHttpClientFactory`, 3 lần thử, fail-closed; **cầu dao**: lỗi liên tiếp 5 file → lô `PAUSED` lý do TSA | Không đốt cả lô thành `FAILED` khi TSA sập; lỗi TSA không giữ token |
| F | Nhận việc bằng một `UPDATE`; kết quả + bộ đếm bằng `ExecuteUpdate`; **tiến độ đọc từ RAM** khi lô chạy; stream MinIO; bản ký **riêng tư**; **xoá nguồn ngay khi file `DONE`** | Poll 2 s không đập DB; sửa G1 (kssm xoá cả `lo-ky/{id}/` làm mất bản ký) |
| G | Khởi động: lô `SIGNING` → `PAUSED`, file `SIGNING` → `PENDING` | Hàng đợi chỉ trong RAM — chạy **một instance** |

Con số đã chốt (3 lô, 4 kết nối TSA, cầu dao 5 file lỗi TSA liên tiếp; đợt 8, chạy trước 16) gom vào hằng số; chỉnh sau khi đo thật ở bước 1.

## Steps

1. **Đo**: `T_token` trên token thật + thời gian TSA trung bình hiện tại; chỉnh các hằng số.
2. **Lô — dữ liệu**: `ISigningBatchRepository` + query; API tạo lô, thêm / xoá file, danh sách file, lô đang dở
   (route gốc `api/digital-signature/signing-batch`, quyền `Function.DigitalSignature.SigningBatch.*` — hỏi duyệt).
3. **Phiên**: hàng đợi phiên trong RAM (singleton) — mở (`ICertificateValidatorExternal.LoadTrusted` + `IsTrusted`, sai → `615`,
   lưu thumbprint), đóng, còn sống; API `session`.
4. **Mật mã**: port `PdfPreparer`, `CmsAssembler`, `PdfContentWriter`, `TimestampClient`, `SignatureVerifier` vào
   `infrastructure.external`; ký thử bằng chứng thư test, mở trong Adobe / Foxit.
5. **Runner (A, B, C, E)**: pool dùng chung, 3 channel, cache theo lô, cầu dao TSA.
6. **Đưa thư (D)**: `requests` (long-poll), `signatures` (nộp + nhận đợt kế), mất phiên → `PAUSED`.
7. **Điều khiển (F, G)**: bắt đầu / ký tiếp, tạm dừng, huỷ, tiến độ từ RAM, khôi phục lúc khởi động.
8. Tài liệu `nghiep-vu/10-lo-ky.md` + architecture; `dotnet build sh.be/sh.be.api/sh.be.api.sln` sạch,
   `has-pending-model-changes` = "No changes" (trừ thay đổi schema đã duyệt).

## Expected output

- Lô 1000 file chạy hết mà RAM và số luồng của backend phẳng; token không có khoảng rảnh giữa các đợt.
- TSA sập giữa chừng → lô `PAUSED` có lý do, ký tiếp được; restart backend → không còn lô / file kẹt `SIGNING`.
- Bản ký trên MinIO mở được, Adobe báo chữ ký hợp lệ có dấu thời gian; nguồn của file `DONE` đã dọn.

## Điểm cần chú ý

- Trần kết nối TSA là núm ảnh hưởng thời gian lớn nhất; chưa biết Ban Cơ yếu cho tối đa bao nhiêu.
- Tiến độ đọc RAM nên phải ghi DB trước rồi mới cập nhật RAM, lệch ngược lại thì restart mất số liệu.
- Xoá nguồn theo file ⇒ ký lại file `DONE` là không thể (đúng ý: file đã ký không ký lại).
- Một instance + phải giữ tab mở + plugin tự đóng phiên sau 15 phút rảnh — giới hạn đã biết, ghi vào báo cáo đồ án.
- Chưa có WYSIWYS: plugin ký mọi `SignedAttributes` nhận được.
