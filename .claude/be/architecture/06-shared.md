# Shared Kernel — sh.be.shared

> **Phần 6/8** · trước: [05-infrastructure-external.md](05-infrastructure-external.md) · mục lục: [README.md](README.md)

Phần dùng chung cho mọi project. Không tham chiếu project nào khác.

## Thư mục

| Thư mục | Chứa gì |
|---|---|
| `HttpRequest/Response/ApiResponse.cs` | Envelope `{ status, data, code, message }`, `StatusCodeE` (`Success = 1`, `Error = 0`) |
| `HttpRequest/Error/` | `ErrorCodes`, `ErrorMessages` (message tiếng Việt theo mã) |
| `HttpRequest/AppException/` | `BaseException`, `UserFriendlyException(errorCode, message?)` |
| `HttpRequest/BaseRequest/` | `BaseRequestPagingDto`, `BaseResponsePagingDto<T>`, `query.Paging(dto)` |
| `Constants/Auth/` | `PermissionConstants`, `RoleConstants`, `UserConstants`, claim, tham số login, `ProgramExtensions` |
| `Constants/Core/AuditLogConstants.cs` | Action, action type, `SOURCE_SYSTEM` của nhật ký |
| `Constants/Construction/ProjectConfigConst.cs` | Tên các dòng cấu hình Google, `SECRET_NAMES`, `MASKED_VALUE = "******"`, hạn `state` 10 phút |
| `Constants/Construction/ProjectConst.cs` | Trạng thái dự án: `DEPLOYING = 1` (đang triển khai), `DOING = 2` (đang làm), `ACCEPTING = 3` (đang nghiệm thu), `DONE = 4` (đã xong) |
| `Constants/Construction/SubmissionConst.cs` | Trạng thái kiểm tra hồ sơ `NOT_CHECKED = 0`, `CHECKED = 1`; tham số import (khoá 5 giây, lô bulk 100.000, ngưỡng song song 5.000 dòng, ký tự nối khoá) |
| `Constants/DigitalSignature/` | `TemplateConst` (mặc định / sàn / trần độ đậm, độ dày nét, màu, font, cỡ chữ của mẫu chữ ký; `DIGITAL_SIGNATURE_FONTS`, `COLOR_PATTERN`, đuôi / trần 5 MB và tiền tố object key ảnh), `TemplatePositionConst` (loại khối: `DIGITAL_SIGNATURE = 0`, `STAMP = 1`, `WET_SIGNATURE = 2`), `SigningBatchConst` (trạng thái lô `NEW = 0` … `PAUSED = 5`, nhóm trạng thái, trần 200 file / 512 MB mỗi lượt tải, tiền tố key nguồn / bản ký, **núm runner**: 3 lô, 4 kết nối TSA, cầu dao 5, đợt 8, chạy trước 16, long-poll 25 s, chờ ký 120 s; lý do dừng), `PdfSigningConst` (OID, chỗ trống chữ ký 32 768 byte, kích thước khối chữ ký, font), `TimestampConst` (URL TSA, 3 lần thử), `SigningBatchFileConst` (trạng thái file `PENDING = 0` … `FAILED = 3`), `PluginConst` (tên bộ cài, whitelist mặc định), `CertificateConst` (file + pin SHA-256 các CA Ban Cơ yếu) |
| `Constants/Db/DbSchemas.cs` | Schema `auth`, `core`, `construction`, `digitalsignature` |
| `Constants/Mail/`, `Constants/Swagger/` | Tiêu đề email; tên module Swagger |
| `Interfaces/IFullAudited.cs` | `ICreatedBy`, `IModifiedBy`, `ISoftDeleted` (chỉ `get`) |
| `Settings/` | `AuthServerSettings`, `EmailSettings`, `GoogleSheetSettings`, `S3Settings` (section `S3`, khoá `S3_URL`…), `PluginSettings` (section `Plugin`, `SupportedVersions`) |
| `Ultils/` | `TreeUtils`, `ValidationUtils`, `CryptoUtils`, `AuditLogUtils`, `DateTimeUtils` |

