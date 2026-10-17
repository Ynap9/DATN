# Tầng Domain — sh.be.domain

> **Phần 3/8** · trước: [02-applications.md](02-applications.md) · mục lục: [README.md](README.md)

Lõi nghiệp vụ. Chỉ tham chiếu `sh.be.shared`. Mọi quy tắc nghiệp vụ nằm ở đây.

## Thư mục

```text
Base/IUnitOfWork.cs             IUnitOfWork, IUnitOfWorkTransaction
Module/<TenModule>/
  Entities/                     dữ liệu + hành vi của chính entity
  Services/                     domain service (cần repository) và policy (hàm thuần)
  Repositories/                 interface repository — implement ở infrastructure.data
```

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
| `Project` | Construction | `Create` (`Status = DEPLOYING`, `GoogleSheetName = Name`, kiểm `StartedAt ≤ EndedAt`), `Update` (kiểm thêm `Status` thuộc `ProjectConst.STATUSES`), `LinkGoogleSheet`, `EnsureImportable` (`DONE` → `313`), `Delete` (xoá mềm, `ISoftDeleted`), `AssignDocumentTypes` (dựng `ProjectDocumentType`, gọi sau khi đã có `Id`); `Name` duy nhất qua `ProjectDomainService.EnsureUniqueNameAsync` |
| `ProjectConfig` | Construction | Key-value: `Create`, `Update` (giữ secret cũ khi gửi `******`), `ChangeValue`, `Delete`, `IsSecret`; `ProjectConfigDomainService`: `Name` duy nhất, `GetRequiredValuesAsync` (thiếu → `304`), `SaveValueAsync` (có thì sửa, chưa có thì tạo) |
| `ProjectDocumentType` | Construction | Bảng nối dự án – loại văn bản, khoá ghép `(ProjectId, DocumentTypeId)`: `Create`, `Delete` (xoá mềm), `AssignDocumentFields` (chép trường của loại, `OrderFieldImport` từ 1) |
| `ProjectDocumentTypeDocumentField` | Construction | Trường văn bản theo dự án, khoá ghép `(ProjectId, DocumentTypeId, DocumentFieldId)`, thêm `FieldImport`, `OrderFieldImport`: `Create`, `Delete` (xoá mềm) |
| `Submission` | Construction | Hồ sơ = một dòng Google Sheet; gốc của `SubmissionData`. `Create` (trim, `Name = FileName`, một link gán cho cả `FileUrlS3Object` / `FileUrlGoogleDrive` / `FileUrlGstore`, `StatusCheck = SubmissionConst.NOT_CHECKED`), `CreateData` (sinh `SubmissionData` lấy `ProjectId`, `DocumentTypeId` từ hồ sơ), `IsChecked`, `Delete` (xoá mềm) |
| `SubmissionData` | Construction | Giá trị một trường của hồ sơ, không có navigation: `Create` (public cho bulk insert; chỉ gọi qua `Submission.CreateData` hoặc policy import), `AssignSubmission` (gán `Id` thật sau bulk insert), `FillIfBlank` / `Overwrite` (trả `false` nếu không đổi gì), `Delete` (xoá mềm) |
| `DocumentField` | Construction | `Create` (`FieldType = "string"`, hai cờ hiển thị `true`), `Update` (không đổi `DocumentTypeId`, không đổi thứ tự), `UpdateSetting` (chỉ `IsShow`, `IsChargeable`, `IsFieldOcr`, `FieldNameOcr`; tắt OCR thì giữ `FieldNameOcr` cũ), `ChangeOrderTableView` / `ChangeOrderListView` (mỗi method một cột), `Delete` (xoá mềm, giữ nguyên thứ tự); `DocumentFieldDomainService` kiểm loại văn bản tồn tại, `Field` duy nhất, thứ tự không trùng (tạo = lớn nhất của cả hai cột kể cả trường đã xoá + 1, kéo thả từng cột thì dịch `±1` các trường ở giữa, xoá không đổi thứ tự) |

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
| Phép tính thuần, không I/O | Policy tĩnh | `PermissionGrantPolicy.ResolveGrantedKeys`, `IsSuperAdmin`, `HasAnyPermission`; `FieldImportMatchPolicy.IsMatch` / `ToMatchKey` |

`Services/Import/` (Construction) là policy **thuần**, chạy song song an toàn: `SubmissionKeyPolicy`, `SheetCellAddressPolicy`,
`SubmissionImportColumnPolicy`, `SubmissionImportPolicy` (dựng `Append/Blank/OverwriteImportPlan` dùng chung cho verify và import) +
record trong `Import/Models/`. Bảng đọc từ nguồn (`SheetTable`, `SheetRow`) ở `domain/Base/Models` vì `IDataReader` dùng chung mọi module.

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
