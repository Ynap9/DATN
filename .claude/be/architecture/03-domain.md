# Tầng Domain — sh.be.domain

> **Phần 3/9** · trước: [02-applications.md](02-applications.md) · mục lục: [README.md](README.md)

Lõi nghiệp vụ. Chỉ tham chiếu `sh.be.shared`. Mọi quy tắc nghiệp vụ nằm ở đây.

## Thư mục

```text
Base/IUnitOfWork.cs             IUnitOfWork, IUnitOfWorkTransaction
Module/<TenModule>/
  Entities/                     dữ liệu + hành vi của chính entity
  Services/                     domain service (cần repository) và policy (hàm thuần)
  Repositories/                 interface repository — implement ở infrastructure.data
Module/Construction/<NghiepVu>/ module lớn: gom theo nghiệp vụ trước, vai trò sau
  DocumentTypes/ DocumentFields/ Projects/ Submissions/   mỗi thư mục có Entities/ Repositories/ Services/
```

⚠️ Thư mục nghiệp vụ đặt tên **số nhiều** (`Submissions/`, không `Submission/`): namespace `...Construction.Submission`
sẽ che entity `Submission` trong code domain (CS0118).

## Entity giàu hành vi

- Setter `private set`, constructor `private`; tạo bằng factory `static Create(...)`.
- Thay đổi trạng thái chỉ qua method: `Update`, `Delete`, `AssignPermissions`, `AssignRoles`, `Sync`…
- Thời điểm và người thao tác truyền **vào** method (`modifiedBy`, `modifiedDate`); entity không tự lấy giờ.

