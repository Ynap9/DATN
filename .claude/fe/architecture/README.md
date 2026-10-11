# Kiến trúc Frontend sh.fe

> Angular 21 theo **NgModule** (không standalone), PrimeNG 21 (theme Aura), Tailwind 4. Cập nhật 2026-10-11.

Trạng thái: **bộ khung**. Đã có layout, đăng nhập (chưa gọi API), interceptor, guard, thư viện component `h-*`.
Chưa có màn hình nghiệp vụ nào; menu đang rỗng. Chỗ lệch với BE xem
[../plans/khop-voi-be.plan.md](../plans/khop-voi-be.plan.md).

## Ba project trong một workspace

```text
projects/admin           Ứng dụng quản trị: AppModule, layout (core/), module tính năng (modules/)
projects/lib-components  Thư viện dùng chung: component h-*, pipe, HComponentBase, auth/interceptor, hằng số
projects/shared          Tài nguyên chung: assets (SCSS layout, ảnh), environments, AuthGuard, validator
```

## Chiều phụ thuộc

```text
admin           → lib-components, shared
shared (guard)  → lib-components (TokenService, BaseConsts)
lib-components  → (chỉ thư viện ngoài: Angular, PrimeNG, moment, rxjs)
```

`lib-components` **không** được import gì từ `admin`. Component chỉ dùng cho admin thì để trong admin.

## Kiến trúc module

```text
AppModule ─ CoreModule (layout: topbar, sidebar, menu, footer, configurator)
          ─ MyLibComponentsModule.forRoot()   (h-*, pipe, LibHelperService)
          ─ AppRoutingModule
               /auth   → AuthModule  (lazy)  → h-login-fe
               /admin  → AppLayout + AdminModule (lazy) → các module tính năng
               **      → HNotFoundComponent
```

## Đọc tiếp

| File | Nội dung |
|---|---|
| [01-workspace.md](01-workspace.md) | `angular.json`, alias, environment, lệnh build |
| [02-app-admin.md](02-app-admin.md) | AppModule, routing, layout, module tính năng |
| [03-lib-components.md](03-lib-components.md) | Component `h-*`, `HComponentBase`, `LibHelperService`, `Page`, pipe |
| [04-http-auth.md](04-http-auth.md) | Token, interceptor, guard, xử lý response và lỗi |
| [05-conventions.md](05-conventions.md) | **Quy ước code bắt buộc** |
| [06-them-man-hinh.md](06-them-man-hinh.md) | Các bước thêm một màn hình gọi API BE |

## Nguyên tắc không đổi

- Mọi component / directive / pipe là `standalone: false`, khai báo trong một NgModule.
- Module tính năng **lazy load** dưới `/admin`, mỗi module một thư mục trong `admin/src/app/modules/`.
- Gọi API qua service của module (HttpClient), **không** gọi `HttpClient` thẳng trong component.
- Token gắn tự động bởi `HandleInterceptorService`; component không tự thêm header.
- Component trang kế thừa `HComponentBase` để có `page`, thông báo, kiểm form dùng chung.
- Hợp đồng API theo BE: envelope `{ status, data, code, message }` — xem
  [../../be/architecture/06-shared.md](../../be/architecture/06-shared.md).
- Code **không thêm comment**.

> **Tiếp:** [01-workspace.md](01-workspace.md) — cấu hình workspace và build.
