# Tầng Infrastructure External — sh.be.infrastructure.external

> **Phần 5/8** · trước: [04-infrastructure-data.md](04-infrastructure-data.md) · mục lục: [README.md](README.md)

Dịch vụ bên thứ ba không phải database. Chỉ tham chiếu `sh.be.applications` để implement cổng ra mà
application khai báo.

## Thư mục

| Thư mục | Chứa gì |
|---|---|
| `Mail/Implements/MailService.cs`, `Mail/Templates/AccountCreatedMailTemplate.cs` | Implement `IMailService`: gửi SMTP bằng `EmailSettings`; HTML email báo tạo tài khoản |
| `DataReader/Base/DataReaderBase.cs`, `Implements/GoogleSheetReader.cs`, `Models/SheetHeaderRow.cs` | `IDataReader`: lớp nền chuẩn hoá header + đọc Google Sheet bằng service account |
| `Excel/Implements/ExcelTemplateWriter.cs` | Implement `IExcelTemplateWriter`: sinh file `.xlsx` template bằng ClosedXML |
| `Spreadsheet/Implements/GoogleSheetWriter.cs` | Implement `ISpreadsheetWriter`: tạo / xoá Google Sheet (`Google.Apis.Drive.v3` + Sheets) |
| `FileDownloader/Implements/HttpFileDownloader.cs` | Implement `IFileDownloader.OpenPdfAsync`: `IHttpClientFactory`, `ResponseHeadersRead`, trả **stream** (không nạp RAM); không phải http(s), lỗi mạng / HTTP, `Content-Type` khác `application/pdf` / `application/octet-stream` → `315` |
| `Storage/Implements/S3FileStorage.cs` | Implement `IFileStorage`: đẩy (công khai / riêng tư), tải, link ký sẵn, xoá object trên MinIO (`AWSSDK.S3`) |
| `SampleFile/Implements/SampleFileReader.cs` | Implement `ISampleFileReader`: trả PDF mẫu nhúng `SampleFile/Files/*.pdf` (embedded resource) |
| `Plugin/Implements/PluginSetupProvider.cs` | Implement `IPluginSetupProvider`: whitelist phiên bản plugin + mở bộ cài `Plugins/Ký số plugin.exe` |
| `Certificate/Implements/CertificateValidator.cs` | Implement `ICertificateValidator` (singleton): đọc chứng thư base64, dựng chuỗi tin cậy về CA Ban Cơ yếu đã ghim |
| `Pdf/Implements/PdfSigner.cs` + `Pdf/Base/*`, `Pdf/Models/*` | Implement `IPdfSigner` (singleton): đọc / nối bản PDF, vẽ khối chữ ký (PDFsharp 6.2.1), ghép + tự kiểm chữ ký |
| `Signing/Base/CmsBuilder.cs`, `Signing/Implements/SigningQueue.cs` | Dựng `SignedAttributes` + CMS (CAdES); `ISigningQueue` (singleton): phiên ký và hàng đợi yêu cầu cho plugin |
| `Timestamp/Implements/TimestampClient.cs` | Implement `ITimestampClient`: RFC 3161 tới TSA Ban Cơ yếu qua `IHttpClientFactory` (client `Timestamp`, timeout 180 s) |

## IMailService

Interface ở `sh.be.applications/Base/Interfaces/IMailService.cs`:
| Method | Dùng khi |
|---|---|
| `SendAsync(to, subject, body)` | Gửi email tuỳ ý |
| `SendAccountCreatedAsync(to, userName, password)` | Tạo user xong, gửi thông tin đăng nhập |

**Vì sao interface ở application, không ở external:** application chỉ nói "gửi mail báo tạo tài khoản"; tiêu đề (`MailTemplateConstants`) và HTML là chi tiết của tầng ngoài. Sửa giao diện email không chạm `UserService`. Cấu hình: section `Email` bind vào `EmailSettings` (`Smtp.*`, `DefaultFromAddress`, `DefaultFromDisplayName`).

## IDataReader — đọc header theo từng sheet

Interface ở `sh.be.applications/Base/Interfaces/IDataReader.cs`: `GetSheetHeadersAsync(source)` trả `List<SheetHeaderDto>` (`{ sheetName, headers }`, đúng thứ tự sheet). Dùng **template method**:

| Lớp | Việc |
|---|---|
| `DataReaderBase` | `GetSheetHeadersAsync` gọi `ReadHeaderRowsAsync` rồi bỏ ô trống, trim — viết **một lần** cho mọi nguồn |
| `GoogleSheetReader` | Override `ReadHeaderRowsAsync`: tách `spreadsheetId` từ link (sai → `202`), `spreadsheets.get` lấy tên sheet, một lần `values.batchGet` range `'Tên sheet'!1:1` cho mọi sheet; `GoogleApiException` → `203` |

