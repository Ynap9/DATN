# Quy ước code bắt buộc

> **Phần 7/9** · trước: [06-shared.md](06-shared.md) · mục lục: [README.md](README.md)

Áp dụng cho mọi thay đổi trong `sh.be`. Vi phạm một dòng ở đây coi như chưa xong việc.

## Phạm vi thay đổi

- **Chỉ sửa chỗ cần thiết** cho yêu cầu. Không tiện tay đổi tên, dọn `using`, format lại, refactor ngoài lề.
- **Không thêm comment** khi chưa được yêu cầu — xem mục [Comment](#comment).
- Giữ nguyên chữ ký public của service, DTO, route mà FE đang dùng, trừ khi được duyệt đổi.
- Không commit khi người dùng chưa yêu cầu.

## Đặt đúng tầng

| Muốn viết | Đặt ở |
|---|---|
| Quy tắc chỉ cần dữ liệu một entity | Method trong `domain/.../Entities/` |
| Quy tắc cần tra dữ liệu khác | Domain service trong `domain/.../Services/` |
| Phép tính thuần (cây quyền, so khớp) | Policy tĩnh trong `domain/.../Services/` |
| Lưu / nạp aggregate | Interface `domain/.../Repositories/` + implement `infrastructure.data/.../Repositories/` |
| Màn hình danh sách, chi tiết, dropdown | Interface `applications/.../Interfaces/Queries/` + implement `infrastructure.data/.../Queries/` |
| Luồng use case, transaction, audit | `applications/.../Implements/` |
| Gọi dịch vụ ngoài | Interface ở applications + implement ở `infrastructure.external` |
| Nhận HTTP, kiểm quyền | `api/Controllers`, `[Permission]` |

## Comment

- **Code không được có comment nếu người dùng chưa yêu cầu**: không `//`, không `/* */`, không XML `<summary>`,
  không `TODO`. Ý nghĩa phải đọc được từ tên class, method, biến.
- Comment **đã có sẵn** trong code cũ thì giữ nguyên, không xoá, không sửa khi không liên quan yêu cầu.
- Được yêu cầu viết comment thì viết ngắn, nói **vì sao**, không thuật lại code.

## Đặt tên biến, hàm

- Tên phải **dễ hiểu và sát với nghiệp vụ của yêu cầu**: đọc tên là biết nó làm gì, cho việc gì.
  `EnsureAllExistAsync(roleIds)`, `oldPermissionKeys`, `currentUserId` — không `Check(list)`, `data2`, `temp`, `x`.
- Dùng đúng thuật ngữ đã có trong code và tài liệu (role, permission, audit log…); một khái niệm một tên, không
  lúc `Role` lúc `Group`.
- Method bắt đầu bằng động từ (`Create`, `Assign`, `Ensure`, `Resolve`, `Find`); biến `bool` đọc như câu hỏi
  (`isRandomPassword`, `hasRole`); danh sách dùng số nhiều (`roleIds`, `userRoles`).
- Không viết tắt khó đoán (`usrRl`, `pmsKey`); chỉ giữ viết tắt phổ biến (`id`, `dto`, `ex`).
- **Hạn chế hàm private**: logic chỉ dùng một chỗ viết thẳng trong hàm public. Chỉ tách hàm private khi được gọi
  từ **≥ 2 nơi** (tránh lặp code). Đang áp dụng cho module Submission; code cũ sửa dần khi đụng tới.

## Tên và namespace

- Namespace **khớp đường dẫn thư mục**: `sh.be.domain.Module.Auth.Entities`,
  `sh.be.applications.Module.Auth.Interfaces.Queries`…
- Interface tiền tố `I`; implement cùng tên bỏ `I` (`IRoleQuery` → `RoleQuery`).
- Repository: `I<Aggregate>Repository`. Query: `I<Aggregate>Query`. Domain service: `<Aggregate>DomainService`.
- Factory entity: `static Create(...)`. Đổi trạng thái: động từ nghiệp vụ (`Delete`, `AssignRoles`).
- Field private dạng `_camelCase`. Method async có hậu tố `Async` ở repository / domain service.
- ⚠️ `BaseService.getCurrentUserId()` viết thường chữ đầu (quy ước cũ). Giữ nguyên, đừng đổi tên lan khắp nơi.

## Entity

- Setter `private set`, constructor `private`, tạo bằng `Create`.
- Không inject gì vào entity; thời điểm và người thao tác truyền qua tham số.
- Không thêm navigation property hay đổi attribute EF khi chưa được duyệt đổi schema.

## Service

- Kế thừa `BaseService`; dependency qua constructor; đăng ký DI trong `Program.cs`.
- Log đầu method: `_logger.LogInformation($"{nameof(Method)}, ...")`.
- Không có `if` nghiệp vụ; lỗi "không tìm thấy" dùng `?? throw new UserFriendlyException(ErrorCodes.X)`.
- Ghi nhiều bước ⇒ `BeginTransactionAsync` + `try / CommitAsync / catch RollbackAsync; throw`.
- Thời gian: `GetVietnamTime()`; người thao tác: `getCurrentUserId()`.

## Truy vấn

- Không viết `!x.Deleted` (đã có global query filter). Cần bản ghi đã xoá ⇒ `IgnoreQueryFilters()`.
- Query đọc dùng `AsNoTracking()` rồi chiếu thẳng ra DTO trong SQL: `ProjectTo<TDto>(_mapper.ConfigurationProvider)`
  với map khai trong `MappingProfile` (module mới, từ `DocumentType`), hoặc `Select` tay (module cũ). Đặt sau `.Paging(dto)`.
  Query mới kế thừa `BaseQuery` để có `_shDbContext`, `_mapper`; service đã có `_mapper` từ `BaseService`.
- AutoMapper **chỉ** dùng chiều entity → DTO. DTO → entity luôn qua `Create`/`Update` của entity (setter `private`).
- Không trả `IQueryable` ra khỏi infrastructure.

## Lỗi và response

- Lỗi nghiệp vụ: `UserFriendlyException(ErrorCodes.X)` hoặc kèm message tiếng Việt.
- Controller: `try { return new(data); } catch (Exception ex) { return OkException(ex); }`.
- Không trả HTTP code khác 200 cho lỗi nghiệp vụ (trừ `401`/`403` từ auth).

## Kiểm trước khi báo xong

```bash
dotnet build sh.be/sh.be.api/sh.be.api.sln
dotnet ef migrations has-pending-model-changes --project sh.be/sh.be.infrastructure.data --startup-project sh.be/sh.be.api
```

Build 0 lỗi, không thêm warning mới; lệnh EF trả "No changes" nếu không chủ đích đổi schema.

> **Tiếp:** [08-them-chuc-nang.md](08-them-chuc-nang.md) — các bước thêm use case, module mới.
