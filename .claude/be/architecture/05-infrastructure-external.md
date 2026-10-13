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
| `DataReader/Models/SheetHeaderRow.cs` | Dữ liệu thô lớp con trả cho lớp nền: tên sheet + các ô dòng `1` |

## IMailService

Interface nằm ở `sh.be.applications/Base/Interfaces/IMailService.cs`:

| Method | Dùng khi |
|---|---|
| `SendAsync(to, subject, body)` | Gửi email tuỳ ý |
| `SendAccountCreatedAsync(to, userName, password)` | Tạo user xong, gửi thông tin đăng nhập |

**Vì sao interface ở application, không ở external:** application chỉ nói "gửi mail báo tạo tài khoản"; tiêu
đề (`MailTemplateConstants`) và HTML là chi tiết của tầng ngoài. Sửa giao diện email không chạm `UserService`.

## Cấu hình

Section `Email` trong `appsettings.json`, bind vào `EmailSettings` (`sh.be.shared/Settings`):
`Smtp.Host`, `Smtp.Port`, `Smtp.UserName`, `Smtp.Password`, `Smtp.EnableSsl`, `DefaultFromAddress`,
`DefaultFromDisplayName`.

## IDataReader — đọc header theo từng sheet

Interface ở `sh.be.applications/Base/Interfaces/IDataReader.cs`: `GetSheetHeadersAsync(source)` trả
`List<SheetHeaderDto>` (`{ sheetName, headers }`, đúng thứ tự sheet). Dùng **template method**:

| Lớp | Việc |
|---|---|
| `DataReaderBase` | `GetSheetHeadersAsync` gọi `ReadHeaderRowsAsync` rồi bỏ ô trống, trim — viết **một lần** cho mọi nguồn |
| `GoogleSheetReader` | Override `ReadHeaderRowsAsync`: tách `spreadsheetId` từ link (sai → `202`), `spreadsheets.get` lấy tên sheet, một lần `values.batchGet` range `'Tên sheet'!1:1` cho mọi sheet; `GoogleApiException` → `203` |

Thêm nguồn mới (ví dụ Excel): tạo `DataReader/Implements/ExcelReader : DataReaderBase`, chỉ override
`ReadHeaderRowsAsync`. ⚠️ Khi có từ hai reader trở lên, `IDataReader` không còn biết lấy implement nào (DI trả cái đăng ký
sau cùng) — cần keyed service hoặc factory theo loại nguồn; đừng đăng ký thêm `AddScoped<IDataReader, ...>` rồi để vậy.

Cấu hình: section `GoogleSheet` bind vào `GoogleSheetSettings` — `ApplicationName`, `CredentialsPath` (file JSON service
account, mặc định `Credentials/google-service-account.json`, tương đối theo thư mục chạy app). Thư mục `sh.be.api/Credentials/`
nằm trong `.gitignore` vì file chứa private key. Sheet phải được chia sẻ cho `client_email` của service account.

⚠️ Dùng `CredentialFactory.FromFile<ServiceAccountCredential>(...)`, **không** `GoogleCredential.FromFile`: method sau bị
Google đánh dấu lỗi thời (nhận mọi loại credential, rủi ro bảo mật) và sinh warning CS0618.

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
