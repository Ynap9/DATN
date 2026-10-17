# Tầng Infrastructure External — sh.be.infrastructure.external

> **Phần 5/8** · trước: [04-infrastructure-data.md](04-infrastructure-data.md) · mục lục: [README.md](README.md)

Dịch vụ bên thứ ba không phải database. Chỉ tham chiếu `sh.be.applications` để implement cổng ra mà
application khai báo.

## Thư mục

| Thư mục | Chứa gì |
|---|---|
| `Mail/Implements/MailService.cs` | Implement `IMailService`: gửi SMTP bằng cấu hình `EmailSettings` |
| `Mail/Templates/AccountCreatedMailTemplate.cs` | HTML email báo tạo tài khoản |
| `DataReader/Base/DataReaderBase.cs` | Lớp nền abstract implement `IDataReader`: luồng chung + chuẩn hoá header |
| `DataReader/Implements/GoogleSheetReader.cs` | Đọc header từ Google Sheet bằng service account (`Google.Apis.Sheets.v4`) |
| `Excel/Implements/ExcelTemplateWriter.cs` | Implement `IExcelTemplateWriter`: sinh file `.xlsx` template bằng ClosedXML |
| `DataReader/Models/SheetHeaderRow.cs` | Dữ liệu thô lớp con trả cho lớp nền: tên sheet + các ô dòng `1` |
| `Spreadsheet/Implements/GoogleSheetWriter.cs` | Implement `ISpreadsheetWriter`: tạo / xoá Google Sheet (`Google.Apis.Drive.v3` + Sheets) |
| `FileDownloader/Implements/HttpFileDownloader.cs` | Implement `IFileDownloader.OpenPdfAsync`: `IHttpClientFactory`, `ResponseHeadersRead`, trả **stream** (không nạp RAM); không phải http(s), lỗi mạng / HTTP, `Content-Type` khác `application/pdf` / `application/octet-stream` → `315` |

## IMailService

Interface nằm ở `sh.be.applications/Base/Interfaces/IMailService.cs`:

| Method | Dùng khi |
|---|---|
| `SendAsync(to, subject, body)` | Gửi email tuỳ ý |
| `SendAccountCreatedAsync(to, userName, password)` | Tạo user xong, gửi thông tin đăng nhập |

**Vì sao interface ở application, không ở external:** application chỉ nói "gửi mail báo tạo tài khoản"; tiêu
đề (`MailTemplateConstants`) và HTML là chi tiết của tầng ngoài. Sửa giao diện email không chạm `UserService`.

## Cấu hình

Section `Email` trong `appsettings.json`, bind vào `EmailSettings` (`sh.be.shared/Settings`): `Smtp.Host`, `Smtp.Port`,
`Smtp.UserName`, `Smtp.Password`, `Smtp.EnableSsl`, `DefaultFromAddress`, `DefaultFromDisplayName`.

## IDataReader — đọc header theo từng sheet

Interface ở `sh.be.applications/Base/Interfaces/IDataReader.cs`: `GetSheetHeadersAsync(source)` trả
`List<SheetHeaderDto>` (`{ sheetName, headers }`, đúng thứ tự sheet). Dùng **template method**:

| Lớp | Việc |
|---|---|
| `DataReaderBase` | `GetSheetHeadersAsync` gọi `ReadHeaderRowsAsync` rồi bỏ ô trống, trim — viết **một lần** cho mọi nguồn |
| `GoogleSheetReader` | Override `ReadHeaderRowsAsync`: tách `spreadsheetId` từ link (sai → `202`), `spreadsheets.get` lấy tên sheet, một lần `values.batchGet` range `'Tên sheet'!1:1` cho mọi sheet; `GoogleApiException` → `203` |

`GetHeadersAsync(source, sheetName, headerRowIndex)` trả header **một** sheet ở dòng tuỳ chọn (`>= 1`, sai → `400`); lớp con kiểm sheet có
trong danh sách tên (so đúng chữ, không có → `204`) rồi `values.get` range `'Tên sheet'!n:n`.
`ReadTableAsync(source, sheetName)` trả `SheetTable` cả sheet (import hồ sơ): lớp con `ReadRowsAsync` (`values.get` range `'Tên sheet'`,
kiểm `204`, lỗi `203`) trả ô thô; lớp nền trim mọi ô, **giữ** ô trống để đúng vị trí cột, dòng `1` là header, `SheetRowNumber` = số dòng thật.

Thêm nguồn mới (ví dụ Excel): tạo `DataReader/Implements/ExcelReader : DataReaderBase`, chỉ override
`ReadHeaderRowsAsync`. ⚠️ Khi có từ hai reader trở lên, `IDataReader` không còn biết lấy implement nào (DI trả cái đăng ký
sau cùng) — cần keyed service hoặc factory theo loại nguồn; đừng đăng ký thêm `AddScoped<IDataReader, ...>` rồi để vậy.

Cấu hình: section `GoogleSheet` bind vào `GoogleSheetSettings` — `ApplicationName`, `CredentialsPath` (file JSON service
account, mặc định `Credentials/google-service-account.json`, tương đối theo thư mục chạy app). Thư mục `sh.be.api/Credentials/`
nằm trong `.gitignore` vì file chứa private key. Sheet phải được chia sẻ cho `client_email` của service account.

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

## Thêm một dịch vụ ngoài

1. Khai interface trong `sh.be.applications/Base/Interfaces/` (dùng chung) hoặc
   `Module/<TenModule>/Interfaces/` (riêng module), method mang nghĩa nghiệp vụ.
2. Implement trong `sh.be.infrastructure.external/<TenDichVu>/Implements/`.
3. Settings (nếu có) đặt ở `sh.be.shared/Settings/`, bind trong `Program.cs`.
4. Đăng ký DI trong `Program.cs`.

**Không** cho external tham chiếu `domain` hay `infrastructure.data`; **không** để application tham chiếu
external.

⚠️ Gửi mail đang chạy bên trong transaction của `UserService.Create` (xem
[02-applications.md](02-applications.md)). Dịch vụ ngoài mới đừng gọi bên trong transaction DB nếu tránh được.

> **Tiếp:** [06-shared.md](06-shared.md) — shared kernel dùng chung.
