# Shared Kernel — sh.be.shared

> **Phần 6/9** · trước: [05-infrastructure-external.md](05-infrastructure-external.md) · mục lục: [README.md](README.md)

Phần dùng chung cho mọi project. Không tham chiếu project nào khác.

## Thư mục

| Thư mục | Chứa gì |
|---|---|
| `HttpRequest/Response/ApiResponse.cs` | Envelope `{ status, data, code, message }`, `StatusCodeE` (`Success = 1`, `Error = 0`) |
| `HttpRequest/Error/` | `ErrorCodes`, `ErrorMessages` (message tiếng Việt theo mã) |
| `HttpRequest/AppException/` | `BaseException`, `UserFriendlyException(errorCode, message?)`; `Message` trả câu nghiệp vụ (`message` truyền vào, không có thì `ErrorMessages`) để log và job Hangfire lưu đúng câu |
| `HttpRequest/BaseRequest/` | `BaseRequestPagingDto`, `BaseResponsePagingDto<T>`, `query.Paging(dto)` |
| `Constants/Auth/` | `PermissionConstants`, `RoleConstants` (+ `EXCLUSIVE_STAFF_ROLES` = `Data`, `QA`: một user chỉ giữ một trong hai), `UserConstants`, claim, tham số login, `ProgramExtensions` |
| `Constants/Core/AuditLogConstants.cs` | Action, action type, `SOURCE_SYSTEM` của nhật ký |
| `Constants/Construction/ProjectConfigConst.cs` | Tên các dòng cấu hình Google, `SECRET_NAMES`, `MASKED_VALUE = "******"`, hạn `state` 10 phút |
| `Constants/Construction/ProjectConst.cs` | Trạng thái dự án: `DEPLOYING = 1` (đang triển khai), `DOING = 2` (đang làm), `ACCEPTING = 3` (đang nghiệm thu), `DONE = 4` (đã xong) |
| `Constants/Construction/QaAssignmentConst.cs` | Chấm mẫu QA: `MODE_FULL` / `MODE_SAMPLE` (`MODES`), mặc định cấu hình mẫu (`DEFAULT_SAMPLE_SIZE = 20`, `DEFAULT_THRESHOLD_PERCENT = 50`, `DEFAULT_STRATEGY = "SIMPLE_RANDOM"`, `DEFAULT_STOP_EARLY`), giới hạn (`SAMPLE_SIZE_MAX = 5000`, `THRESHOLD_MAX = 100`, `STRATEGY_CODE_PATTERN`, độ dài cột), `TICKET_STATUS_*` và `TICKET_MAX_DOCUMENTS` / `UPDATE_CHUNK_SIZE` cho 18C, `SUBMISSION_USER_ROLES` (`Data`, `QA`) ([../nghiep-vu/18-qa-cham-mau.md](../nghiep-vu/18-qa-cham-mau.md)) |
| `Constants/Construction/SubmissionConst.cs` | Trạng thái kiểm tra hồ sơ `NOT_CHECKED = 0`, `CHECKED = 1`, `RETURNED = 2`; trạng thái QA `QA_NOT_REVIEWED = 0`, `QA_PASSED = 1`, `QA_FAILED = 2`, `QA_RESULTS`, `QA_REASON_MAX_LENGTH = 1000`; thống kê QA: `QA_STATS_*` (30 ngày mặc định, tối đa 366, `orderBy`), `UNKNOWN_ENTRY_USER_NAME`, `DELETED_FIELD_NAME_SUFFIX`, `TIMELINE_*` (500 sự kiện, loại `CHECK` / `QA`); tham số import (khoá 5 giây, lô bulk 100.000, ngưỡng song song 5.000 dòng, ký tự nối khoá); xem / kiểm tra: `MAX_PAGE_SIZE = 200`, `CHECK_NOTE_MAX_LENGTH = 1000`; xuất Excel: `EXPORT_MAX_ROWS`, `EXPORT_*` của job xuất nền (loại job, tiền tố key MinIO, link 15 phút, giữ file 1 ngày, quét lại 7 ngày) |
| `Constants/DigitalSignature/` | `TemplateConst` (mặc định / sàn / trần độ đậm, độ dày nét, màu, font, cỡ chữ của mẫu chữ ký; `DIGITAL_SIGNATURE_FONTS`, `COLOR_PATTERN`, đuôi / trần 5 MB và tiền tố object key ảnh), `TemplatePositionConst` (loại khối: `DIGITAL_SIGNATURE = 0`, `STAMP = 1`, `WET_SIGNATURE = 2`), `SigningBatchConst` (trạng thái lô `NEW = 0` … `PAUSED = 5`, `RUNNING_STATUSES`, trần 200 file / 512 MB mỗi lượt tải, song song 4, tiền tố key nguồn), `SigningBatchFileConst` (trạng thái file `PENDING = 0` … `FAILED = 3`), `PluginConst` (tên bộ cài, whitelist mặc định), `CertificateConst` (file + pin SHA-256 các CA Ban Cơ yếu) |
| `Constants/Packaging/` | `PackageConst` (loại gói `AIP_HOSO = 1` … `DIP = 5` theo số phụ lục TT05, `PACKAGE_TYPES`, `PACKAGE_TYPE_CODES`, `PACKAGE_TYPE_NAMES`), `PackageSchemaConst` (loại metadata `METADATA_PACKAGE = 1` … `METADATA_MEDIA = 4`, `METADATA_TYPES`, `NAME_MAX_LENGTH = 31` = trần tên sheet Excel), `PackageSchemaFieldConst` (kiểu `DATA_TYPE_TEXT = 1` … `DATA_TYPE_BOOLEAN = 4`; `REQUIRED = 1`, `OPTIONAL = 2`, `CONDITIONAL = 3`), `PackageCodeGroupConst` (13 tên nhóm mã). Mọi hằng bắt đầu từ `1` để `0` không lẫn với giá trị chưa gán |
| `Constants/Operation/` | `StaffGroupConst` (tên 200, mô tả 1000, 500 người / lượt), `ProjectMemberConst` (`ROLES` = `RoleConstants` `Leader` / `Data` / `QA`, không khai hằng vai trò riêng; lý do bỏ qua `SKIP_REASON_*`), `PriceConst` (`PRICE_MAX = 100.000.000`, `decimal(18,2)`), `AssignmentConst` (`MODE_MANUAL = 1`, `RANDOM = 2`, `AUTO = 3`; `STATUS_ACTIVE = 1`, `CANCELLED = 2`, `DONE = 3`; `ENTRY_RETURNS_BEFORE_POOL = 2`). Kế hoạch ở [../plans/van-hanh/README.md](../plans/van-hanh/README.md) |
| `Constants/Db/DbSchemas.cs` | Schema `auth`, `core`, `construction`, `digitalsignature`, `packaging`, `operation`; `hangfire` (bảng do Hangfire tự tạo, không qua EF) |
| `Constants/BackgroundJob/BackgroundJobConst.cs` | `DASHBOARD_PATH = "/hangfire"`, `CONFIG_SECTION = "BackgroundJob"`, `SYSTEM_USER = "system"`, `RETRY_ATTEMPTS = 2`, queue `QUEUE_DEFAULT` / `QUEUE_EXPORT`, `SERVER_ALIVE_SECONDS`, trạng thái `STATUS_*` (0–3), tên tham số job `PARAMETER_*` |
| `Constants/SignalR/SignalRConst.cs` | `HUB_PATH = "/hubs/notification"`, `ACCESS_TOKEN_QUERY`, `CORS_POLICY = "SignalRPolicy"`, sự kiện `EVENT_PING` |
| `Constants/Mail/`, `Constants/Swagger/` | Tiêu đề email; tên module, khuôn tên tài liệu `{version}-{module}`, ghi chú deprecated / sunset của Swagger |
| `Constants/Versioning/ApiVersionConstants.cs` | Version API (`V1 = "1.0"`, `DEFAULT_VERSION`), khuôn nhóm `'v'VVV`, section `ApiVersion` |
| `Interfaces/IFullAudited.cs` | `ICreatedBy`, `IModifiedBy`, `ISoftDeleted` (chỉ `get`) |
| `Settings/` | `AuthServerSettings`, `EmailSettings`, `GoogleSheetSettings`, `S3Settings` (section `S3`, khoá `S3_URL`…), `PluginSettings` (section `Plugin`, `SupportedVersions`), `ApiVersionSettings` (section `ApiVersion`, `Sunsets[]`), `BackgroundJobSettings` (section `BackgroundJob`: `Enabled`, `WorkerCount`, `ExportWorkerCount`, `Jobs.ProjectDeadlineCheck`, `Jobs.SubmissionExportCleanup`) |
| `Ultils/` | `TreeUtils`, `ValidationUtils`, `CryptoUtils`, `AuditLogUtils`, `DateTimeUtils` |

