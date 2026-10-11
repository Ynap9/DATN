# Ứng dụng admin

> **Phần 2/6** · trước: [01-workspace.md](01-workspace.md) · mục lục: [README.md](README.md)

`projects/admin/src/app`. Bootstrap bằng `platformBrowserDynamic().bootstrapModule(AppModule)`.

## Thư mục

| Thư mục / file | Chứa gì |
|---|---|
| `app.module.ts`, `app.ts` | Module gốc, component gốc `App` |
| `app-routing.module.ts` | Route cấp gốc |
| `core/core.module.ts` | Khai báo và export các component layout |
| `core/layout/` | `app-layout`, `app-topbar`, `app-sidebar`, `app-menu`, `app-menuitem`, `app-footer`, `app-configurator`, `app-floating-configurator` |
| `core/services/layout.service.ts` | Trạng thái layout (menu mở/đóng, theme, configurator) |
| `interfaces/` | Kiểu cấu hình layout |
| `modules/auth/` | `AuthModule` lazy: `AccountComponent` (khung + `p-toast`) và route `login` |
| `modules/admin/` | `AdminModule` lazy: nơi gắn các module tính năng (hiện rỗng) |

## AppModule cung cấp gì

| Provider | Vì sao ở gốc |
|---|---|
| `providePrimeNG({ theme: { preset: Aura, options: { darkModeSelector: false } } })` | Theme dùng chung, tắt dark mode |
| `HTTP_INTERCEPTORS` → `HandleInterceptorService` | Mọi request đều đi qua (token, chống bấm đúp, toast lỗi) |
| `ErrorHandler` → `GlobalErrorHandler` | Lỗi tải chunk lazy thì reload trang |
| `MessageService`, `DialogService`, `ConfirmationService` | Một instance cho toast / dialog / confirm toàn app |
| `MyLibComponentsModule.forRoot()` | Đăng ký `LibHelperService` một lần |

Module tính năng import `MyLibComponentsModule` **không** `.forRoot()`.

## Routing

| Path | Guard | Đích |
|---|---|---|
| `''` | — | redirect `/admin` |
| `auth` | `AuthGuard` | `AuthModule` (lazy) → `auth/login` = `HLoginFeComponent` |
| `admin` | `AuthGuard` | `AppLayout` + children lazy `AdminModule` |
| `**` | — | `HNotFoundComponent` |

`AuthGuard` (`projects/shared/guard`): chưa có token ⇒ về `/auth/login`; có token mà vào trang login ⇒ về
`/admin`. Guard chỉ kiểm **có token hay không**, không kiểm hạn token, không kiểm quyền.

Module tính năng mới gắn vào `AdminRoutingModule` dạng lazy:

```typescript
RouterModule.forChild([
    { path: 'user', loadChildren: () => import('./user/user.module').then((m) => m.UserModule) },
])
```

## Layout

`AppLayout` gồm topbar, sidebar (chứa `AppMenu`), vùng `router-outlet`, footer. `AppMenu.model` là
`MenuItem[]` của PrimeNG, hiện gán `[]` trong `setMenu()`.

⚠️ Menu chưa lọc theo quyền và topbar chưa có đăng xuất. Khi thêm, menu phải dựng từ quyền của user
(`GET /api/auth/user/me`), key quyền khớp `PermissionConstants` của BE — xem
[../plans/khop-voi-be.plan.md](../plans/khop-voi-be.plan.md).

## Đăng nhập

`HLoginFeComponent` (trong lib-components) có form `username`, `password` và validate bắt buộc.

⚠️ `onSubmit()` **chưa gọi API**: chưa có lời gọi `POST /connect/token`, chưa lưu token, chưa điều hướng. Đây là
việc đầu tiên trong plan khớp BE.

> **Tiếp:** [03-lib-components.md](03-lib-components.md) — thư viện component và lớp nền dùng chung.