| Entity | Module | Hành vi chính |
|---|---|---|
| `Role` | Auth | `Create` (luôn `IsSystem = 1`), `Update`, `Delete` (xoá mềm), `AssignPermissions` |
| `Permission` | Auth | `Create`, `Sync` (trả `false` nếu không đổi, khôi phục nếu đã xoá) |
| `RolePermission` | Auth | `Create` |
| `UserRole` | Auth | `Create`, `Delete` (xoá mềm) |
| `AppUser : IdentityUser` | Auth | `Create` (`EmailConfirmed = true`), `UpdateProfile`, `AssignRoles` |
| `AuditLog` | Core | `Create` (tự serialize `oldData`/`newData` sang JSON) |
| `DocumentType` | Construction | `Create` (`DefaultView` mặc định `1`), `Update`, `Delete` (xoá mềm); `Name` duy nhất qua `DocumentTypeDomainService.EnsureUniqueNameAsync`; `EnsureAllExistAsync` (thiếu id nào → `301`) |
| `Project` | Construction | `Create` (`Status = DEPLOYING`, `GoogleSheetName = Name`, kiểm `StartedAt ≤ EndedAt`), `Update` (kiểm thêm `Status` thuộc `ProjectConst.STATUSES`), `LinkGoogleSheet`, `Expire(today, …)` (đang hoạt động và `EndedAt` < hôm nay → `EXPIRED`, trả `bool`), `EnsureImportable` (`DONE` → `313`, `EXPIRED` → `323`), `EnsureEditable(isSuperAdmin)` (không phải SuperAdmin: `DONE` → `316`, `EXPIRED` → `323`), `Delete` (xoá mềm, `ISoftDeleted`), `AssignDocumentTypes` (dựng `ProjectDocumentType`, gọi sau khi đã có `Id`); `Name` duy nhất qua `ProjectDomainService.EnsureUniqueNameAsync` |
| `ProjectConfig` | Construction | Key-value: `Create`, `Update` (giữ secret cũ khi gửi `******`), `ChangeValue`, `Delete`, `IsSecret`; `ProjectConfigDomainService`: `Name` duy nhất, `GetRequiredValuesAsync` (thiếu → `304`), `SaveValueAsync` (có thì sửa, chưa có thì tạo), `GetDeadlineSettingsAsync` (2 key hạn dự án, thiếu thì mặc định) |
| `ProjectDeadlineNotification` | Construction | Nhật ký mail báo hạn, không xoá mềm, migration `Add_ProjectDeadlineNotification`: `Create` (`IsSuccess = true`), `MarkFailed` (lỗi cắt 2000 ký tự) |
| `ProjectDocumentType` | Construction | Bảng nối dự án – loại văn bản, khoá ghép `(ProjectId, DocumentTypeId)`: `Create`, `Delete` (xoá mềm), `AssignDocumentFields` (chép trường của loại, `OrderFieldImport` từ 1) |
| `ProjectDocumentTypeDocumentField` | Construction | Trường văn bản theo dự án, khoá ghép `(ProjectId, DocumentTypeId, DocumentFieldId)`, thêm `FieldImport`, `OrderFieldImport`: `Create`, `Delete` (xoá mềm) |
| `Submission` | Construction | Hồ sơ = một dòng Google Sheet; gốc của `SubmissionData`, `SubmissionLog`, `SubmissionQaLog`; cột QA `StatusQa`, `QaReason`, `QaBy`, `QaDate` (migration `Add_SubmissionQa`). `Create` (trim, `Name = FileName`, một link gán cho cả `FileUrlS3Object` / `FileUrlGoogleDrive` / `FileUrlGstore`, `StatusCheck = SubmissionConst.NOT_CHECKED`), `CreateData` (sinh `SubmissionData` lấy `ProjectId`, `DocumentTypeId` từ hồ sơ), `IsChecked`, `Check` (luôn sang `CHECKED`, trả `SubmissionLog` "kiểm tra" / "tái kiểm tra" / "kiểm tra lại hồ sơ bị trả lại" — hồ sơ `RETURNED` thì `StatusQa` về `0`, ghi chú ≤ 1000 ký tự), `ReviewQa` (chỉ hồ sơ `CHECKED` → `320`; Đạt → `QA_PASSED`; Không đạt bắt buộc lý do → `321` và ≥ 1 trường sai thuộc cấu hình → `324` / `318` / `311`, sang `QA_FAILED` + `RETURNED`; trả `SubmissionQaLog`), `EnsureBelongsTo` (`319`), `Delete` (xoá mềm) |
| `SubmissionData` | Construction | Giá trị một trường của hồ sơ, không có navigation: `Create` (public cho bulk insert; chỉ gọi qua `Submission.CreateData` hoặc policy import), `AssignSubmission` (gán `Id` thật sau bulk insert), `FillIfBlank` / `Overwrite` (trả `false` nếu không đổi gì), `ChangeData` (người kiểm tra sửa tay, cho phép xoá trắng, không đổi `IndexGoogleSheet`), `Delete` (xoá mềm) |
| `SubmissionLog` | Construction | Một lần kiểm tra hồ sơ, migration `Add_SubmissionLog`: `Create` (chỉ gọi từ `Submission.Check`), lưu `UserId`, trạng thái cũ / mới, mô tả, ghi chú, số ô đã sửa |
| `SubmissionQaLog` | Construction | Một lần chấm QA, migration `Add_SubmissionQa`: `Create` (chỉ gọi từ `Submission.ReviewQa`), lưu `UserId`, `StatusQa` và `StatusCheck` cũ / mới, lý do, mô tả; `CreateWrongFields` (gọi sau khi đã có `Id`, chỉ sinh khi Không đạt) dựng `SubmissionQaLogField` — trường sai, khoá ghép `(SubmissionQaLogId, DocumentFieldId)`, migration `Add_SubmissionQaLogField` |
| `DocumentField` | Construction | `Create` (`FieldType = "string"`, hai cờ hiển thị `true`), `Update` (không đổi `DocumentTypeId`, không đổi thứ tự), `UpdateSetting` (chỉ `IsShow`, `IsChargeable`, `IsFieldOcr`, `FieldNameOcr`; tắt OCR thì giữ `FieldNameOcr` cũ), `ChangeOrderTableView` / `ChangeOrderListView` (mỗi method một cột), `Delete` (xoá mềm, giữ nguyên thứ tự); `DocumentFieldDomainService` kiểm loại văn bản tồn tại, `Field` duy nhất, thứ tự không trùng (tạo = lớn nhất của cả hai cột kể cả trường đã xoá + 1, kéo thả từng cột thì dịch `±1` các trường ở giữa, xoá không đổi thứ tự) |

