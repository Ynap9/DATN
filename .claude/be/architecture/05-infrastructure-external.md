# Tầng Infrastructure External — sh.be.infrastructure.external

> **Phần 5/9** · trước: [04-infrastructure-data.md](04-infrastructure-data.md) · mục lục: [README.md](README.md)

Dịch vụ bên thứ ba không phải database. Chỉ tham chiếu `sh.be.applications` để implement cổng ra mà application khai báo.
Service và external chỉ nối qua interface: đổi nhà cung cấp không chạm service, sửa service không chạm external.

## Gom theo mục, đặt tên `External`

| Mục | Cổng ra (`applications/Base/Interfaces/External/<Muc>/`) → implement (`external/<Muc>/Implements/`) |
|---|---|
| `Ai` | `ISecretProtectorExternal` → `DataProtectionSecretProtectorExternal` (singleton): `Protect` / `TryUnprotect` theo `purpose` bằng ASP.NET DataProtection; giải mã lỗi (`CryptographicException`, `FormatException` khi chuỗi rác) → `null`. Giữ API key nhà cung cấp AI ([../nghiep-vu/20-ai-nha-cung-cap.md](../nghiep-vu/20-ai-nha-cung-cap.md)) |
| `BackgroundJob` | `IBackgroundJobExternal` → `HangfireBackgroundJobExternal`; cấu hình Hangfire ở `Base/HangfireExtensions` |
| `DataReader` | `IDataReaderExternal` → `GoogleSheetReaderExternal` (qua `Base/DataReaderExternalBase`); `IExcelExternal` → `ExcelExternal`; `IGoogleSheetExternal` → `GoogleSheetExternal` |
| `DigitalSignature` | `IPdfSignerExternal`, `ISigningQueueExternal`, `ITimestampExternal`, `ICertificateValidatorExternal` → `...External` cùng tên (singleton). Xem [09-chu-ky-so.md](09-chu-ky-so.md) |
| `Mail` | `IMailExternal` → `MailExternal`; HTML ở `Mail/Templates/` (embedded resource: `AccountCreated`, `ProjectDeadline`), đọc qua `Base/MailTemplateLoader` |
| `SignalR` | `ISignalRExternal` → `SignalRExternal` (singleton); hub, tracker, cấu hình ở `Base/` |
| `Storage` | `IFileStorageExternal` → `S3FileStorageExternal` (MinIO); `IFileDownloadExternal` → `HttpFileDownloadExternal`; `ISampleFileExternal` → `SampleFileExternal` (PDF nhúng `Storage/Files/*.pdf`); `IPluginSetupExternal` → `PluginSetupExternal` |

- Mỗi mục: `Implements/` (lớp implement cổng ra), `Base/` (lớp nền, helper, cấu hình), `Models/` (model nội bộ, không lộ ra applications).
  DTO ở `applications/Base/Dtos/<Muc>/`. Tên: `I<Ten>External` → `<NhaCungCap><Ten>External`, field trong service `_<ten>External`.
- `IRequestContext`, `IAuthorizationStateProtector` do **api** implement → `Base/Interfaces/Api/`, không hậu tố. `DataReader/` gom đọc / ghi mọi bên thứ ba.

⚠️ Khoá DataProtection mặc định nằm trong hồ sơ user / container: build lại container là sinh khoá mới, mọi API key AI đã lưu thành rác (`907`) và link xác thực Google đang dở hỏng. Production phải đặt `DataProtection:KeysPath` (appsettings, mặc định rỗng = giữ hành vi cũ) **và mount volume Docker** tới đường dẫn đó; có giá trị thì `Program.cs` gọi `PersistKeysToFileSystem` + `SetApplicationName("sh.be")`. Đừng "sửa" bằng cách lưu key chữ thường.

## BackgroundJob — Hangfire

`AddHangfireBackgroundJob(connectionString, BackgroundJobSettings)`: Hangfire 1.8.25 trên `SO_HOA_DB`, schema `hangfire` (11 bảng **tự tạo**, ngoài EF migration).

- Retry `RETRY_ATTEMPTS = 2`, trừ `UserFriendlyException` (`ExceptOn`: lỗi nghiệp vụ Failed ngay). Khi `BackgroundJob:Enabled` dựng **2 server**: queue
  `default` (`WorkerCount`) và queue `export` (`ExportWorkerCount`, tên `{máy}:export`), nên mỗi app có `serverCount = 2`.
- Cổng ra: `AddOrUpdateRecurring` / `TriggerRecurring` / `FindRecurring` (job định kỳ); `Enqueue<TJob>(job, queue, parameters)` (tham số ghi **cùng lúc**
  tạo job, `IBackgroundJobClientV2.Create`); `FindJob(jobId, names)` (không có / hết hạn / id không phải số → `null`; `0` chờ, `1` chạy, `2` xong, `3` Failed /
  Deleted; `ErrorMessage` chỉ khi lỗi là `UserFriendlyException`); `HasServerForQueue` (server nghe queue có heartbeat trong `SERVER_ALIVE_SECONDS = 90`).
