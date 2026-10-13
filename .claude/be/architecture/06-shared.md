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
| `Constants/Db/DbSchemas.cs` | Schema `auth`, `core` |
| `Constants/Mail/`, `Constants/Swagger/` | Tiêu đề email; tên module Swagger |
| `Interfaces/IFullAudited.cs` | `ICreatedBy`, `IModifiedBy`, `ISoftDeleted` (chỉ `get`) |
| `Settings/` | `AuthServerSettings`, `EmailSettings`, `GoogleSheetSettings` |
| `Ultils/` | `TreeUtils`, `ValidationUtils`, `CryptoUtils`, `AuditLogUtils`, `DateTimeUtils` |

⚠️ Thư mục tên `Ultils` (sai chính tả) nhưng namespace là `sh.be.shared.Utils`. Đừng đổi tên thư mục nếu không
được yêu cầu.

## Mã lỗi

| Dải | Module | Ví dụ |
|---|---|---|
| `1`, `400`–`500` | Chung | `NotFound = 404`, `Found = 409`, `InternalServerError = 500` |
| `101`–`199` | Auth | `AuthErrorUserNotFound = 103`, `AuthErrorRoleNotFound = 105` |
| `201`–`299` | Core | `CoreErrorAuditLogNotFound = 201`; nguồn dữ liệu (`IDataReader`, dùng chung mọi module): `CoreErrorInvalidDataSource = 202`, `CoreErrorReadDataSourceFailed = 203` |
| `301`–`399` | Construction | `ConstructionErrorDocumentTypeNotFound = 301`, `ConstructionErrorDocumentFieldNotFound = 302` |

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
