# Hạ tầng ký số — pipeline lô ký, PDF, CMS, TSA

> **Phần 9/9** · trước: [08-them-chuc-nang.md](08-them-chuc-nang.md) · mục lục: [README.md](README.md)

Phần kỹ thuật của ký số PDF. Nghiệp vụ lô ký xem [../nghiep-vu/10-lo-ky.md](../nghiep-vu/10-lo-ky.md), mẫu chữ ký
xem [../nghiep-vu/09-mau-chu-ky.md](../nghiep-vu/09-mau-chu-ky.md). Đây là phần **duy nhất** có pipeline chạy nền.

## Thư mục

| Thư mục / file | Chứa gì |
|---|---|
| `applications/.../DigitalSignature/Implements/SigningRunner.cs` | Điều phối pipeline lô ký (singleton, chạy bằng `Task.Run`) |
| `infrastructure.external/Pdf/Implements/PdfSigner.cs` | Implement `IPdfSigner`: `CreateContext`, `Prepare`, `Complete`, `HasSignature` |
| `infrastructure.external/Pdf/Base/PdfPreparer.cs` | Thêm một **revision mới** vào cuối PDF: widget chữ ký, ô hình, ô chữ; giữ nguyên byte cũ |
| `infrastructure.external/Pdf/Base/PdfAppearanceBuilder.cs`, `ImageSizeReader`, `AppearanceFontResolver` | Dựng appearance stream: chữ, con dấu, chữ ký tay |
| `infrastructure.external/Pdf/Base/PdfRevisionReader`, `PdfSignatureReader`, `PdfObjectWriter` | Đọc revision / xref, đọc chữ ký có sẵn, ghi object |
| `infrastructure.external/Signing/Base/CmsBuilder.cs` | Dựng `SigningCertificateV2`, signed attributes, gói CMS (detached) |
| `infrastructure.external/Signing/Implements/SigningQueue.cs` | Implement `ISigningQueue`: phiên ký **trong bộ nhớ**, một phiên mỗi lô |
| `infrastructure.external/Timestamp/Implements/TimestampClient.cs` | Implement `ITimestampClient`: TSA RFC 3161, băm SHA-256, nonce, retry |

## Pipeline của một lô

```text
ClaimAsync ──► PrepareAsync ──► SignAsync ──────────► FinishAsync
DB: PENDING→SIGNING  tải PDF, dựng signedAttributes   chờ plugin ký   TSA → ghép CMS → lưu MinIO → DB DONE
   1 worker          PREPARE_WORKERS = 4              REQUEST_BATCH_SIZE = 8   TSA_CONNECTIONS = 4
```

Ba kênh có giới hạn: `claimed` (8), `prepared` (16), `signed` (8). Giới hạn toàn cục: `MAX_RUNNING_BATCHES = 3`
lô song song, số worker prepare = số core CPU, `FINISH_SLOTS = 4` cho bước lưu.

1. **CreateContext** — nạp `Template`, vị trí, ảnh con dấu / chữ ký tay. Chứng thư lấy bản **copy** từ phiên
   (`CopySessionCertificate`).
2. **Prepare** — `PdfPreparer` đọc revision, từ chối PDF không hỗ trợ / mã hoá / khoá (`625`–`627`). PDF đã có chữ ký
   chỉ được ký khi template cho phép (`628`). Kết quả: `signedAttributes` = băm phần byte theo `ByteRange`.
3. **Sign** — `RequestSignatureAsync` đưa `signedAttributes` vào hàng đợi phiên. Plugin trên máy người dùng long-poll
   `TakeRequestsAsync` (25 giây) rồi trả về `SubmitSignatures`. Quá 120 giây → `634`.
4. **Finish** — `RequestTimestampAsync` xin token TSA trên băm chữ ký, retry với thời gian chờ nhân đôi.
   `PdfSigner.Complete` ghép CMS vào vùng placeholder hex (quá chỗ → `629`), kiểm lại chữ ký phủ toàn file và token
   timestamp (`631`). Upload private với key `SigningBatchFilePolicy.BuildSignedObjectKey(lô, thứ tự)`, ghi DB qua
   `MarkFileSignedAsync`, rồi xoá file nguồn.

Hết file: lô `DONE`, `CloseSession`. Lô dừng giữa chừng thì `Pause` hoặc `Fail`.

## Lỗi và cách lô phản ứng

| Tình huống | Kết quả |
|---|---|
| Phiên plugin đóng (rút token, đóng tab, plugin tắt) — `619` | Lô `PAUSED`, lý do `PAUSED_BY_SESSION_REASON` |
| TSA lỗi liên tiếp ≥ `TSA_FAILURES_TO_PAUSE` (5) | Lô `PAUSED`, file lỗi `FAILED`, đếm lại khi TSA phục hồi |
| Lỗi riêng một file (PDF hỏng, timeout, ghi DB lỗi) | Chỉ file đó `FAILED`, lô chạy tiếp |
| Lỗi không lường trước ở `RunAsync` | File `SIGNING` trả về `PENDING`, lô `FAILED` (`FAILED_BY_SYSTEM_REASON`) |
| Máy chủ khởi động lại | `RecoverAsync` pause mọi lô đang `SIGNING` (`PAUSED_BY_RESTART_REASON`) |

## Được / không được

- **Được:** chỉ `SigningRunner` gọi `ISigningQueue` và `ITimestampClient`. Controller chỉ mở / đóng phiên và đọc tiến độ.
- **Không được:** thêm I/O DB hoặc gọi chậm vào `SignAsync` — nó giữ phiên plugin, làm chậm mọi file của lô.
- **Không được:** tin cờ "đã tin cậy" từ client. Chứng thư chỉ qua `ICertificateValidator.IsTrusted` (xem [05](05-infrastructure-external.md)).

⚠️ Phiên ký (`SigningQueue`) và slot của `SigningRunner` nằm trong bộ nhớ tiến trình. Hệ thống chỉ chạy đúng với
**một** instance API. Muốn scale ngang phải đưa hàng đợi và trạng thái phiên ra ngoài (Redis hoặc DB). Đừng nhân bản API
mà không sửa phần này.

⚠️ `SaveSignedFileAsync` upload MinIO **trước**, ghi DB **sau** trong scope khác. Nếu ghi DB lỗi, file thành `FAILED` nhưng
object đã có trên MinIO. Key xác định theo (lô, thứ tự) nên lần ký lại ghi đè đúng object đó. Đừng đổi sang key ngẫu nhiên
mà không xét lại chuyện này.

⚠️ Đừng dùng `GetSession(...).Certificate` ngoài phiên. Dùng `CopySessionCertificate`, vì pipeline dispose bản
copy của nó khi kết thúc (`using var certificate`).

⚠️ Thời gian có hai dạng: `signedAt` theo giờ Việt Nam đưa vào appearance, `signedAtUtc` đưa vào signed attributes.
Đừng gộp một giá trị cho cả hai.
