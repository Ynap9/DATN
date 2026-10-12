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
| `DocumentType` | Construction | `Create` (`DefaultView` mặc định `1`), `Update`, `Delete` (xoá mềm); `Name` duy nhất qua `DocumentTypeDomainService.EnsureUniqueNameAsync` |

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
| Phép tính thuần, không I/O | Policy tĩnh | `PermissionGrantPolicy.ResolveGrantedKeys`, `IsSuperAdmin`, `HasAnyPermission` |

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
