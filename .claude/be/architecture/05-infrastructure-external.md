# Tầng Infrastructure External — sh.be.infrastructure.external

> **Phần 5/9** · trước: [04-infrastructure-data.md](04-infrastructure-data.md) · mục lục: [README.md](README.md)

Dịch vụ bên thứ ba không phải database. Chỉ tham chiếu `sh.be.applications` để implement cổng ra mà application khai báo.
Service và external chỉ nối qua interface: đổi nhà cung cấp không chạm service, sửa service không chạm external.

## Gom theo mục, đặt tên `External`

| Mục | Cổng ra (`applications/Base/Interfaces/External/<Muc>/`) → implement (`external/<Muc>/Implements/`) |
|---|---|
| `BackgroundJob` | `IBackgroundJobExternal` → `HangfireBackgroundJobExternal`; cấu hình Hangfire ở `Base/HangfireExtensions` |
| `DataReader` | `IDataReaderExternal` → `GoogleSheetReaderExternal` (qua `Base/DataReaderExternalBase`); `IExcelExternal` → `ExcelExternal`; `IGoogleSheetExternal` → `GoogleSheetExternal` |
| `DigitalSignature` | `IPdfSignerExternal`, `ISigningQueueExternal`, `ITimestampExternal`, `ICertificateValidatorExternal` → `...External` cùng tên (singleton). Xem [09-chu-ky-so.md](09-chu-ky-so.md) |
| `Mail` | `IMailExternal` → `MailExternal`; HTML ở `Mail/Templates/` (embedded resource) |
| `SignalR` | `ISignalRExternal` → `SignalRExternal` (singleton); hub, tracker, cấu hình ở `Base/` |
| `Storage` | `IFileStorageExternal` → `S3FileStorageExternal` (MinIO); `IFileDownloadExternal` → `HttpFileDownloadExternal`; `ISampleFileExternal` → `SampleFileExternal` (PDF nhúng `Storage/Files/*.pdf`); `IPluginSetupExternal` → `PluginSetupExternal` |

- Mỗi mục: `Implements/` (lớp implement cổng ra), `Base/` (lớp nền, helper, cấu hình), `Models/` (model nội bộ, không lộ ra applications).
  DTO ở `applications/Base/Dtos/<Muc>/`. Tên: `I<Ten>External` → `<NhaCungCap><Ten>External`, field trong service `_<ten>External`.
- `IRequestContext`, `IAuthorizationStateProtector` do **api** implement → `Base/Interfaces/Api/`, không hậu tố. `DataReader/` gom đọc / ghi
  dữ liệu với **mọi** bên thứ ba (Google Sheet, Excel, PDF…).

⚠️ Namespace `sh.be.infrastructure.external.BackgroundJob` trùng tên lớp tĩnh `Hangfire.BackgroundJob`: ở mọi namespace `sh.be.infrastructure.external.*`,
`BackgroundJob.Enqueue(...)` trỏ vào namespace (CS0234). Inject `IBackgroundJobClient` thay vì gọi lớp tĩnh; đừng đổi tên mục.

## BackgroundJob — Hangfire

`AddHangfireBackgroundJob(connectionString)` (`#region hangfire`, ngay sau `#region db`): storage SQL Server chung `SO_HOA_DB`,
`SchemaName = DbSchemas.Hangfire` (`hangfire`), kèm `AddHangfireServer()`. 11 bảng (`Job`, `State`, `Server`…) do **Hangfire tự tạo** lúc
khởi động, không qua EF migration — `has-pending-model-changes` không thấy. Bản 1.8.25, mặc định đã là cấu hình khuyến nghị.

`UseHangfireBackgroundJobDashboard(isReadOnly)` mở `/hangfire` (`BackgroundJobConst.DASHBOARD_PATH`) sau `UseAuthorization()`;
`AnonymousDashboardAuthorizationFilter` cho mọi request, chỉ đọc khi **không** phải Development. `GET api/v1/core/background-job/connection`
(`[AllowAnonymous]`) trả `{ isConnected, serverCount }` qua `GetMonitoringApi().Servers()`; lỗi storage → `isConnected = false`.

⚠️ Dashboard và API kiểm tra kết nối **không cần đăng nhập** theo yêu cầu: ai vào được máy chủ đều xem được job (ngoài Development chỉ
xem). Muốn khoá thì thay filter, đừng thêm `[Authorize]` vào `/hangfire`. Hub SignalR thì **bắt buộc** token (mục dưới).

## SignalR — đẩy tin realtime

`AddSignalRNotification()` (`#region signalr`) đăng ký SignalR + `SignalRConnectionTracker` (singleton, `connectionId → userId`);
`MapSignalRNotificationHub(JwtBearer)` map `NotificationHub` tại `/hubs/notification` (`SignalRConst.HUB_PATH`), bắt buộc token, CORS
`SignalRPolicy`. WebSocket không gửi được header nên JwtBearer `OnMessageReceived` đọc query `access_token` (chỉ đường dẫn hub). Gửi theo
user (`NameIdentifier`, map từ `sub`) bằng `ISignalRExternal.SendToUserAsync(userId, eventName, payload)`, dùng được cả trong job Hangfire.
Hub **không** có method cho client gọi (`invoke` → `Method does not exist`). Test: `GET api/v1/core/signalr/connection` (`[AllowAnonymous]`,
`{ isConnected, connectionCount }`); `POST .../ping` (cần token) đẩy sự kiện `Ping` `{ message, sentDate }` tới mọi kết nối của người gọi.