⚠️ Thư mục tên `Ultils` (sai chính tả) nhưng namespace là `sh.be.shared.Utils`. Đừng đổi tên thư mục nếu không
được yêu cầu.

## Mã lỗi

| Dải | Module | Ví dụ |
|---|---|---|
| `1`, `400`–`500` | Chung | `NotFound = 404`, `Found = 409`, `InternalServerError = 500` |
| `101`–`199` | Auth | `AuthErrorUserNotFound = 103`, `AuthErrorRoleNotFound = 105` |
| `201`–`299` | Core | `CoreErrorAuditLogNotFound = 201`; nguồn dữ liệu (`IDataReader`, dùng chung mọi module): `CoreErrorInvalidDataSource = 202`, `CoreErrorReadDataSourceFailed = 203`, `CoreErrorSheetNotFound = 204`, `CoreErrorCreateDataSourceFailed = 205`, `CoreErrorUpdateDataSourceFailed = 206`, `CoreErrorAuthorizeDataSourceFailed = 207` (Google Sheet); kho file (`IFileStorage`): `CoreErrorStorageNotConfigured = 208`, `CoreErrorUploadFileFailed = 209` |
| `301`–`399` | Construction | `...DocumentTypeNotFound = 301`, `...DocumentFieldNotFound = 302`, `...ProjectNotFound = 303`, `...ProjectConfigNotFound = 304`; hồ sơ / import `305`–`315` (bảng ở [../nghiep-vu/11-ho-so-import.md](../nghiep-vu/11-ho-so-import.md)) |
| `601`–`699` | DigitalSignature | `DigitalSignatureError...`: `TemplateNotFound = 601`, mẫu chữ ký `602`–`603`, `SigningBatchNotFound = 604`, lô ký `605`–`609`, `SigningBatchFileNotFound = 610`, `SigningBatchFileNotDeletable = 611`, `InvalidTemplateImage = 612`, `PluginSetupMissing = 613`, `InvalidCertificate = 614`, `UntrustedCertificate = 615`, `SigningBatchNotOwned = 616`, `TooManyUploadFiles = 617`, `SigningBatchNotSignable = 618`, phiên / điều khiển lô `619`–`624`, PDF / chữ ký / TSA / plugin `625`–`635` (bảng ở [../nghiep-vu/10-lo-ky.md](../nghiep-vu/10-lo-ky.md)); `CoreErrorDownloadFileFailed = 210`. Bỏ qua `401`–`599` vì đụng mã chung `400`/`401`/`404`/`409`/`500` |

Thêm mã mới: hằng trong `ErrorCodes` (đúng dải module) + message trong `ErrorMessages`.

## Utils đáng chú ý

| Hàm | Ghi chú |
|---|---|
| `TreeUtils.BuildTree`, `Flatten`, `SyncChecked` | Dựng và lan truyền cây quyền cha–con |
| `ValidationUtils.ValidatePassword` | Kiểm mật khẩu không rỗng và khớp xác nhận; độ mạnh do Identity kiểm khi lưu |
| `CryptoUtils.GenerateRandomString` | Sinh mật khẩu bằng `RandomNumberGenerator` (an toàn mật mã) |
| `DateTimeUtils.GetVietnamTime` | Chỉ cho code tĩnh (seeder); service dùng `BaseService.GetVietnamTime()` |

⚠️ Đừng quay lại `System.Random` để sinh mật khẩu: chuỗi của nó đoán được.

## Thoả hiệp đã biết

`sh.be.shared.csproj` có `FrameworkReference Microsoft.AspNetCore.App` (DTO phân trang dùng `[FromQuery]`), nên
domain và application về kỹ thuật vẫn thấy được kiểu của ASP.NET Core. Quy ước: **không** dùng kiểu web trong
domain/application ngoài các DTO sẵn có.

> **Tiếp:** [07-conventions.md](07-conventions.md) — quy ước code bắt buộc.
