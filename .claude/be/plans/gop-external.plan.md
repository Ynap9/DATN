# Plan: gom tầng external theo mục, chuẩn tên `External`

> **Trạng thái:** 🔶 làm dở — chốt 2026-10-24; ✅ bước 1–5 (code + tài liệu), còn bước 6 (kiểm tổng).

## Input

- `sh.be.infrastructure.external` có 13 thư mục, 14 cổng ra ở `applications/Base/Interfaces/` mang tên lẫn lộn (`Service`, `Reader`,
  `Writer`, `Provider`, `Client`…). `Excel/` đã gộp vào `DataReader/`.
- Mục tiêu: service và external chỉ nối qua interface (sửa bên này không chạm bên kia), tìm code nhanh, bỏ code trùng.

## Quyết định

| Mục | Chốt |
|---|---|
| Tên | Interface `I<Ten>External` ở applications, class `<NhaCungCap><Ten>External` ở external. Field trong service `_<ten>External` |
| Thư mục external | `<Muc>/{Base,Implements,Models}`; 4 mục `DataReader`, `Storage`, `Mail`, `DigitalSignature` (+ `BackgroundJob` cho Hangfire) |
| Thư mục interface | `applications/Base/Interfaces/External/<Muc>/`, namespace `sh.be.applications.Base.Interfaces.External.<Muc>`. `IRequestContext`, `IAuthorizationStateProtector` (api implement) → `Base/Interfaces/Api/`, giữ tên. Gốc `Base/Interfaces` không còn file |
| Thư mục DTO | `applications/Base/Dtos/<Muc>/` cùng 5 mục; không đổi tên class DTO, không đổi JSON |
| Gộp | Excel template + table → `IExcelExternal`; Google writer + authorizer → `IGoogleSheetExternal`. Reader Google giữ riêng vì dùng service account và kế thừa lớp nền đa hình |

## Bảng đổi tên

| Mục | Interface cũ → mới | Class cũ → mới |
|---|---|---|
| Mail | `IMailService` → `IMailExternal` | `MailService` → `MailExternal` |
| Storage | `IFileStorage` → `IFileStorageExternal` | `S3FileStorage` → `S3FileStorageExternal` |
| Storage | `IFileDownloader` → `IFileDownloadExternal` | `HttpFileDownloader` → `HttpFileDownloadExternal` |
| Storage | `ISampleFileReader` → `ISampleFileExternal` | `SampleFileReader` → `SampleFileExternal` (file nhúng `SampleFile/Files` → `Storage/Files`) |
| Storage | `IPluginSetupProvider` → `IPluginSetupExternal` | `PluginSetupProvider` → `PluginSetupExternal` |
| DataReader | `IDataReader` → `IDataReaderExternal` | `DataReaderBase` → `DataReaderExternalBase`, `GoogleSheetReader` → `GoogleSheetReaderExternal` |
| DataReader | `IExcelTemplateWriter` + `IExcelTableWriter` → `IExcelExternal` | `ExcelTemplateWriter` + `ExcelTableWriter` → `ExcelExternal` |
| DataReader | `ISpreadsheetWriter` + `ISpreadsheetAuthorizer` → `IGoogleSheetExternal` | `GoogleSheetWriter` + `GoogleSheetAuthorizer` → `GoogleSheetExternal`; `Spreadsheet/Base/*` → `DataReader/Base/` |
| DigitalSignature | `IPdfSigner` → `IPdfSignerExternal` | `PdfSigner` → `PdfSignerExternal`; `Pdf/Base/*` → `DigitalSignature/Base/`, `Pdf/Models/*` → `DigitalSignature/Models/` |
| DigitalSignature | `ISigningQueue` → `ISigningQueueExternal` | `SigningQueue` → `SigningQueueExternal`; `Signing/Base/CmsBuilder` → `DigitalSignature/Base/` |
| DigitalSignature | `ITimestampClient` → `ITimestampExternal` | `TimestampClient` → `TimestampExternal` |
| DigitalSignature | `ICertificateValidator` → `ICertificateValidatorExternal` | `CertificateValidator` → `CertificateValidatorExternal` |

## Steps

Mỗi phần: `git mv` (giữ lịch sử), đổi tên / namespace, sửa service dùng cổng đó + `Program.cs`, build sạch, dừng cho review.

1. ✅ Mail — làm mẫu cho cách đặt tên và thư mục.
2. ✅ Storage — kiểm lại tên embedded resource sau khi chuyển `SampleFile/Files`. ✅ DTO `File/`, `Plugin/` → `Base/Dtos/Storage/`;
   ✅ `IRequestContext`, `IAuthorizationStateProtector` → `Base/Interfaces/Api/`.
   ✅ 9 cổng còn lại → `External/DataReader/`, `External/DigitalSignature/` (giữ tên cũ); DTO `Excel/`, `Spreadsheet/` → `Dtos/DataReader/`,
   `Signing/` → `Dtos/DigitalSignature/`. Applications xong phần thư mục.
3. ✅ DataReader — đổi tên `...External`, gộp Excel, gộp Google writer + authorizer, chuyển `external/Spreadsheet/` vào `DataReader/`.
   ✅ (mở rộng) `domain/Module/Construction` gom theo nghiệp vụ → vai trò: `{DocumentTypes,DocumentFields,Projects,Submissions}/
   {Entities,Repositories,Services}`; `Check/`, `Import/` vào `Submissions/Services/`. Migration cũ giữ nguyên (chỉ chứa chuỗi tên kiểu).
4. ✅ DigitalSignature — đổi tên, chuyển `external/{Pdf,Signing,Timestamp,Certificate}/` vào `DigitalSignature/`.
5. ✅ Tài liệu: bảng interface ở `architecture/README.md`, `05-infrastructure-external.md`, `09-chu-ky-so.md`, quy ước tên ở `07-conventions.md`,
   bước "thêm dịch vụ ngoài" ở `08-them-chuc-nang.md`.
6. Build `dotnet build sh.be/sh.be.api/sh.be.api.sln`, chạy API, gọi thử một API của mỗi mục.

## Expected output

Tầng external còn 4 mục (+ `BackgroundJob`), mọi cổng ra tên `I...External`, mọi implement tên `...External`. Hành vi API không đổi,
trừ một điểm ở dưới. Hangfire nằm trong mục `BackgroundJob/` (đã thi công).

## Điểm cần chú ý

- Gộp Excel: file template dùng chung cách giãn cột của file xuất (kẹp rộng 8–60) — khác nhỏ so với hiện tại, người dùng đã đồng ý.
- ⚠️ Thư mục nghiệp vụ ở domain dùng **số nhiều** (`Submissions/`): namespace `...Construction.Submission` sẽ che entity `Submission`
  trong code domain (CS0118). Cùng loại bẫy với `Hangfire.BackgroundJob`.
- `ISampleFileExternal` (Storage) dùng `ExportFileDto` nằm ở `Dtos/DataReader/` — phụ thuộc chéo mục, chấp nhận vì DTO là file xuất Excel.
- Đổi tên là đổi chữ ký nội bộ, không đổi route / DTO / schema; FE không bị ảnh hưởng.
