# Kiến trúc Backend sh.be

> Clean Architecture + DDD, tổ chức modular monolith (module `Auth`, `Core`). Cập nhật 2026-10-11.

.NET 9, ASP.NET Core Web API, EF Core + SQL Server, ASP.NET Identity, OpenIddict, AutoMapper, NLog.

## Sáu project

```text
sh.be.api                      Controller, PermissionAttribute, HttpRequestContext, Program.cs (composition root)
sh.be.applications             Use case: Interfaces/{Services,Queries}, Implements, Dtos, BaseService
sh.be.domain                   Lõi nghiệp vụ: Entities, Services (domain service, policy), Repositories (interface)
sh.be.infrastructure.data      EF Core: ShDbContext, Repositories, Queries, UnitOfWork, Migrations, Seeder
sh.be.infrastructure.external  Dịch vụ ngoài: mail SMTP, Google Sheet, Excel, kho file MinIO, file mẫu
sh.be.shared                   Shared kernel: ApiResponse, ErrorCodes, exception, hằng số, settings, utils
```

## Chiều phụ thuộc

```text
api                      → applications, infrastructure.data, infrastructure.external
infrastructure.data      → applications, domain
infrastructure.external  → applications
applications             → domain
domain                   → shared
shared                   → (không)
```

Mọi mũi tên hướng **vào trong**. Tầng ngoài implement interface do tầng trong khai báo (dependency inversion):

| Interface (tầng trong) | Implementation (tầng ngoài) |
|---|---|
| `domain/.../Repositories/IRoleRepository`… | `infrastructure.data/.../Repositories/RoleRepository`… |
| `domain/Base/IUnitOfWork` | `infrastructure.data/Base/UnitOfWork` |
| `applications/.../Interfaces/Queries/IRoleQuery`… | `infrastructure.data/.../Queries/RoleQuery`… |
| `applications/Base/Interfaces/IMailService` | `infrastructure.external/Mail/Implements/MailService` |
| `applications/Base/Interfaces/IDataReader` | `infrastructure.external/DataReader/Implements/GoogleSheetReader` (qua `DataReaderBase`) |
| `applications/Base/Interfaces/IFileDownloader` | `infrastructure.external/FileDownloader/Implements/HttpFileDownloader` |
| `applications/Base/Interfaces/IFileStorage` | `infrastructure.external/Storage/Implements/S3FileStorage` (MinIO) |
| `applications/Base/Interfaces/ISampleFileReader` | `infrastructure.external/SampleFile/Implements/SampleFileReader` |
| `applications/Base/Interfaces/IPluginSetupProvider` | `infrastructure.external/Plugin/Implements/PluginSetupProvider` |
| `applications/Base/Interfaces/ICertificateValidator` | `infrastructure.external/Certificate/Implements/CertificateValidator` |
| `applications/Base/Interfaces/IRequestContext` | `api/Services/HttpRequestContext` |
| `applications/Base/Interfaces/IExcelTemplateWriter` | `infrastructure.external/Excel/Implements/ExcelTemplateWriter` |
| `applications/Base/Interfaces/ISpreadsheetWriter`, `ISpreadsheetAuthorizer` | `infrastructure.external/Spreadsheet/Implements/GoogleSheetWriter`, `GoogleSheetAuthorizer` |
| `applications/Base/Interfaces/IPdfSigner`, `ISigningQueue`, `ITimestampClient` | `infrastructure.external/Pdf`, `Signing`, `Timestamp` (xem [09-chu-ky-so.md](09-chu-ky-so.md)) |
| `applications/.../DigitalSignature/Interfaces/Services/ISigningRunner` | `applications/.../DigitalSignature/Implements/SigningRunner` (singleton, chạy nền) |
| `applications/.../Interfaces/Services/IRoleService`… | `applications/.../Implements/RoleService`… |

Chiều này do `.csproj` cưỡng chế: domain gọi `ShDbContext` là build lỗi ngay.

## Đọc tiếp

| File | Nội dung |
|---|---|
| [01-api.md](01-api.md) | Controller mỏng, `ApiResponse`, `[Permission]`, OpenIddict, composition root |
| [02-applications.md](02-applications.md) | Service điều phối, `BaseService`, Services vs Queries, DTO |
| [03-domain.md](03-domain.md) | Entity giàu hành vi, domain service, policy, repository interface |
| [04-infrastructure-data.md](04-infrastructure-data.md) | `ShDbContext`, repository, query, UnitOfWork, soft delete, migration, seeder |
| [05-infrastructure-external.md](05-infrastructure-external.md) | Gửi mail |
| [06-shared.md](06-shared.md) | Mã lỗi, exception, hằng số, settings, utils |
| [07-conventions.md](07-conventions.md) | **Quy ước code bắt buộc** |
| [08-them-chuc-nang.md](08-them-chuc-nang.md) | Các bước thêm một use case hoặc một module mới |
| [09-chu-ky-so.md](09-chu-ky-so.md) | Pipeline lô ký, phiên plugin, PDF revision, CMS, TSA |
| [../db/README.md](../db/README.md) | Lệnh migration và cập nhật database |
| [../nghiep-vu/README.md](../nghiep-vu/README.md) | **Nghiệp vụ**: người dùng, phân quyền, vai trò mặc định, nhật ký hệ thống |

## Nguyên tắc không đổi

- **Nghiệp vụ nằm ở domain.** Service chỉ nạp dữ liệu, giao cho entity / domain service quyết định, rồi lưu.
- **Ghi đi qua repository + aggregate, đọc đi qua query trả thẳng DTO** (CQRS-lite, chung một DB).
- Controller **không có nghiệp vụ** — gọi service rồi bọc `ApiResponse`.
- Mọi REST trả **HTTP 200**; trạng thái thật nằm trong `ApiResponse.Status` (`1` ok, `0` lỗi) và `Code`. Ngoại lệ: API tải
  file (`excel-template`) trả thẳng file khi thành công, lỗi vẫn là `ApiResponse`.
- Lỗi nghiệp vụ ⇒ `throw new UserFriendlyException(ErrorCodes.X)`; controller bắt bằng `OkException(ex)`.
- Bản ghi xoá mềm bị **global query filter** ẩn đi; không viết tay `!x.Deleted`.
- Giờ trong service lấy qua `GetVietnamTime()` (dùng `TimeProvider`), không gọi `DateTime.Now`.
- Code **không thêm comment**. Chi tiết ở [07-conventions.md](07-conventions.md).

> **Tiếp:** [01-api.md](01-api.md) — tầng ngoài cùng nhận request.