| `Template` | DigitalSignature | Mẫu cấu hình chữ ký (port từ `kssm.be`), migration `Add_DigitalSignature`: `Create`, `Update` (chỉ `Name`, trim), `UpdateConfig` (kẹp độ đậm `40–250`, độ dày nét `0–200`, cỡ chữ `6–24`; màu chuẩn hoá `#RRGGBB`, sai → mặc định / `null`; font ngoài `TemplateConst.DIGITAL_SIGNATURE_FONTS` → `Times New Roman`; `AllowSigningSignedPdf` = cho ký lên PDF đã có chữ ký), `ChangeStampImage` / `RemoveStampImage`, `ChangeWetSignatureImage` / `RemoveWetSignatureImage`, `Delete` (xoá mềm + bỏ link ảnh), `CreatePosition` |
| `TemplatePosition` | DigitalSignature | Khối đặt trên trang PDF, toạ độ theo tỉ lệ `0–1`: `Create` (`Kind` thuộc `TemplatePositionConst.KINDS`, `PageNumber ≥ 1`, khối nằm trọn trong trang, kích thước `> 0`, sai → `602` / `603`), `Delete` (xoá mềm). Chỉ tạo qua `Template.CreatePosition` |
| `SigningBatch` | DigitalSignature | Lô ký (port từ `LoKy` của `kssm.be`), migration `Add_DigitalSignature`, trạng thái `SigningBatchConst`: `Create` (`NEW`), `NameSignedFolder` (bỏ `/`, `\`, ký tự điều khiển, trim; rỗng → `605`), `EnsureOwnedBy` (khác `CreatedBy` → `616`), `LinkProject`, `EnsureAcceptUpload` (lô dự án → `606`), `CreateFile(order, …)` (chỉ khi `NEW`, không thì `607`; `Order` do service truyền = lớn nhất của lô kể cả file đã xoá + 1), `RemoveFile` (lô `SIGNING` → `608`, trừ bộ đếm), `ChangeCertificate` (ngoài `NEW`/`PAUSED`/`FAILED` → `618`), `RequeueFailedFiles`, `Start(sessionThumbprint, pendingFiles)` (`618`/`609`/`619`/`620`/`621`), `Pause` (chỉ `SIGNING` → `622`), `Cancel` (đã `DONE`/`CANCELLED` → `623`), `Complete` (còn file lỗi → `Fail`, không thì `Finish`), `Fail` (chỉ `SIGNING`), `MarkProjectNotified`, `Delete` |
| `SigningBatchFile` | DigitalSignature | Một file trong lô, trạng thái `SigningBatchFileConst`: `Create` (`PENDING`), `StartSigning`, `MarkSigned` (ghi `SignedObjectKey` — bản đã ký lưu trên MinIO, không lên Google Drive như kssm), `MarkFailed`, `ReturnToQueue`, `Retry` (xoá lỗi và kết quả kiểm chữ ký), `GetSignedObjectKey` (chưa `DONE` → `624`), `Delete` (chỉ `PENDING`/`FAILED`, không thì `611`). Chỉ tạo qua `SigningBatch.CreateFile` |

**Vì sao hành vi nằm trong entity:** setter `private` chỉ có nghĩa khi entity tự cung cấp cách thay đổi. Tách
các method này ra class khác thì setter phải mở `public` — entity thành anemic model, ai cũng gán được
`role.Deleted = true` mà quên `DeletedBy`.

## Aggregate

- `Role` là gốc của `RolePermission`; `AppUser` là gốc của `UserRole`.
- Aggregate **không** giữ collection con (không có navigation property, để khỏi đổi schema). Method gốc **sinh ra**
  danh sách con hợp lệ, repository lưu danh sách đó (`ReplacePermissionsAsync`, `AddUserRoles`).

⚠️ `RolePermission.Create` / `UserRole.Create` là `public`, nên vẫn tạo được con mà không qua gốc. Quy ước: chỉ
gọi chúng bên trong method của aggregate gốc hoặc seeder; đừng gọi từ service.

## Entity hay domain service?

| Quy tắc cần | Đặt ở | Ví dụ |
|---|---|---|
| Chỉ dữ liệu của chính entity | Method của entity | `Role.Update`, `AppUser.UpdateProfile` |
| Tra cứu dữ liệu khác (qua repository) | Domain service | `UserDomainService.EnsureUniqueAsync`, `RoleDomainService.EnsureAllExistAsync` |
| Phép tính thuần, không I/O | Policy tĩnh | `PermissionGrantPolicy.ResolveGrantedKeys`, `IsSuperAdmin`, `HasAnyPermission`; `FieldImportMatchPolicy.IsMatch` / `ToMatchKey`; `TemplateImagePolicy` (kiểm ảnh mẫu chữ ký → `612`, dựng object key); `PluginVersionPolicy` (whitelist phiên bản plugin); `SigningBatchFilePolicy` (trần 200 file / lượt → `617`, nhận `.pdf` không rỗng, ký đè → `628`, dựng key nguồn / bản ký, đổi exception thành lỗi lưu cho file); `ProjectDeadlinePolicy` (`daysLeft`, phân loại sắp / đã hết hạn, đọc cấu hình, khoá chống gửi trùng) |

`Construction/Submissions/Services/Import/` là policy **thuần**, chạy song song an toàn: `SubmissionKeyPolicy`, `SheetCellAddressPolicy`,
`SubmissionImportColumnPolicy`, `SubmissionImportPolicy` (dựng `Append/Blank/OverwriteImportPlan` dùng chung cho verify và import) +
record trong `Import/Models/`. `Submissions/Services/Check/SubmissionCheckPolicy` cũng thuần: dựng `SubmissionCheckPlan` (ô sửa, ô thêm, thay đổi cho audit) cho API kiểm tra hồ sơ, lỗi `318` / `311`. Bảng đọc từ nguồn (`SheetTable`, `SheetRow`) ở `domain/Base/Models` vì `IDataReaderExternal` dùng chung mọi module.

Entity **không** được giữ repository. Domain service nhận repository qua constructor và được đăng ký DI dạng
class cụ thể.

## Repository interface

- Một repository cho mỗi aggregate; tên method mang nghĩa nghiệp vụ (`GetRoleNamesByUserIdAsync`,
  `CountActiveAsync`), **không** dùng generic repository, **không** lộ `IQueryable`.
- Interface ở domain vì domain service cần tra dữ liệu mà không được biết EF. Implementation ở
  [04-infrastructure-data.md](04-infrastructure-data.md).
- Lưu thay đổi qua `IUnitOfWork.SaveChangesAsync()`; repository không tự `SaveChanges` (ngoại lệ:
  `UserRepository` đi qua `UserManager`, vốn tự lưu).

## Lỗi nghiệp vụ

Domain ném `UserFriendlyException(ErrorCodes.X)` từ shared. ⚠️ Mã lỗi này cũng là mã API trả về, nên domain
đang biết mã của tầng ngoài — thoả hiệp đã biết; muốn tách thì thêm `DomainException` và map ở api.

> **Tiếp:** [04-infrastructure-data.md](04-infrastructure-data.md) — implement repository, query bằng EF Core.