⚠️ Client SignalR gửi credentials nên `SignalRPolicy` phải `AllowCredentials`; `AllowedHosts = *` mà dùng `WithOrigins("*")` thì app **chết lúc
khởi động**. Giữ `SetIsOriginAllowed` (có `*` → mọi origin, không thì đúng danh sách). ⚠️ Tracker trong RAM: chỉ đúng với **một** instance.

## DataReader

`IDataReaderExternal` dùng **template method**: `DataReaderExternalBase` viết luồng chung (bỏ ô trống, trim header; `GetHeadersAsync` kiểm
`headerRowIndex >= 1` → `400`; `ReadTableAsync` trim mọi ô, **giữ** ô trống để đúng cột, dòng `1` là header, `SheetRowNumber` = số dòng thật),
lớp con chỉ override `ReadHeaderRowsAsync`, `ReadHeaderCellsAsync`, `ReadRowsAsync`. `GoogleSheetReaderExternal` dùng **service account**
(`CredentialsPath`, `sh.be.api/Credentials/` trong `.gitignore`): link sai `202`, `GoogleApiException` `203`, không có sheet `204`; chia sẻ cho `client_email`.

⚠️ Thêm nguồn đọc thứ hai (ví dụ Excel) thì `IDataReaderExternal` không còn biết lấy implement nào (DI trả cái đăng ký sau cùng) —
cần keyed service hoặc factory theo loại nguồn; đừng đăng ký thêm `AddScoped<IDataReaderExternal, ...>` rồi để vậy.
⚠️ Dùng `CredentialFactory.FromFile<ServiceAccountCredential | UserCredential>(...)`, **không** `GoogleCredential.FromFile` (CS0618).

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
cả hai dùng chung `CreateWorkbook` (header dòng `1` in đậm + freeze, giãn cột theo 200 dòng đầu, kẹp rộng 8–60). Luật tên sheet / file
Excel (31 ký tự, ký tự cấm) ở `Base/ExcelNaming`. Controller trả `File(...)`; lỗi vẫn trả `ApiResponse`.

## Storage, Mail

- `IFileStorageExternal`: `UploadAsync` `PublicRead` trả URL công khai (ảnh mẫu chữ ký), `UploadPrivateAsync` cho file nguồn / bản ký; lỗi
  `209`. `DownloadAsync` lỗi `210`. `GetTemporaryUrl` = link ký sẵn kèm tên gốc. `Delete*` chỉ log khi lỗi. Section `S3`; thiếu URL / bucket →
  `208`; `ForcePathStyle = true`. ⚠️ Bucket phải tạo sẵn, thiếu thì upload `209`.
- `IFileDownloadExternal.OpenPdfAsync`: trả **stream** (không nạp RAM); không phải http(s), lỗi mạng / HTTP, `Content-Type` lạ → `315`.
- `IPluginSetupExternal`: whitelist `PluginSettings.SupportedVersions` (`IOptionsMonitor`, sửa appsettings không cần build) + bộ cài
  `sh.be.api/Plugins/` đúng tên `PluginConst.SETUP_FILE_NAME`. ⚠️ Exe ~160 MB trong `.gitignore`: máy chủ phải chép tay, thiếu thì `613`.
- `IMailExternal`: `SendAsync`, `SendAccountCreatedAsync`; tiêu đề + HTML ở tầng ngoài (sửa email không chạm `UserService`); section `Email`.

⚠️ Đổi chỗ embedded resource (`Mail/Templates`, `Storage/Files`) phải sửa cả csproj và `SampleFileExternal.FilesNamespace` — build qua nhưng đọc ra `null`.

## Thêm một dịch vụ ngoài

1. Chọn mục theo **tác dụng** (đọc / ghi dữ liệu → `DataReader`, file → `Storage`…); chỉ mở mục mới khi không khớp mục nào.
2. `I<Ten>External` ở `applications/Base/Interfaces/External/<Muc>/` (DTO ở `Base/Dtos/<Muc>/`); implement ở `external/<Muc>/Implements/`.
3. DI ở nhóm `// third-party services`. External **không** tham chiếu `domain` / `infrastructure.data`; application không tham chiếu external.

⚠️ Gửi mail đang chạy trong transaction của `UserService.Create` ([02-applications.md](02-applications.md)). Dịch vụ ngoài mới đừng
gọi trong transaction DB nếu tránh được (`TemplateService.UpdateConfig` đẩy ảnh trước, xoá ảnh cũ sau commit).

> **Tiếp:** [06-shared.md](06-shared.md) — shared kernel dùng chung.
