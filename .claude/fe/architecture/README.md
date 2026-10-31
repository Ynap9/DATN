# Kiến trúc Frontend sh.fe

> Angular 21 standalone, zone.js, PrimeNG 21 (theme Aura), Tailwind 4. Phân tầng gọn theo tinh thần clean,
> chia tính năng theo module BE. Cập nhật 2026-10-30.

Trạng thái: **bộ khung**. Đã có layout (gốc Sakai), trang đăng nhập (chưa gọi API), auth / guard / interceptor,
UI kit `@sh/ui`. Chưa có màn hình nghiệp vụ; mỗi module BE đã có file route rỗng chờ thêm màn hình. Chỗ lệch với
BE xem [../plans/khop-voi-be.plan.md](../plans/khop-voi-be.plan.md).

## Các tầng

```text
projects/admin/src/app/
  core/       Hạ tầng toàn app, mỗi thứ một instance: http, error, layout
  shared/     Dùng chung giữa các màn hình: BaseComponent, DataTable, TblAction, Breadcrumb, SharedImports, shared/models
  models/     Interface DTO, phẳng, mỗi file ứng với một controller BE
  services/   Gọi API BE, phẳng, mỗi file ứng với một controller BE
  features/   Màn hình theo module BE: auth, core, construction, digital-signature, packaging, operation, ai
projects/lib-components   UI kit @sh/ui: component sh-*, pipe, validator, Page, Utils — không biết gì về app
projects/shared/assets    Chỉ còn tài nguyên tĩnh: SCSS layout, ảnh, font
```

## Chiều phụ thuộc

```text
features  →  services, models, shared, core, @sh/ui
services  →  models, core/http, shared/constants, shared/utils
shared    →  core, models, @sh/ui
core      →  @sh/ui
```

| Tầng | Được import | Không được import |
|---|---|---|
| `@sh/ui` | Angular, PrimeNG, thư viện ngoài | Bất cứ gì trong `admin` |
| `core` | `@sh/ui`, `@env/*` | `shared`, `models`, `services`, `features` |
| `models` | `models` khác | Mọi tầng khác |
| `services` | `models`, `core/http`, `shared/constants`, `shared/utils` | Phần còn lại của `shared`, `features` |
| `shared` | `core`, `models`, `@sh/ui` | `services`, `features` |
| `features/<a>` | `services`, `models`, `shared`, `core`, `@sh/ui` | `HttpClient` trực tiếp, `features/<b>` khác |

Vì sao: `@sh/ui` tách khỏi app để component dùng lại không kéo theo token, router, API. `core` đứng dưới
`features` để một tính năng hỏng không kéo theo hạ tầng; hai tính năng không gọi chéo để xoá / sửa một module
không làm gãy module khác — giống luật module BE không tham chiếu chéo.

## Khởi động và route

```text
main.ts → bootstrapApplication(App, appConfig)
/auth/login            features/auth (AUTH_PUBLIC_ROUTES)
/admin                 AppLayout
   ''                  HomeComponent
   auth | core | construction | digital-signature | packaging | operation | ai   → <module>.routes.ts (lazy)
**                     ShNotFoundComponent
```

URL FE `/admin/<module>/...` trùng tên module BE `api/v1/<module>/...` để tra hai đầu cùng tên.

## Đọc tiếp

| File | Nội dung |
|---|---|
| [01-workspace.md](01-workspace.md) | `angular.json`, alias import, environment, lệnh build, số đo |
| [02-app-admin.md](02-app-admin.md) | `app.config.ts`, routing, `core/` (layout, error), `shared/`, `features/` |
| [03-lib-components.md](03-lib-components.md) | UI kit `@sh/ui`: component, pipe, validator, `Page`, `Utils` |
| [04-http-auth.md](04-http-auth.md) | `core/auth` (chưa có) và `core/http` (interceptor, tải file) |
| [05-conventions.md](05-conventions.md) | **Quy ước code bắt buộc**, bảng "đặt đúng chỗ" |
| [06-them-man-hinh.md](06-them-man-hinh.md) | Các bước thêm một màn hình gọi API BE |

## Nguyên tắc không đổi

- Component / directive / pipe **standalone**, tự khai `imports`; không tạo NgModule.
- Import chéo tầng bằng alias (`@/*`, `@sh/ui`, `@env/*`), không `../../../`.
- Màn hình ở `features/<module-be>/`; service ở `services/`, model ở `models/`, đặt tên theo controller BE.
- Component không gọi `HttpClient` trực tiếp, không tự gắn token, không tự toast lỗi HTTP.
- Hợp đồng API theo BE: envelope `{ status, data, code, message }` — xem
  [../../be/architecture/06-shared.md](../../be/architecture/06-shared.md).
- Code **không thêm comment**.

> **Tiếp:** [01-workspace.md](01-workspace.md) — cấu hình workspace và build.
