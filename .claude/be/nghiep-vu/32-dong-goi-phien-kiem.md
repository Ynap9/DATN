# Đóng gói — phiên kiểm tra thư mục tải lên (Task 65)

> Cập nhật 2026-11-28. Luật gốc: [../plans/dong-goi-tt05.plan.md](../plans/dong-goi-tt05.plan.md) (mã C / N), thi công:
> [../plans/dong-goi-tt05-thi-cong-65.plan.md](../plans/dong-goi-tt05-thi-cong-65.plan.md). Bộ trường dùng để kiểm: [16](16-dong-goi-cau-hinh.md).

Người dùng tải cả thư mục (Excel mẫu Task 37 + file tài liệu), BE soi từng file, ghép cột Excel, gom file vào gói, kiểm giá trị
ô rồi trả kết quả theo gói / file. Dựng ZIP là Task 66, dùng lại kết quả này.

## Mô hình — 3 bảng schema `packaging`, quan hệ mềm theo Id

| Bảng | Một dòng là | Cột chính |
|---|---|---|
| `PackageSession` | Một phiên | `PackageType`, `MetadataTypes` ("1,2"), `Status`, `Total/Done/Error/WarningDocument`, `MatchReport` (JSON), `ExpiredDate` = tạo + 7 ngày, `FailReason` (chỉ lỗi hệ thống); cột `Build*`, `FondsCode`, `BuiltBy` để Task 66 |
| `PackageDossier` | Một gói sẽ dựng = một dòng sheet cấp gói | `ExcelDocumentId`, `PackageCode`, `RowIndex`, `FolderName`, `Status`, `ErrorSummary` (lỗi + cảnh báo), `DocumentCount`; cột `Package*` để Task 66 |
| `PackageDocument` | Một file tải lên | `FileRole` (`data` / `excel` / `dip-auth`), `RelativePath`, `ObjectKey`, `Sha256` (HOA), `IsSigned`, `PdfAPart` ("2B"), `PageCount`, `DossierId`, `MetadataType`, `DocKey`, `Status`, `Reason` |

Trạng thái (`PackageSessionConst`): phiên `pending` → `running` → `passed` / `warning` / `failed`, hoặc `cancelled`; gói và file
`pending` / `passed` / `warning` / `failed`. Quyền: `FUNCTION_PACKAGE_BUILD` cho mọi API (chỉ Admin / SuperAdmin), xem mọi phiên.

## API — `api/packaging/session`

| API | Việc |
|---|---|
| `POST` | `{packageType, metadataTypes[]}`: loại sai `701`; `metadataTypes` phải có `1` + ≥ 1 loại tài liệu, không trùng, nếu sai `702`. Trả `id` + sheet / cột cần có |
| `POST {id}/files` | multipart `Files` + `RelativePaths` (webkitRelativePath, cùng số lượng, có thư mục gốc, nếu thiếu `709`); 1–200 file / đợt (`708`); phiên phải `pending` (`707`). Trả `acceptedCount`, `skipped`, `rejected` |
| `POST {id}/start` | Phải `pending` + ≥ 1 Excel + ≥ 1 file data (`704`); không có server Hangfire queue `default` → `210`; đặt `running` rồi Enqueue `PackageVerifyJob` |
| `GET {id}/progress` | `status`, tổng / xong / lỗi / cảnh báo, giờ bắt đầu / kết thúc (FE hỏi 2 giây / lần). Không có phiên → `706` |
| `GET {id}/result` | Danh sách gói + danh sách Excel (lỗi Excel, cột ghép kèm điểm, thiếu, thiếu bắt buộc, không dùng — đọc lại `MatchReport`) |
| `GET {id}/documents` | Phân trang `pageNumber` / `pageSize`, lọc `dossierId`, `status` |
| `POST {id}/cancel` | `pending` / `running` → `cancelled`, xoá MinIO `packaging/{id}/`; đã kết thúc → `705`. Job tự dừng ở lô kế (đọc lại `Status`) |

Lỗi nghiệp vụ của gói / file **không** ném exception, ghi vào `ErrorSummary` / `Reason`.

## Nhận file (65.4–65.5)