Method thứ hai `GetHeadersAsync(source, sheetName, headerRowIndex)` trả header **một** sheet ở dòng tuỳ chọn (đếm từ `1`): lớp nền kiểm `headerRowIndex >= 1` (sai → `400`) rồi chuẩn hoá như trên; `GoogleSheetReader.ReadHeaderCellsAsync` kiểm sheet có trong danh sách tên (so đúng chữ, không có → `204`) rồi `values.get` range `'Tên sheet'!n:n`.
`ReadTableAsync(source, sheetName)` trả `SheetTable` cả sheet (import hồ sơ): lớp con `ReadRowsAsync` (`values.get` range `'Tên sheet'`,
kiểm `204`, lỗi `203`) trả ô thô; lớp nền trim mọi ô, **giữ** ô trống để đúng vị trí cột, dòng `1` là header, `SheetRowNumber` = số dòng thật.

Thêm nguồn mới (ví dụ Excel): tạo `DataReader/Implements/ExcelReader : DataReaderBase`, chỉ override `ReadHeaderRowsAsync`. ⚠️ Khi có từ hai reader trở lên,
`IDataReader` không còn biết lấy implement nào (DI trả cái đăng ký sau cùng) — cần keyed service hoặc factory theo loại nguồn; đừng đăng ký thêm `AddScoped<IDataReader, ...>` rồi để vậy.

Cấu hình: section `GoogleSheet` bind vào `GoogleSheetSettings` — `ApplicationName`, `CredentialsPath` (file JSON service account, mặc định `Credentials/google-service-account.json`,
tương đối theo thư mục chạy app). Thư mục `sh.be.api/Credentials/` nằm trong `.gitignore` vì file chứa private key. Sheet phải được chia sẻ cho `client_email` của service account.
⚠️ Dùng `CredentialFactory.FromFile<ServiceAccountCredential | UserCredential>(...)`, **không** `GoogleCredential.FromFile` (lỗi thời, CS0618).

## ISpreadsheetWriter, ISpreadsheetAuthorizer — tạo Google Sheet bằng tài khoản Gmail

Interface ở `sh.be.applications/Base/Interfaces/`; mọi method nhận `SpreadsheetAccountDto` (client id/secret, redirect URI, refresh token,
folder id) do service đọc từ bảng `ProjectConfig` ([../nghiep-vu/08-cau-hinh-du-an.md](../nghiep-vu/08-cau-hinh-du-an.md)).

| Method | Việc |
|---|---|
| `CreateSpreadsheetAsync(account, title, sheetName)` | Drive `files.create` trong `FolderId`, Sheets `batchUpdate` đổi tên sheet `0` (lỗi thì xoá file); `GoogleApiException` → `205` |
| `RenameSpreadsheetAsync` / `TryRename...` / `TryDeleteSpreadsheetAsync` | Drive `files.update` / `files.delete`; lỗi → `206`, bản `Try` trả `false` |
| `ISpreadsheetAuthorizer.BuildAuthorizationUrl` / `ExchangeRefreshTokenAsync` | Link consent (`access_type=offline`, `prompt=consent`) / đổi `code` lấy refresh token; lỗi → `207` |
| `Spreadsheet/Base/GoogleOAuthFlowFactory`, `GoogleSheetUrl` | Dựng flow + `UserCredential` từ account (scope `drive` + `spreadsheets`); đổi URL ↔ id (link sai → `202`) |

Refresh token hết hạn / bị thu hồi (`TokenResponseException`) → `207`, phải xác thực lại. Reader vẫn dùng service account nên thư mục vẫn
chia sẻ cho `client_email`. ⚠️ Writer dùng service account thì Google trả `403 storageQuotaExceeded` (service account không có dung lượng,
Gmail cá nhân không có Shared Drive) — đừng quay lại. ⚠️ OAuth consent screen ở **Testing** thì refresh token hết hạn sau 7 ngày.

## IExcelTemplateWriter — sinh file Excel template

Interface ở `sh.be.applications/Base/Interfaces/IExcelTemplateWriter.cs`: `CreateTemplate(templateName, headers)` trả
`ExportFileDto` (`FileName`, `ContentType`, `Content`). Implement `Excel/Implements/ExcelTemplateWriter` bằng **ClosedXML**
(MIT): một sheet, header dòng `1` in đậm + freeze, tự giãn cột. Luật tên sheet / tên file của Excel (31 ký tự, ký tự cấm)
nằm ở đây, không ở service. Controller trả `File(...)`; lỗi vẫn trả `Ok(OkException(ex))` để giữ envelope `ApiResponse`.