- Job là class thường ở `applications/Module/<M>/Jobs/`, `AddScoped` nhóm `// jobs`, người thao tác `SYSTEM_USER`. Định kỳ: thêm cron vào
  `BackgroundJobScheduleSettings` + `api/Worker/RecurringJobRegistrar` (giờ VN); theo yêu cầu: `Enqueue`. Ví dụ [15](../nghiep-vu/15-han-du-an.md), [14](../nghiep-vu/14-ho-so-xoa-xuat.md#xuất-excel-chạy-nền).
- `UseHangfireBackgroundJobDashboard(isReadOnly)` mở `/hangfire` sau `UseAuthorization()`, chỉ đọc khi **không** phải Development;
  `GET api/v1/core/background-job/connection` (`[AllowAnonymous]`) trả `{ isConnected, serverCount }`, lỗi storage → `isConnected = false`.

⚠️ Đổi tên class / method / namespace của job khi DB còn job chờ hoặc lịch đã đăng ký thì Hangfire không tìm thấy kiểu, job lỗi: đợi hàng đợi trống hoặc
xoá job cũ trên dashboard. Tham số job serialize JSON vào DB: chỉ truyền DTO nhỏ và chuỗi, không entity / `byte[]`. `JobParameter` lưu JSON (`"abc"` có ngoặc
kép) nên đọc phải qua `SerializationHelper.Deserialize`. ⚠️ Múi giờ lịch là `BaseService.VietnamTimeZone` (tên Windows); chạy Linux phải đổi.
⚠️ Mọi app trỏ cùng DB đều là server Hangfire: máy đồng đội chạy code cũ vẫn nhận job queue `default` mà không có kiểu job mới → Failed.
⚠️ Namespace `...external.BackgroundJob` trùng lớp tĩnh `Hangfire.BackgroundJob` (CS0234): inject client, đừng gọi lớp tĩnh, đừng đổi tên mục. ⚠️ Dashboard và API kết nối **không cần đăng nhập** theo yêu cầu (`AnonymousDashboardAuthorizationFilter`); muốn khoá thì thay filter, đừng thêm `[Authorize]`.

## SignalR — đẩy tin realtime

`AddSignalRNotification()` (`#region signalr`) đăng ký SignalR + `SignalRConnectionTracker` (singleton, `connectionId → userId`);
`MapSignalRNotificationHub(JwtBearer)` map `NotificationHub` tại `/hubs/notification` (`SignalRConst.HUB_PATH`), bắt buộc token, CORS
`SignalRPolicy`. WebSocket không gửi được header nên JwtBearer `OnMessageReceived` đọc query `access_token` (chỉ đường dẫn hub). Gửi theo
user (`NameIdentifier`, map từ `sub`) bằng `ISignalRExternal.SendToUserAsync(userId, eventName, payload)`, dùng được cả trong job Hangfire.
Hub **không** có method cho client gọi. Test: `GET api/v1/core/signalr/connection` (`[AllowAnonymous]`, `{ isConnected, connectionCount }`);
`POST .../ping` (cần token) đẩy sự kiện `Ping` `{ message, sentDate }` tới mọi kết nối của người gọi.
⚠️ Client SignalR gửi credentials nên `SignalRPolicy` phải `AllowCredentials`; `AllowedHosts = *` mà dùng `WithOrigins("*")` thì app **chết lúc
khởi động**. Giữ `SetIsOriginAllowed` (có `*` → mọi origin, không thì đúng danh sách). ⚠️ Tracker trong RAM: chỉ đúng với **một** instance.

## DataReader

`IDataReaderExternal` dùng **template method**: `DataReaderExternalBase` viết luồng chung (bỏ ô trống, trim header; `GetHeadersAsync` kiểm
`headerRowIndex >= 1` → `400`; `ReadTableAsync` trim mọi ô, **giữ** ô trống để đúng cột, dòng `1` là header, `SheetRowNumber` = số dòng thật),
lớp con chỉ override `ReadHeaderRowsAsync`, `ReadHeaderCellsAsync`, `ReadRowsAsync`. `GoogleSheetReaderExternal` dùng **service account**
(`CredentialsPath`, `sh.be.api/Credentials/` trong `.gitignore`): link sai `202`, `GoogleApiException` `203`, không có sheet `204`; chia sẻ cho `client_email`.
⚠️ Thêm nguồn đọc thứ hai thì DI trả implement đăng ký sau cùng: cần keyed service hoặc factory theo loại nguồn, đừng `AddScoped` thêm rồi để vậy. ⚠️ Dùng `CredentialFactory.FromFile<ServiceAccountCredential | UserCredential>(...)`, **không** `GoogleCredential.FromFile` (CS0618).

`IGoogleSheetExternal` ghi bằng **Gmail** (OAuth), nhận `SpreadsheetAccountDto` từ `ProjectConfig` ([08](../nghiep-vu/08-cau-hinh-du-an.md)):

| Method | Việc |
|---|---|
| `BuildAuthorizationUrl` / `ExchangeRefreshTokenAsync` | Link consent (`prompt=consent`) / đổi `code` lấy refresh token; lỗi → `207` |
| `CreateSpreadsheetAsync(account, title, sheetName)` | Drive `files.create` trong `FolderId`, đổi tên sheet `0` (lỗi thì xoá file); lỗi → `205` |
| `RenameSpreadsheetAsync` / `TryRename...` / `TryDeleteSpreadsheetAsync` | Drive `files.update` / `files.delete`; lỗi → `206`, bản `Try` trả `false` |

`Base/GoogleOAuthFlowFactory` dựng flow + `UserCredential` (scope `drive` + `spreadsheets`), `Base/GoogleSheetUrl` đổi URL ↔ id.
Refresh token hết hạn / bị thu hồi → `207`, phải xác thực lại. ⚠️ Ghi bằng service account thì Google trả `403 storageQuotaExceeded`
(service account không có dung lượng) — đừng quay lại. ⚠️ OAuth consent screen ở **Testing** thì refresh token hết hạn sau 7 ngày.

`IExcelExternal` (ClosedXML): `CreateTemplate(templateName, headers)` = `CreateTable` không có dòng dữ liệu, tên file thêm `_template`;
`CreateMultiSheetTemplate(templateName, sheets)` như `CreateTemplate` nhưng mỗi `ExcelSheetTemplateDto` một sheet. Cả ba dùng chung `CreateWorkbook` + `AddWorksheet` (header dòng `1` in đậm + freeze, giãn cột theo 200 dòng đầu, kẹp rộng 8–60). Luật tên sheet / file Excel (31 ký tự, ký tự cấm) ở `Base/ExcelNaming`. Controller trả `File(...)`; lỗi vẫn trả `ApiResponse`.
⚠️ Hai tên sheet trùng nhau sau khi cắt 31 ký tự thì ClosedXML ném lỗi — chưa xử lý vì 6 tên schema đóng gói hiện tại không trùng.

## Storage, Mail

- `IFileStorageExternal`: `UploadAsync` `PublicRead` trả URL công khai (ảnh mẫu chữ ký), `UploadPrivateAsync` cho file nguồn / bản ký; lỗi
  `209`. `DownloadAsync` lỗi `210`. `GetTemporaryUrl` = link ký sẵn kèm tên gốc. `Delete*` chỉ log khi lỗi. Section `S3`; thiếu URL / bucket →
  `208`; `ForcePathStyle = true`. ⚠️ Bucket phải tạo sẵn, thiếu thì upload `209`.
- `IFileDownloadExternal.OpenPdfAsync`: trả **stream** (không nạp RAM); không phải http(s), lỗi mạng / HTTP, `Content-Type` lạ → `315`.
- `IPluginSetupExternal`: whitelist `PluginSettings.SupportedVersions` (`IOptionsMonitor`, sửa appsettings không cần build) + bộ cài
  `sh.be.api/Plugins/` đúng tên `PluginConst.SETUP_FILE_NAME`. ⚠️ Exe ~160 MB trong `.gitignore`: máy chủ phải chép tay, thiếu thì `613`.
- `IMailExternal`: `SendAsync`, `SendAccountCreatedAsync`, `SendProjectDeadlineAsync`; tiêu đề + HTML ở tầng ngoài; section `Email`.
⚠️ Đổi chỗ embedded resource (`Mail/Templates`, `Storage/Files`) phải sửa cả csproj và `SampleFileExternal.FilesNamespace` — build qua nhưng đọc ra `null`.

## Thêm một dịch vụ ngoài

1. Chọn mục theo **tác dụng** (đọc / ghi dữ liệu → `DataReader`, file → `Storage`…); chỉ mở mục mới khi không khớp mục nào.
2. `I<Ten>External` ở `applications/Base/Interfaces/External/<Muc>/` (DTO ở `Base/Dtos/<Muc>/`); implement ở `external/<Muc>/Implements/`.
3. DI ở nhóm `// third-party services`. External **không** tham chiếu `domain` / `infrastructure.data`; application không tham chiếu external.

⚠️ Gửi mail đang chạy trong transaction của `UserService.Create` ([02-applications.md](02-applications.md)). Dịch vụ ngoài mới đừng gọi trong transaction DB nếu tránh được (`TemplateService.UpdateConfig` đẩy ảnh trước, xoá ảnh cũ sau commit).

> **Tiếp:** [06-shared.md](06-shared.md) — shared kernel dùng chung.