⚠️ Thư mục tên `Ultils` (sai chính tả) nhưng namespace là `sh.be.shared.Utils`. Đừng đổi tên thư mục nếu không
được yêu cầu.

## Mã lỗi

| Dải | Module | Ví dụ |
|---|---|---|
| `1`, `400`–`500` | Chung | `NotFound = 404`, `Found = 409`, `InternalServerError = 500` |
| `101`–`199` | Auth | `AuthErrorUserNotFound = 103`, `AuthErrorRoleNotFound = 105`, `AuthErrorConflictingRoles = 109` (vừa Data vừa QA) |
| `201`–`299` | Core | `CoreErrorAuditLogNotFound = 201`; nguồn dữ liệu (`IDataReaderExternal`, `IGoogleSheetExternal`, dùng chung mọi module): `CoreErrorInvalidDataSource = 202`, `CoreErrorReadDataSourceFailed = 203`, `CoreErrorSheetNotFound = 204`, `CoreErrorCreateDataSourceFailed = 205`, `CoreErrorUpdateDataSourceFailed = 206`, `CoreErrorAuthorizeDataSourceFailed = 207` (Google Sheet); kho file (`IFileStorageExternal`): `CoreErrorStorageNotConfigured = 208`, `CoreErrorUploadFileFailed = 209`, `CoreErrorDownloadFileFailed = 210`; job nền: `CoreErrorBackgroundJobUnavailable = 211` (không còn server Hangfire nghe queue) |
| `301`–`399` | Construction | `...DocumentTypeNotFound = 301`, `...DocumentFieldNotFound = 302`, `...ProjectNotFound = 303`, `...ProjectConfigNotFound = 304`; hồ sơ / import `305`–`315` (bảng ở [../nghiep-vu/11-ho-so-import.md](../nghiep-vu/11-ho-so-import.md)); kiểm tra hồ sơ `316`–`319` ([../nghiep-vu/12-ho-so-kiem-tra.md](../nghiep-vu/12-ho-so-kiem-tra.md)); QA hồ sơ `320`–`321`, `324` ([../nghiep-vu/13-ho-so-qa.md](../nghiep-vu/13-ho-so-qa.md)); xuất Excel `322`, job xuất không tìm thấy `326`, xoá hồ sơ đã kiểm `325`; dự án hết hạn `323` ([../nghiep-vu/15-han-du-an.md](../nghiep-vu/15-han-du-an.md)); phiếu QA / giao QA `327`–`335`, chặn xoá văn bản đang sửa / đang QA `336`, ngoài phạm vi Leader `337` ([../nghiep-vu/18-qa-cham-mau.md](../nghiep-vu/18-qa-cham-mau.md)) |
| `601`–`699` | DigitalSignature | `DigitalSignatureError...`: `TemplateNotFound = 601`, mẫu chữ ký `602`–`603`, `SigningBatchNotFound = 604`, lô ký `605`–`609`, `SigningBatchFileNotFound = 610`, `SigningBatchFileNotDeletable = 611`, `InvalidTemplateImage = 612`, `PluginSetupMissing = 613`, `InvalidCertificate = 614`, `UntrustedCertificate = 615`, `SigningBatchNotOwned = 616`, `TooManyUploadFiles = 617`, `618`–`635` (lô không ký được, phiên ký, PDF, TSA, chữ ký; bảng đầy đủ ở [09-chu-ky-so.md](09-chu-ky-so.md)). Bỏ qua `401`–`599` vì đụng mã chung `400`/`401`/`404`/`409`/`500` |
| `701`–`799` | Packaging | `PackagingError...`: `PackageTypeInvalid = 701`, `MetadataTypeInvalid = 702`, `SchemaFieldNotFound = 703` ([../nghiep-vu/16-dong-goi-cau-hinh.md](../nghiep-vu/16-dong-goi-cau-hinh.md)) |
| `801`–`899` | Operation | `OperationError...`: nhóm `801`–`803`, thành viên `804`–`807`, `821`, `823`, đơn giá `808`–`810`, `822`, phiếu `811`–`813`, `815`–`818`, `820`, kỳ `819`. Khai đủ 22 mã ở Task 9 cho Task 10–15 khỏi sửa cùng file; `814` bỏ trống (luật QA ≠ người nhập là `331` của Construction), không đánh số lại |

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