- Đuôi: `.xlsx` là Excel; `.xls` bị từ chối "Excel định dạng cũ, lưu lại thành .xlsx". File data chỉ nhận đuôi của loại tài liệu phiên
  đã chọn: văn bản `.pdf`; ảnh `.jpg .jpeg .png .gif .tif .tiff .pdf`; ghi âm / ghi hình `.mp4 .mpg .mpeg .avi .wmv .mov .qt .mp3 .wma .aac .wav`.
  Phiên DIP luôn nhận `.pdf` (file xác thực). Đuôi khác → `skipped`, không lỗi cả đợt.
- Cỡ: PDF / ảnh ≤ 100 MB, ghi âm / ghi hình ≤ 1 GB → vượt thì `rejected` đúng file đó. Cả request ≤ 2 GB (`[RequestSizeLimit]` riêng action).
- MimeType theo [03 §8](../dong-goi/03-sip-metadata.md); `.wav` → `audio/wav` (bảng 03 §8 không có, PM chốt kỹ thuật).
- Soi lúc nhận (`IPackageFileInspectExternal`): SHA-256 viết HOA mọi file; PDF thêm số trang (PDFsharp), có chữ ký (dùng chung
  `PdfSignatureReader.HasSignature`), PDF/A từ XMP `pdfaid:part` + `pdfaid:conformance`. Rồi `UploadPrivateAsync`
  `packaging/{sessionId}/source/{seq:D6}{ext}` (`seq` = số file của phiên kể cả đã xoá). Bước kiểm chỉ tải lại Excel.

## Kiểm — `PackageSessionDomainService.BuildVerifyPlan` (65.6–65.15)

1. **Excel áp dụng**: mỗi file dùng Excel ở thư mục gần nhất đi ngược lên cha; không có → "Không tìm thấy file Excel metadata áp dụng
   cho thư mục này". 2 Excel cùng thư mục → lỗi cả hai, file dưới đó lỗi theo. Excel lỗi (đọc không được, thiếu sheet, thiếu cột
   bắt buộc) → file dùng nó lỗi "File Excel áp dụng ... bị lỗi".
2. **Sheet + cột**: sheet đúng `PackageSchema.Name` (không phân biệt hoa thường). Header ↔ `DisplayName`: bỏ phần trong ngoặc, bỏ dấu
   (`đ` → `d`), chữ thường, ký tự khác chữ / số thành cách, gộp cách; điểm = max(1 − Levenshtein / độ dài lớn hơn, Dice theo từ),
   ghép khi ≥ 0,8; chọn tham lam theo điểm cao nhất, mỗi trường 1 header, mỗi header 1 trường. "Tiêu đề của hồ sơ" → 0,89.
3. **Gói** = mỗi dòng có dữ liệu ở sheet cấp gói. Mã gói = ô trường mã đầu tiên có trong bộ cấp gói: `fileCode` / `arcFileCode` /
   `requestID`. Trống hoặc trùng trong cùng Excel → lỗi gói. DIP: sheet cấp gói phải đúng 1 dòng, không thì lỗi Excel.
4. **Khoá tài liệu** (`ResolveDocKey`, theo bộ trường của schema): `docId` → `docCode` → `arcDocCode` → `arcFileCode` + "." +
   `docOrdinal` đệm 7 số. File khớp dòng khi tên bỏ đuôi (Trim, FormC, không phân biệt hoa thường) = khoá; khớp > 1 dòng → lỗi trùng mã.
5. **Gom vào gói**: (1) Excel chỉ 1 gói → gói đó; (2) sheet có cột `arcFileCode` → gói có mã bằng ô đó; (3) khoá = mã gói hoặc bắt đầu
   bằng mã gói + "." → mã dài nhất; (4) không khớp → lỗi file.
6. **DIP**: `{requestID}.pdf` cùng thư mục Excel là file xác thực (`dip-auth`, không phải tài liệu); thiếu → lỗi gói; không PDF/A → cảnh báo.
7. **Giá trị ô** (chỉ dòng có file + dòng cấp gói): ô trống = rỗng / khoảng trắng / U+200B / U+FEFF, chữ "null" là dữ liệu (dùng chung
   `SubmissionImportDomainService.NormalizeImportValue`). REQUIRED; CONDITIONAL khi ô `ConditionElement` ∈ `ConditionValues`; `MaxLength`;
   NUMBER chữ số ASCII; BOOLEAN `1` / `0`; DATE `dd/MM/yyyy`, riêng AIP (1, 2) ở `startDate` / `endDate` / `issuedDate` nhận thêm
   `MM/yyyy`, `yyyy`; mã thuộc `CodeGroup` (`language` tách dấu phẩy). `SIP_hoso` `docCode` = mã gói + "." + 7 chữ số; `SIP_tailieu`
   chỉ kiểm đuôi "." + 7 chữ số; AIP `docCode`, `arcDocCode` chỉ độ dài. Lỗi dòng tài liệu vào `Reason` file, lỗi dòng cấp gói vào gói.
