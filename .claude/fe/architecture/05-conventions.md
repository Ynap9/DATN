# Quy ước code bắt buộc

> **Phần 5/6** · trước: [04-http-auth.md](04-http-auth.md) · mục lục: [README.md](README.md)

Áp dụng cho mọi thay đổi trong `sh.fe`. Luật chung (chỉ sửa chỗ cần thiết, comment, tách hàm, không commit khi
chưa yêu cầu) giống BE — xem [../../be/architecture/07-conventions.md](../../be/architecture/07-conventions.md).

## Standalone

- Component, directive, pipe: standalone (mặc định, **không** ghi `standalone: false`); không tạo NgModule.
- `imports` của component chỉ chứa đúng thứ template dùng: `CommonModule` / `ReactiveFormsModule`, module
  PrimeNG, component `sh-*`, pipe.
- Guard, interceptor viết dạng hàm (`CanActivateFn`, `HttpInterceptorFn`); inject bằng `inject()`.
- Provider toàn cục chỉ ở `app.config.ts`.

## Đặt đúng chỗ

| Muốn viết | Đặt ở |
|---|---|
| Màn hình của một tính năng | `features/<module-be>/<tinh-nang>/` (ví dụ `features/auth/role/`) |
| Service gọi API | `services/<controller>.service.ts`, phẳng, mỗi file một controller BE |
| Kiểu DTO khớp BE | `models/<controller>.model.ts` (số ít), phẳng (trường **camelCase đúng như JSON BE trả**) |
| Mục menu | `app/app.menu.ts` |
| Route của module | `features/<module-be>/<module-be>.routes.ts` |
| Component con chỉ một tính năng dùng | Ngay trong thư mục tính năng đó |
| Thứ nhiều tính năng của app cùng dùng (lớp nền, hằng quyền, pipe biết nghiệp vụ) | `app/shared/` |
| Hạ tầng: token, interceptor, toast, layout | `app/core/` |
| Component / pipe / validator thuần giao diện, dùng được ở dự án khác | `lib-components` + export ở `public-api.ts` |
| SCSS dùng chung | `projects/shared/assets/layout/` |

Thư mục module FE trùng tên **module BE** (`auth`, `core`, `construction`, `digital-signature`, `packaging`,
`operation`, `ai`) để một chức năng tra được hai đầu cùng tên. Tính năng nhỏ để file phẳng; khi một tính năng
quá 6–7 file mới chia thư mục con (`components/`).

## Tên

- File kebab-case có hậu tố: `role-list.component.ts`, `role.service.ts`, `role.model.ts`, `role.routes.ts`;
  class PascalCase + hậu tố (`RoleListComponent`, `RoleService`); interface tiền tố `I` (`IRole`).
- `core/layout/` giữ kiểu Sakai gốc (`app-topbar.ts`, class `AppTopbar`); code mới dùng kiểu có hậu tố.
- Selector: `app-` cho component của admin, `sh-` cho component UI kit (class `Sh...Component`, file
  `sh-<ten>/sh-<ten>.ts`). Biến theme SCSS dùng `$sh-*` / `--sh-*`. ⚠️ Đừng bỏ trắng tiền tố: thẻ tự tạo bắt buộc
  có `-`, và `button`, `select`, `table` sẽ trùng thẻ HTML gốc.
- Hằng route của module: `<MODULE>_ROUTES` (`CONSTRUCTION_ROUTES`).
- Field inject private: `private readonly _camelCase = inject(...)` (`_roleService`).

## Gọi API

- Service `@Injectable({ providedIn: 'root' })`, base URL `environment.api` (import `@env/environment`), route
  **copy đúng** từ controller BE, có version (`api/v1/auth/role/paging`; `/connect/token` không có version).
- Service trả `Observable`; component subscribe kèm `takeUntilDestroyed(this.destroyRef)`.
- Service khai `api = apiUrl('auth/role')` (`@/shared/utils/api-url`) và `http = inject(HttpClient)`; `baseUrlInterceptor` ghép base URL. Không ghi cứng `v1`.
- Hằng số đặt ở `shared/constants/`. `ApiVersionConstants` (`@/shared/constants/api-version.constants`) khớp `ApiVersionConstants` của BE. BE thêm `V2` thì thêm ở FE; endpoint riêng dùng `apiUrl('auth/role', ApiVersionConstants.V2)`; BE đổi `DEFAULT_VERSION` thì đổi một chỗ ở FE. `/connect/*` không có version.
- App chạy **zone.js**, field thường vẫn cập nhật giao diện; nút ghi khoá bằng `submitting` + `finalize()` vì không có interceptor chống trùng.
- Không tự gắn header `Authorization`, không tự toast lỗi HTTP (interceptor đã làm). Muốn tắt thì dùng
  `HttpContextToken` trong `@/core/http/http-context`.

## Form và giao diện

- Form reactive (`FormBuilder`), kiểm bằng `isFormInvalid` của `BaseComponent` trước khi gửi.
- Bảng dùng `DataTable` + `TblAction`; input, nút dùng `sh-*`; `imports` dùng `SharedImports`. Màn mới không dùng `sh-table`, `Page`.
- `DataTable` có thêm `paginator` (mặc định `true`), `reorderable` + output `rowReorder`, `selectable` + `[(selection)]`, cột `SWITCH`
  (bấm phát `CELL_CLICK`, khoá bằng `disabled`). Không truyền thì bảng y như cũ. Dòng trống thay nội dung bằng phần tử con có
  thuộc tính `tableEmpty` (vd. câu hướng dẫn + nút thêm), không có thì hiện "Không có dữ liệu".
- Cột thao tác: header `Thao tác`, `width: 100px; text-align: center`. `TblAction` là nút ⋯ mở dropdown; mỗi `ITableAction` bắt buộc
  có `icon`, thao tác xoá đặt `severity: 'danger'` (chữ đỏ).
- Style: Tailwind cho bố cục; style riêng component dưới 8 kB.
- Thông điệp người dùng tiếng Việt, lấy từ `SuccessMessage` / `ErrorMessage` khi có sẵn.

## Kiểm trước khi báo xong

```bash
npx ng build admin --configuration development
npm run build:admin
```

Build sạch, không vượt budget. Tính năng gọi API thì chạy `sh.be.api` và thử trên trình duyệt ít nhất một luồng.

> **Tiếp:** [06-them-man-hinh.md](06-them-man-hinh.md) — các bước thêm một màn hình gọi API BE.
