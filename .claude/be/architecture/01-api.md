# Tầng API — sh.be.api

> **Phần 1/8** · mục lục: [README.md](README.md)

Presentation + **composition root**. Tham chiếu `applications`, `infrastructure.data`, `infrastructure.external`.

## Thư mục

| Thư mục / file | Chứa gì |
|---|---|
| `Base/BaseController.cs` | `OkException(ex)`: đổi exception thành `ApiResponse` lỗi |
| `Controllers/Module/Auth/` | `AuthorizationController` (token), `UserController`, `RoleController`, `PermissionController` |
| `Controllers/Module/Core/` | `AuditLogController` |
| `Controllers/Module/Construction/` | `DocumentTypeController` |
| `Attributes/PermissionAttribute.cs` | Kiểm quyền theo key, gọi `IPermissionService.HasAnyPermission` |
| `Services/HttpRequestContext.cs` | Implement `IRequestContext`: `UserId`, `Source`, `ContentType` từ `HttpContext` |
| `Worker/AuthWorker.cs` | Tạo client OpenIddict lúc khởi động (`service-worker`, `client-web`, `client-web2`, `client-guest-traobang`) |
| `Swagger/` | Chia tài liệu Swagger / Scalar theo module |
| `Program.cs` | Đăng ký DI, cấu hình auth, CORS, chạy seeder |

## Controller mỏng

```csharp
[HttpGet("{id}")]
[Permission(PermissionConstants.FUNCTION_ROLE_VIEW)]
public ApiResponse FindById([FromRoute] int id)
{
    try
    {
        var data = _roleService.FindById(id);
        return new(data);              // Status = 1, Code = 200, Message = "Ok"
    }
    catch (Exception ex)
    {
        return OkException(ex);        // Status = 0, Code = mã lỗi, Message tiếng Việt
    }
}
```

- Controller chỉ inject **interface trong `Interfaces/Services/`** của applications.
- Không inject repository, query, `ShDbContext`, `UserManager` vào controller nghiệp vụ.
- `OkException` lấy message theo thứ tự: `MessageLocalize` của exception → `ErrorMessages.GetMessage(code)`.
  Exception lạ trả `500` + `"Internal server error"`.

## Phân quyền

- `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` ở class: bắt buộc có token.
- `[Permission(key1, key2…)]` ở action: có **ít nhất một** key là qua. Luật tính nằm ở
  `PermissionGrantPolicy` (domain): SuperAdmin luôn qua; quyền cha được chọn ⇒ quyền con cũng được.
- Thiếu token ⇒ `401`; có token nhưng thiếu quyền ⇒ `403` (`ForbidResult`).

⚠️ Attribute truy vấn DB mỗi request (vai trò + quyền + danh sách permission), chưa cache. Đừng gắn
`[Permission]` vào endpoint gọi dày đặc mà không tính tới chi phí này.

## Xác thực — OpenIddict

| Mục | Giá trị |
|---|---|
| Token endpoint | `POST /connect/token` (`AuthorizationController`) |
| Grant type | password, refresh_token, client_credentials, authorization_code (+PKCE) |
| Ký token | Khoá đối xứng `AuthServer:SecretKey` trong appsettings |
| Claim vai trò | Lấy qua `IUserService.GetRoleNames` — đã loại vai trò xoá mềm |

⚠️ `AuthorizationController` còn nhận trực tiếp `UserManager<AppUser>` (`[FromServices]`) để kiểm mật khẩu
khi cấp token. Đây là chỗ còn dính Identity ở tầng api, đã biết; đừng nhân bản kiểu này sang controller khác.

## Composition root — Program.cs

- Mọi interface ↔ implementation đăng ký ở vùng `#region services`, scope `AddScoped`;
  `TimeProvider.System` là `AddSingleton`.
- Domain service (`RoleDomainService`, `UserDomainService`) đăng ký **class cụ thể**, không qua interface.
- Seeder chạy sau `builder.Build()` theo thứ tự: Role → Permission → User → RolePermission.
- Connection string tên `SO_HOA_DB`; CORS lấy từ `AllowedHosts` (phân cách `;`).

> **Tiếp:** [02-applications.md](02-applications.md) — service điều phối use case.