## IFileStorage — kho file MinIO

`UploadAsync` đặt `PublicRead`, trả URL công khai (ảnh mẫu chữ ký); `UploadPrivateAsync` cho file nguồn / bản ký; lỗi → `209`. `DownloadAsync` lỗi → `210`. `GetTemporaryUrl(key, fileName, lifetime)` trả link ký sẵn kèm `Content-Disposition` (tên gốc). `DeleteAsync` / `DeleteByPrefixAsync` chỉ ghi log khi lỗi. Section `S3` → `S3Settings`; thiếu URL / bucket → `208` lúc gọi đầu tiên; `ForcePathStyle = true` cho MinIO. ⚠️ Bucket phải tạo sẵn: thiếu bucket thì upload trả `209`.

## IPluginSetupProvider, ICertificateValidator — plugin và chứng thư số

Bộ cài là bản build của repo `Ký số Plugin` (`Window/ks.plugin/bo-cai`, hiện `3.0.0`), chép vào `sh.be.api/Plugins/`, csproj chép sang output; tên file phải đúng `PluginConst.SETUP_FILE_NAME`. Whitelist ở section `Plugin` → `PluginSettings.SupportedVersions` (rỗng → `PluginConst.DEFAULT_SUPPORTED_VERSIONS`), đọc qua `IOptionsMonitor` nên sửa appsettings không cần build lại. ⚠️ Exe ~160 MB nằm trong `.gitignore` (`Plugins/*.exe`): máy chủ phải chép tay, thiếu thì `setup` trả `exists = false`, `setup/content` trả `613`.
`CertificateValidator` nạp **một lần** 5 file `sh.be.api/Cert/*.crt` (port từ kssm, csproj chép sang output), mỗi file đối chiếu SHA-256 với `CertificateConst` — thiếu file / sai pin thì nhánh đó bị bỏ (fail-closed, log cảnh báo). `Load(base64)` hỏng → `614`. `IsTrusted(cert, thời điểm UTC)` đạt **một** trong hai nhánh: G1 (`rootca` + Sub-CA `cp`) hoặc G2 (`rootcag2` + CA trung gian `cpg2`, `dcscag2`); `CustomRootTrust` nên **không** tin kho chứng thư Windows, revocation `NoCheck` (chứng thư bị thu hồi vẫn qua). Plugin liệt kê chứng thư ở máy người dùng nên server **không** đọc kho chứng thư như API `chung-thu-so` cũ của kssm; ⚠️ đừng tin cờ tin cậy client gửi lên, luôn gọi `IsTrusted`.

## IPdfSigner, ITimestampClient, ISigningQueue — ký số

Port từ kssm, nghiệp vụ ở [../nghiep-vu/10-lo-ky.md](../nghiep-vu/10-lo-ky.md). `CreateContext` (một lần mỗi lô: chuỗi CA, `signingCertificateV2`, cache ảnh đã vẽ) → `Prepare` (nối bản có lỗ `/Contents` 32 768 byte, vá `/ByteRange`, trả `SignedAttributes` cho token) → `Complete` (ghép CMS với token TSA rồi tự kiểm `CheckSignature`, ByteRange phủ trọn file, có token TSA; sai → `631`). Lỗi PDF `625`–`629`, `635`. `SigningQueue.CopySessionCertificate` trả **bản sao** vì đóng phiên sẽ dispose chứng thư gốc.
⚠️ PDFsharp giữ font cache toàn tiến trình: mọi lệnh vẽ nằm trong một `lock`, và `GlobalFontSettings.FontResolver` chỉ đặt được **một lần** — `PdfSigner` phải là singleton. ⚠️ Model `PdfIndirectObject` không được đặt tên `PdfObject` (trùng `PdfSharp.Pdf.PdfObject`).

## Thêm một dịch vụ ngoài

1. Interface trong `sh.be.applications/Base/Interfaces/` (dùng chung) hoặc `Module/<TenModule>/Interfaces/` (riêng module).
2. Implement trong `sh.be.infrastructure.external/<TenDichVu>/Implements/`; settings ở `sh.be.shared/Settings/`.
3. Bind settings và đăng ký DI trong `Program.cs`. **Không** cho external tham chiếu `domain` / `infrastructure.data`, **không** để application tham chiếu external.

⚠️ Gửi mail đang chạy trong transaction của `UserService.Create` ([02-applications.md](02-applications.md)). Dịch vụ ngoài mới đừng gọi trong transaction DB nếu tránh được (`TemplateService.UpdateConfig` đẩy ảnh trước, xoá ảnh cũ sau commit).

> **Tiếp:** [06-shared.md](06-shared.md) — shared kernel dùng chung.
