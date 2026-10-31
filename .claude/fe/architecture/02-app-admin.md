# Ứng dụng admin

> **Phần 2/6** · trước: [01-workspace.md](01-workspace.md) · mục lục: [README.md](README.md)

`projects/admin/src/app`. Bootstrap bằng `bootstrapApplication(App, appConfig)` trong `main.ts`.

## Thư mục

| Thư mục / file | Chứa gì |
|---|---|
| `app.ts`, `app.config.ts`, `app.routes.ts` | Component gốc (chỉ `router-outlet`), provider toàn app, route gốc |
| `core/http/` | Interceptor dạng hàm, `HttpContextToken`, `DownloadService` — xem [04-http-auth.md](04-http-auth.md) |
| `core/error/` | `GlobalErrorHandler` |
| `core/layout/` | `app-layout`, `app-topbar`, `app-sidebar`, `app-menu`, `app-menuitem`, `app-footer`, `layout.service.ts`, `layout.models.ts` |
| `shared/base/` | `BaseComponent` — lớp nền cho component trang |
| `features/home/` | Trang chủ `/admin` |
| `features/<module>/` | `auth`, `core`, `construction`, `digital-signature`, `packaging`, `operation`, `ai` — mỗi module một `<module>.routes.ts` |

⚠️ `core/` (hạ tầng FE) khác `features/core/` (màn hình của module BE `Core`: nhật ký, job nền). Đừng đặt màn hình
nhật ký vào `app/core/`.

## app.config.ts cung cấp gì

| Provider | Vì sao ở gốc |
|---|---|
| `provideBrowserGlobalErrorListeners()` | Đưa lỗi ngoài Angular vào `GlobalErrorHandler` |
| `provideZoneChangeDetection({ eventCoalescing: true })` | Chạy zone.js, cần `polyfills: ["zone.js"]` trong `angular.json` |
| `provideRouter(routes)` | Route gốc |
| `provideHttpClient(withInterceptors([baseUrlInterceptor, httpErrorInterceptor]))` | Thứ tự: ghép base URL → bắt lỗi HTTP |
| `{ provide: APP_MENU, useValue: APP_MENU_ITEMS }` | Menu khai ở `app/app.menu.ts`, thêm mục không phải sửa `core` |
| `provideAnimationsAsync()` | `sh-table` dùng `trigger()` của `@angular/animations` |
| `providePrimeNG({ theme: { preset: Aura, options: { darkModeSelector: false } } })` | Theme chung, tắt dark mode |
| `ErrorHandler` → `GlobalErrorHandler` | Lỗi tải chunk lazy (deploy bản mới khi tab cũ còn mở) thì reload trang |
| `MessageService`, `DialogService`, `ConfirmationService` | Một instance cho toast / dialog / confirm |

⚠️ `provideAnimationsAsync` bị đánh dấu deprecated từ Angular 20.2 nhưng vẫn chạy; đừng gỡ khi `sh-table` còn
dùng `trigger()`.

## Routing

| Path | Guard | Đích |
|---|---|---|
| `''` | — | redirect `admin` |
| `auth` | — | `AUTH_PUBLIC_ROUTES` (lazy): `login`, `access`, `error` |
| `admin` | `authGuard` | `AppLayout`, con: `''` = `HomeComponent`, mỗi module BE một nhánh lazy |
| `**` | — | `ShNotFoundComponent` |

⚠️ Route `admin` có `children` thì **phải** có con `path: ''`, nếu không vào `/admin` sẽ rơi xuống trang 404.
Vì vậy mới có `HomeComponent`; đừng xoá nó khi chưa có trang mặc định khác.

## Lớp nền

`BaseComponent` (`shared/base/base.component.ts`) lấy từ SMS, dùng `inject()` nên lớp con **không** cần `constructor` /
`super(injector)`:

| Thành viên | Dùng để |
|---|---|
| `router`, `_activatedRoute`, `_messageService`, `_dialogService`, `_confirmationService` | Service inject sẵn |
| `form`, `ValidationMessages`, `isFormInvalid`, `getError`, `getErrorMessage` | Kiểm form, lấy câu lỗi theo khai báo |
| `loading`, `totalRecords`, `MAX_PAGE_SIZE`, `START_PAGE_NUMBER` | Trạng thái danh sách |
| `isResponseSucceed` | Kiểm `status` của envelope |
| `messageSuccess` / `messageWarning` / `messageError` | Toast |
| `confirmDelete` / `confirmAction` | Xác nhận, hiện ở `<p-confirmdialog>` trong `AppLayout` |
| `onRouteActivated` | Lớp con override để tải lại khi quay về route |
| `designToString`, `stringToDesign` | Đổi design JSON sang chuỗi và ngược lại |

⚠️ `BaseComponent` đăng ký router event trong constructor: chỉ kế thừa ở component trang, không kế thừa ở component
lặp theo dòng bảng, vì mỗi instance tạo thêm một subscription.

## Layout

`AppLayout` gồm topbar, sidebar (chứa `AppMenu`, khối user có nút **Đăng xuất** chưa nối logic),
vùng `router-outlet`. Tên user ở sidebar đang để cố định, chờ phần auth. `AppMenu.model` là
`MenuItem[]` PrimeNG, hiện gán `[]` trong `setMenu()`.

⚠️ Menu chưa lọc theo quyền và chưa nơi nào gọi `setCurrentUser()` — chờ đăng nhập thật
([../plans/khop-voi-be.plan.md](../plans/khop-voi-be.plan.md)).

> **Tiếp:** [03-lib-components.md](03-lib-components.md) — UI kit `@sh/ui`.