8. **Đối chiếu**: file không có dòng → lỗi file (không gắn gói); trùng khoá trong cùng gói → lỗi file; dòng chưa có file → cảnh báo gói.
   PDF tài liệu chưa ký / không PDF/A → cảnh báo.
9. **Trạng thái**: gói `failed` nếu tự lỗi hoặc ≥ 1 file lỗi, `warning` nếu chỉ cảnh báo. Phiên `failed` khi không có gói nào, có gói
   lỗi, hoặc có file / Excel lỗi mà không thuộc gói nào (để lỗi không bị giấu); `warning` khi có gói cảnh báo; còn lại `passed`.

Job (`VerifyFromJob`): đọc bộ trường + mã **1 lần** (`IPackageConfigQuery.GetVerifyConfig`), tính kế hoạch trong RAM, lưu gói, rồi ghi
file theo lô 200 (`AddProgress` + `SaveChanges` mỗi lô, đọc lại `Status` trước mỗi lô). Lỗi bất ngờ → phiên `failed` + `FailReason`.

## Dọn hết hạn (65.19)

`PackageSessionCleanupJob` (recurring `package-session-cleanup`, cron `BackgroundJob:Jobs:PackageSessionCleanup` = `0 2 * * *` giờ VN):
phiên `ExpiredDate < now` → `DeleteByPrefixAsync("packaging/{id}/")` (ZIP Task 66 cũng nằm dưới) + xoá mềm phiên, gói, file (`SYSTEM_USER`).

## Đã kiểm thực tế — 2026-11-28

- API gọi thật: `701`, `702` (thiếu cấp gói / trùng / chỉ cấp gói), `709` (thiếu đường dẫn / không có thư mục), `.xls` + PDF 150 MB bị
  `rejected`, `.mp4` vào phiên ảnh bị `skipped`, `706`, `704` phiên rỗng, huỷ `pending` → `cancelled`, huỷ lại `705`, tải sau huỷ `707`.
- Luồng kiểm chạy bằng harness gọi thẳng `CheckUploadFile` → `PackageFileInspectExternal` → `ExcelReaderExternal` (đọc file local) →
  `BuildVerifyPlan` → `Complete` / `Finish`: đủ 22 ca của task trừ 19–21. PDF gói mẫu: đã ký, `2B`, 2 trang, SHA-256 trùng `CHECKSUM` trong METS.

- BE thật trên DB riêng (LocalDB, Hangfire tách): Leader `403` cả 5 API (ca 21); huỷ phiên `running` → `cancelled`, huỷ phiên `failed` → `705`
  (ca 19); job dọn trigger tay xoá mềm phiên hết hạn + file, `DeletedBy = system` (ca 20); job kiểm qua Hangfire gặp Excel không đọc được →
  lỗi Excel, file dưới lỗi theo, phiên `failed`, không treo.

Lưu ý: Chưa chạy được tải file lên MinIO: MinIO dev báo "minimum free drive threshold" (đầy ổ) khi `PutObject`, và bản MinIO cộng đồng
đã ngừng phát hành (`dl.min.io` trả 410) nên không dựng được MinIO trên máy. Luồng tải → job kiểm với file thật chỉ kiểm qua harness.
Lưu ý: `SO_HOA_DB` trong `appsettings.json` (`127.0.0.1,14330`) là tunnel tới **DB chung của nhóm**. Chạy BE là ghi vào đó (seed quyền,
đăng ký recurring job, làm server Hangfire): server Hangfire của máy đồng đội chưa có code mới sẽ nhận `PackageSessionCleanupJob` rồi
Failed (`TypeLoadException`). Chỉ `database update` / chạy BE có job mới khi cả nhóm đã có code, hoặc dùng DB riêng.

> **Tiếp:** [README.md](README.md) — mục lục nghiệp vụ.
