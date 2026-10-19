# Tầng Application — sh.be.applications

> **Phần 2/9** · trước: [01-api.md](01-api.md) · mục lục: [README.md](README.md)

Điều phối use case. Chỉ tham chiếu `sh.be.domain` (qua đó dùng được `sh.be.shared`). **Không** biết EF Core,
`ShDbContext`, `UserManager`, `HttpContext`, OpenIddict.

## Thư mục

```text
Base/
  BaseService.cs          lớp cha của mọi service: audit log, user hiện tại, giờ Việt Nam
  MappingProfile.cs       profile AutoMapper: map entity → DTO đọc, dùng qua ProjectTo
  Interfaces/             cổng ra dùng chung: IRequestContext, IMailService
Module/<TenModule>/
  Dtos/<Nhom>/            DTO vào/ra của API
  Interfaces/Services/    use case (input port) — controller gọi vào
  Interfaces/Queries/     cổng đọc (output port) — infrastructure.data implement
  Implements/             service implement Interfaces/Services
```

## Services vs Queries

| | `Interfaces/Services/` | `Interfaces/Queries/` |
|---|---|---|
| Ai gọi | Controller | Service |
| Ai implement | `Implements/` (chính tầng này) | `infrastructure.data/.../Queries/` |
| Trả về | DTO | DTO, chiếu thẳng từ SQL |
| Dùng cho | Mọi use case | Phân trang, dropdown, xem chi tiết |

Interface query đặt ở đây (không ở domain) vì nó trả **DTO**, mà DTO thuộc application.

## Service chỉ điều phối

Mẫu một lệnh ghi:

```csharp
var role = await _roleRepository.FindByIdAsync(dto.RoleId)
    ?? throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);   // nạp
var rolePermissions = role.AssignPermissions(permissions, checkedKeys, userId, now); // domain quyết định

await using var transaction = await _unitOfWork.BeginTransactionAsync();
try
{
    await _roleRepository.ReplacePermissionsAsync(dto.RoleId, rolePermissions);    // lưu
    AddAuditLog(...);
    await _unitOfWork.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, ...);
    await transaction.RollbackAsync();
    throw;
}
```

- **Được:** nạp aggregate, gọi method entity / domain service, mở transaction, ghi audit, log, gọi `IMailService`.
- **Không được:** `if` nghiệp vụ, gán thẳng thuộc tính entity, tự viết luật quyền, truy vấn LINQ trên DB.
- Lệnh ghi nhiều bước (xoá + thêm, tạo user + gán role) **phải** nằm trong transaction như trên.

⚠️ `UserService.Create` gửi mail **trước** `CommitAsync`: SMTP chậm thì transaction bị giữ lâu, và nếu commit
lỗi sau khi mail đã đi thì người dùng nhận mail cho tài khoản không tồn tại. Đã biết, chưa sửa — hướng sửa là
domain event / outbox, đừng chỉ đảo thứ tự mà bỏ rollback.

## BaseService

| Thành viên | Dùng để |
|---|---|
| `_unitOfWork`, `_auditLogRepository`, `_mapper`, `_logger`, `_requestContext`, `_timeProvider` | Dependency chung |
| `getCurrentUserId()` | Id user từ token (`IRequestContext.UserId`) |
| `GetVietnamTime()` | Giờ `SE Asia Standard Time` tính từ `TimeProvider` |
| `AddAuditLog(action, actionType, content, oldData?, newData?, userId?)` | Thêm `AuditLog`; lưu khi `SaveChanges` |

Service mới kế thừa `BaseService` và truyền đủ 6 tham số lên `base(...)`.

## DTO

- Tên theo hành động: `CreateXxxDto`, `UpdateXxxDto`, `FindPagingXxxDto`, `FindXxxByIdDto`, `ViewXxxDto`,
  `GetListXxxDto`.
- DTO phân trang kế thừa `BaseRequestPagingDto`; kết quả trả `BaseResponsePagingDto<T>`.
- Không trả entity ra API; luôn map sang DTO (trong query hoặc `Select`).

> **Tiếp:** [03-domain.md](03-domain.md) — nơi nghiệp vụ thật sự nằm.
