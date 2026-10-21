# Tầng API — sh.be.api

> **Phần 1/9** · mục lục: [README.md](README.md) · cập nhật 2026-10-20 (version API)

Presentation + **composition root**. Tham chiếu `applications`, `infrastructure.data`, `infrastructure.external`.

## Thư mục

| Thư mục / file | Chứa gì |
|---|---|
| `Base/BaseController.cs` | `OkException(ex)`: đổi exception thành `ApiResponse` lỗi |
| `Controllers/Module/Auth/` | `AuthorizationController` (token), `UserController`, `RoleController`, `PermissionController` |
| `Controllers/Module/Core/` | `AuditLogController` |
| `Controllers/Module/Construction/` | `DocumentTypeController`, `DocumentFieldController`, `ProjectController`, `ProjectConfigController` (callback Google `[AllowAnonymous]`), `SubmissionController` (một `ISubmissionService` cho cả import, xem, kiểm tra + `ISubmissionFileService`; `proxy-file` trả stream PDF, lỗi vẫn `Ok(OkException(ex))`; 7 API xem / kiểm tra + 4 API QA hồ sơ, tổng 18 route; route có id luôn ràng buộc `{id:int}` để không nuốt `paging`, `count`, `proxy-file`) |
| `Controllers/Module/DigitalSignature/` | `TemplateController` (mẫu chữ ký; `PUT {id}/config` nhận `[FromForm]` multipart), `PluginController` (phiên bản + bộ cài plugin ký số, chỉ cần đăng nhập), `SigningBatchController` (lô ký; `POST {id}/files` multipart, `[RequestSizeLimit]` 512 MB) |
| `Attributes/PermissionAttribute.cs` | Kiểm quyền theo key, gọi `IPermissionService.HasAnyPermission` |
| `Services/HttpRequestContext.cs` | Implement `IRequestContext`: `UserId`, `Source`, `ContentType` từ `HttpContext` |
| `Services/AuthorizationStateProtector.cs` | Implement `IAuthorizationStateProtector`: ký / đọc `state` OAuth bằng `ITimeLimitedDataProtector` |
| `Worker/AuthWorker.cs` | Tạo client OpenIddict lúc khởi động (`service-worker`, `client-web`, `client-web2`, `client-guest-traobang`) |
| `Swagger/` | Tài liệu Swagger / Scalar theo version × module: `ConfigureSwaggerOptions`, `SwaggerDocumentName`, filter nhóm tag |
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

## Version API

Version theo URL: controller khai `[Route("api/v{version:apiVersion}/<module>/<ten>")]` + `[ApiVersion(ApiVersionConstants.V1)]`,
gọi `api/v1/...` (`v1.0` cũng nhận). Thư viện `Asp.Versioning.Mvc.ApiExplorer`, cấu hình ở `#region api version` của `Program.cs`.

| Mục | Cách làm |
|---|---|
| Không version | `AuthorizationController` là `[ApiVersionNeutral]`: `/connect/token`, `/connect/authorize` là URL chuẩn OAuth mà client đã đăng ký |
| Báo version | Mọi response có `api-supported-versions` (và `api-deprecated-versions` khi có) |
| Ngày ngừng | `appsettings` → `ApiVersion:Sunsets[]` (`Version`, `Date`, `Link`) ⇒ header `Sunset` + `Link rel="sunset"` (RFC 8594), ghi chú trên Swagger |
| Swagger / Scalar | Tự sinh mỗi version × (`all` + `SwaggerConstants.MODULES`): `/swagger/v1-all/swagger.json`, `/swagger/v1-auth/swagger.json`… |

Vòng đời: **(1)** thêm `V2` vào `ApiVersionConstants` + controller / action v2 (`[MapToApiVersion]`), v1 vẫn chạy → **(2)** v2 ổn định:
`[ApiVersion(V1, Deprecated = true)]`, `DEFAULT_VERSION = V2` → **(3)** khai `ApiVersion:Sunsets`, báo FE → **(4)** qua ngày đó mới xoá code v1.

⚠️ `IApiVersionDescriptionProvider` trả một mô tả cho **mỗi cặp (module, version)**, không phải mỗi version: lặp thẳng nó thì Swagger
đẻ 25 tài liệu (`auth-all`, `core-auth`…). Luôn gom theo `ApiVersion` rồi đặt tên qua `SwaggerDocumentName.BuildApiVersionGroupName`.
⚠️ Version không có (`api/v2/...`) trả **404**, không phải 400 — với URL segment, đường dẫn không khớp route nào.

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
- Seeder chạy sau `builder.Build()` theo thứ tự: Role → Permission → User → RolePermission; rồi `ISigningRunner.RecoverAsync` đưa lô kẹt `SIGNING` về `PAUSED`.
- Connection string tên `SO_HOA_DB`, `MultipleActiveResultSets=False` (bật MARS làm bulk import treo transaction — xem
  [../nghiep-vu/11-ho-so-import.md](../nghiep-vu/11-ho-so-import.md)); CORS lấy từ `AllowedHosts` (phân cách `;`).

⚠️ `UseRequestLocalization` ép culture **invariant**: form-data bind số theo culture của máy, máy `vi-VN` đọc `0.6` thành
`6` nên toạ độ mẫu chữ ký bị báo `603`. Đừng gỡ dòng này khi dọn `Program.cs`.

> **Tiếp:** [02-applications.md](02-applications.md) — service điều phối use case.
