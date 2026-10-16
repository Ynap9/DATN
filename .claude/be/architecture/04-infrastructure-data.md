# Tầng Infrastructure Data — sh.be.infrastructure.data

> **Phần 4/8** · trước: [03-domain.md](03-domain.md) · mục lục: [README.md](README.md)

Mọi thứ dính EF Core và SQL Server. Tham chiếu `applications` (để implement query) và `domain` (để implement
repository, `IUnitOfWork`).

## Thư mục

| Thư mục | Chứa gì |
|---|---|
| `Base/UnitOfWork.cs` | `UnitOfWork` + `UnitOfWorkTransaction` bọc `IDbContextTransaction` |
| `Base/BaseQuery.cs` | Lớp cha của query: `_shDbContext`, `_mapper`. Query mới kế thừa, không tự inject |
| `Module/<TenModule>/Repositories/` | Implement `I...Repository` của domain |
| `Module/<TenModule>/Queries/` | Implement `I...Query` của applications, trả DTO |
| `Persistence/Db/ShDbContext.cs` | `IdentityUserContext<AppUser>` + `DbSet` + cấu hình model + OpenIddict |
| `Persistence/Seeder/` | `SeedRole`, `SeedPermission`, `SeedUser`, `SeedRolePermission` |
| `Migrations/Auth/`, `Migrations/Core/`, `Migrations/Construction/` | Migration tách theo module; `ShDbContextModelSnapshot.cs` chung |

## Repository vs Query

| | Repository | Query |
|---|---|---|
| Trả về | Entity domain (có tracking) | DTO (`AsNoTracking`, `ProjectTo` hoặc `Select` thẳng) |
| Được join nhiều bảng | Chỉ khi phục vụ aggregate | Thoải mái, kể cả bảng `Users` |
| Lưu | Không tự `SaveChanges`; để `IUnitOfWork` | Không ghi gì |

`UserRepository` bọc `UserManager<AppUser>` và dịch `IdentityResult` lỗi sang `UserFriendlyException` với mã
`102` (tạo), `106` (sửa), `107` (xoá), `108` (đổi mật khẩu). ⚠️ `UserManager` **tự `SaveChanges`**, nên audit
log thêm trước lời gọi `UpdateAsync`/`ChangePasswordAsync` được lưu cùng lúc — đừng gọi thêm `SaveChanges`
trong các luồng đó.

## Schema

- Bảng Auth ở schema `auth`, Core ở `core`, Construction ở `construction` (`[Table(..., Schema = DbSchemas.X)]` trên entity).
- Khoá ghép: `RolePermission (RoleId, PermissionKey)`, `UserRole (UserId, RoleId)`, `ProjectDocumentType (ProjectId, DocumentTypeId)`.
- Index khai bằng `[Index(..., Name = "IX_<Bang>_<Cot>")]` trên entity (không `HasIndex`): `AuditLog` (`CreatedDate`, `UserId`, `ActionType`),
  `UserRole.RoleId`, `DocumentType.Name`, `DocumentField (DocumentTypeId, Field)` và `(DocumentTypeId, OrderTableView)`, `Project.Name`, `ProjectConfig.Name`,
  `ProjectDocumentType.DocumentTypeId`.
  Cột chuỗi có index thành `nvarchar(450)` — giá trị dài hơn sẽ lỗi khi lưu.
- `Deleted` mặc định `0`, `CreatedDate` mặc định `getdate()` (cấu hình trong `OnModelCreating`).
- EF map được setter/constructor `private` của entity, nên entity giàu hành vi không đổi schema.

⚠️ Bẫy: cột `int` có `HasDefaultValue(x)` thì EF bỏ qua giá trị bằng `0` (giá trị mặc định CLR) khi insert, nên DB tự
điền `x` → gán `0` thành `x`. `DocumentType.DefaultView` vì vậy dùng `HasDefaultValue(1).HasSentinel(-1)`. Cột `bool` default `true` cũng vậy:
`DocumentField.IsShowTableView` / `IsShowListView` dùng `HasDefaultValue(true).HasSentinel(true)`. Đừng bỏ `HasSentinel`.

`DocumentTypeRepository.LockAsync` khoá dòng loại văn bản (`UPDLOCK, HOLDLOCK`) để tuần tự hoá việc ghi thứ tự trường;
chỉ có tác dụng **bên trong transaction** (xem [../nghiep-vu/06-truong-van-ban.md](../nghiep-vu/06-truong-van-ban.md)).

## Xoá mềm — global query filter

`Role`, `Permission`, `UserRole`, `RolePermission`, `DocumentType`, `DocumentField`, `ProjectConfig`, `ProjectDocumentType` có `HasQueryFilter(e => !e.Deleted)` trong `ShDbContext`;
`Project` dùng cờ tên khác: `HasQueryFilter(e => !e.IsDeleted)`.
Mọi truy vấn LINQ (kể cả `join`, subquery, `Any`, `Count`) tự loại bản ghi đã xoá mềm.

- **Không** viết tay `!x.Deleted` nữa.
- Cần thấy cả bản ghi đã xoá ⇒ `.IgnoreQueryFilters()`. Hiện dùng ở: `ReplacePermissionsAsync`,
  `DeleteUserRolesAsync` (xoá cứng theo khoá ghép), `DocumentFieldRepository.GetMaxOrderAsync` (thứ tự mới tính cả trường
đã xoá), `SeedPermission` (khôi phục quyền), `SeedRolePermission`,
  `SeedUser` (kiểm cặp khoá đã có).

⚠️ Bẫy: `ExecuteDeleteAsync` cũng bị filter lọc. Xoá cứng mà quên `IgnoreQueryFilters()` thì bản ghi xoá mềm
còn nằm lại và lần `Add` sau trùng khoá ghép. Đừng "sửa" bằng cách bỏ filter khỏi entity.

## Migration

Đổi entity hoặc `OnModelCreating` là **đổi schema** — phải hỏi trước (skill `task-workflow`). Khi đã được duyệt:

```bash
dotnet ef migrations add <Ten> --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api --output-dir Migrations/<Auth|Core|Construction>
dotnet ef migrations has-pending-model-changes --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api
```

Refactor không định đổi schema thì lệnh thứ hai **phải** trả "No changes have been made to the model…".
Lệnh `database update` và `migrations remove`: xem [../db/README.md](../db/README.md).

## Seeder

Chạy mỗi lần khởi động (từ `Program.cs`), idempotent: chỉ thêm cái còn thiếu và ghi `AuditLog` khi có thay đổi.

| Seeder | Làm gì |
|---|---|
| `SeedRole` | Thêm các role mặc định trong `RoleConstants` còn thiếu |
| `SeedPermission` | Đồng bộ cây quyền với danh sách cứng: thêm mới, `Sync` cái đã có, xoá cứng cái thừa |
| `SeedUser` | Tạo role `SuperAdmin`, tài khoản `admin`, gán role |
| `SeedRolePermission` | Gán đủ mọi quyền cho `SuperAdmin`, `Admin` |

Thêm quyền mới = thêm hằng trong `PermissionConstants` + một dòng `Permission.Create(...)` trong
`SeedPermission`. Seeder là class tĩnh nên dùng `DateTimeUtils.GetVietnamTime()` thay vì `TimeProvider`.

> **Tiếp:** [05-infrastructure-external.md](05-infrastructure-external.md) — dịch vụ bên ngoài.
