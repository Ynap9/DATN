# Thêm use case hoặc module mới

> **Phần 8/8** · trước: [07-conventions.md](07-conventions.md) · mục lục: [README.md](README.md)

Làm theo thứ tự **từ trong ra ngoài**: domain → applications → infrastructure → api. Tầng trong xong trước thì
tầng ngoài chỉ việc implement.

## Thêm một use case GHI vào module có sẵn

1. **Domain** — nghiệp vụ:
   - quy tắc của một entity ⇒ thêm method vào entity trong `Entities/`;
   - cần tra dữ liệu ⇒ thêm method vào domain service trong `Services/` (hoặc tạo service mới);
   - cần nạp / lưu kiểu mới ⇒ thêm method vào `Repositories/I<Aggregate>Repository`.
2. **Infrastructure.data** — implement method repository mới trong `Module/<M>/Repositories/`.
3. **Applications** — DTO trong `Dtos/<Nhom>/`, method trong `Interfaces/Services/I<X>Service`, implement trong
   `Implements/<X>Service`: nạp → gọi domain → transaction (nếu nhiều bước) → `AddAuditLog` → `SaveChangesAsync`.
4. **Shared** — mã lỗi mới (`ErrorCodes` + `ErrorMessages`), action nhật ký (`AuditLogConstants`), key quyền mới
   (`PermissionConstants` + dòng `Permission.Create` trong `SeedPermission`).
5. **Api** — action trong controller của module, gắn `[Permission(...)]`, mẫu `try / OkException`.
6. Build + kiểm EF (xem [07-conventions.md](07-conventions.md)).

## Thêm một use case ĐỌC

1. DTO kết quả trong `applications/.../Dtos/`.
2. Method trong `applications/.../Interfaces/Queries/I<X>Query`.
3. Implement trong `infrastructure.data/.../Queries/<X>Query`: `AsNoTracking`, phân trang bằng `.Paging(dto)`,
   chiếu ra DTO bằng `ProjectTo` (thêm `CreateMap<Entity, Dto>()` vào `MappingProfile`).
4. Method trong `I<X>Service` + `<X>Service` chỉ gọi query (và ném lỗi nếu `null`).
5. Action controller + `[Permission]`. Build.

## Thêm một module mới (ví dụ `Catalog`)

| Tầng | Tạo |
|---|---|
| domain | `Module/Catalog/Entities/`, `Services/`, `Repositories/` |
| applications | `Module/Catalog/Dtos/`, `Interfaces/Services/`, `Interfaces/Queries/`, `Implements/` |
| infrastructure.data | `Module/Catalog/Repositories/`, `Queries/`; `DbSet` trong `ShDbContext`; migration vào `Migrations/Catalog/` |
| shared | Schema trong `DbSchemas`, dải mã lỗi mới trong `ErrorCodes`, hằng quyền, tên module trong `SwaggerConstants.MODULES` |
| api | `Controllers/Module/Catalog/`; đăng ký DI trong `Program.cs` |

Entity mới, `DbSet` mới, migration mới là **đổi schema** ⇒ phải hỏi trước (skill `task-workflow`). Entity có
xoá mềm thì thêm `HasQueryFilter(e => !e.Deleted)` cho nó trong `ShDbContext`.

## Đăng ký DI

Trong `#region services` của `Program.cs`, tất cả `AddScoped`:

```csharp
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<ICatalogQuery, CatalogQuery>();
builder.Services.AddScoped<CatalogDomainService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
```

⚠️ Quên đăng ký thì build vẫn qua nhưng request đầu tiên lỗi `Unable to resolve service`. Chạy app và gọi thử
ít nhất một API của phần vừa thêm.

## Checklist

1. Nghiệp vụ nằm ở domain, service không có `if` nghiệp vụ?
2. Interface ở tầng trong, implement ở tầng ngoài, namespace khớp thư mục?
3. Ghi nhiều bước có transaction; có audit log?
4. Đã đăng ký DI, build sạch, EF đúng kỳ vọng?
5. Tài liệu trong `.claude/be/architecture/` nào cần cập nhật theo?
