# Quy ước code bắt buộc

> **Phần 5/13** · trước: [04-http-auth.md](04-http-auth.md) · mục lục: [README.md](README.md)

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
- Cột `PROGRESS` (giá trị 0..100, làm tròn, tooltip `progressTooltip(row)`), `BADGE` (`badge(row)` → `{ label, tone }`, tone
  `blue` / `amber` / `green` / `red` / `gray` theo biến `--app-<tone>` / `--app-<tone>-soft`), `frozen: 'left' | 'right'` (có cột
  frozen thì bảng tự bật cuộn ngang; dưới 768px cột `left` thôi dính để còn chỗ cuộn, cột `right` vẫn dính), `NUMBER` (số vi-VN 1.234, thiếu / null → "—" mờ; `CURRENCY` rỗng ra ô trống). Nhãn BADGE
  là component `StatusBadge` (`shared/components/status-badge`) — chỗ khác cần cùng nhãn (vd. thẻ dự án) dùng lại nó. Task 38.
- ⚠️ Độ rộng cột kéo xong lưu `tbl_col_widths_<tableKey>` **theo key cột**: `key` → `field` → `header`. Cột không `field` mà header
  có thể trùng / đổi chữ thì khai `key`. Cache cũ khoá theo vị trí bị bỏ qua (người dùng kéo lại một lần).
- ⚠️ Căn lề ô dùng `cellStyle: 'text-align: right'` (cùng chuỗi với `headerContainerStyle`), **không** `cellClass: 'text-right'`: CSS `td` của bảng đè mất class.
- Lọc theo cột: cột khai `filter: { type: 'text' | 'select' | 'dateRange', options? }` thì tiêu đề có phễu + popover
  (`DataTableColumnFilter`). Màn giữ state, truyền `[filterValues]` (key cột → giá trị; khoảng ngày `{ from?, to? }` dạng
  `yyyy-MM-dd`), nhận `(filterChange)` = **toàn bộ** map sau mỗi lần Áp dụng / Xoá (key rỗng bị bỏ). Màn tự về trang 1 và tải lại.
- Cấu hình cột: `[columnSettings]="true"` + `tableKey` → cột `ACTIONS` thay chữ bằng ⚙ mở dialog `DataTableColumnSettings`
  (ẩn / hiện, kéo thứ tự, đặt lại). Cột `locked` luôn hiện, giữ chỗ cũ; `hidden` = ẩn mặc định. Lưu `tbl_cols_<tableKey>` =
  `[{ key, visible }]` của các cột không khoá; tập key khác cột hiện tại thì về mặc định. Không bật thì `hidden` bị bỏ qua.
- Cột thao tác: header `Thao tác`, `width: 100px; text-align: center`. `TblAction` là nút ⋯ mở dropdown; mỗi `ITableAction` bắt buộc
  có `icon`, thao tác xoá đặt `severity: 'danger'` (chữ đỏ); ẩn theo từng dòng bằng `visible: (row) => boolean` (vd. "Mở Google Sheet").
  Ngoài bảng (vd. thẻ) dùng lại `TblAction` với cùng mảng `ITableAction`, đổi nút qua input `icon` (mặc định `pi pi-ellipsis-h`).
- Cột `CUSTOM_COMP` luôn truyền `{ row, rowIndex, data }` — component ô phải khai đủ ba input, thiếu thì Angular báo lỗi console khi gán input không tồn tại.
- ⚠️ `ng test` (Karma) **không** kiểm strict template như `ng build` / `ng serve`: truyền `number` vào pipe khai `string` vẫn
  qua test nhưng build đỏ. Sửa template xong luôn chạy `npm run build:admin`, đừng chỉ chạy test.
- Style: Tailwind cho bố cục; style riêng component dưới 8 kB.
- Chuẩn kích cỡ (chốt 23/11): chữ thân 14px, chữ phụ / nhãn trường / tag 12px, tiêu đề khối 13–14px đậm 600. Ô nhập và nút
  thường cao ~36px, nút / ô `size="small"` ~28px chữ 12px. Khoảng cách theo bội 4px (`gap-2` = 8px, `p-4` = 16px). Thiết kế
  ghi số px nào thì dùng đúng lớp Tailwind tương ứng (`text-xs` = 12px, `h-9` = 36px), không cần `text-[12px]`.
- ⚠️ `html { font-size: 14px }` (`shared/assets/layout/_core.scss`) làm mọi cỡ theo rem co 12,5% (`text-xs` ra 10,5px, nút
  nhỏ 22px). Đã chặn ở gốc: `tailwind.css` khai `--spacing` / `--text-*` / `--radius-*` bằng px, `variables/_theme.scss` đặt
  token cỡ PrimeNG (`--p-form-field-*`, `--p-inputtext-*`, `--p-button-*`, `--p-tag-*`) bằng px. Đừng đổi gốc sang 16px:
  PrimeNG đặt cứng `font-size: 1rem` cho ô nhập / nút nên chữ trong ô sẽ thành 16px. Cần cỡ mới thì thêm token px ở hai chỗ trên.
- ⚠️ Lớp Tailwind đặt trên nút / ô PrimeNG có thể không ăn (vd. `px-2` trên `pButton` vẫn ra padding mặc định). Lý do:
  `providePrimeNG` không bật `cssLayer`, nên style PrimeNG nằm ngoài layer và thắng lớp Tailwind. Cách tránh: chỉ thêm `!`
  (`px-2!`, `text-[13px]!`) cho thuộc tính PrimeNG tự đặt, như padding, gap, cỡ chữ nút, độ rộng nút chỉ có icon, độ đậm nhãn
  nút. Height, cỡ icon `.pi` thì không cần `!`. Ví dụ ở thanh trên màn Nhập liệu ([08-man-nhap-lieu.md](08-man-nhap-lieu.md)).
- ⚠️ Biến `--app-*` (`--app-line`, `--app-ink`…) trong dialog mở bằng `DialogService` không có giá trị → viền thành đen, màu chữ
  rơi về mặc định. Lý do: biến khai báo ở `.layout-wrapper` (`shared/assets/layout/_main.scss`), còn dialog gắn thẳng vào `body`.
  Cách tránh: trong dialog dùng mã màu (`border-[#e4e8e6]`, `text-[#8a958f]`…), như dialog Nhóm nhân viên. Cũng đừng dùng `-mt-*`
  cho dòng đầu nội dung dialog: nó bị header che mất nửa.
- Bảng: cột chữ dài không khai `width` thì đặt `min-width` (vd. cột trường động của bảng văn bản 180px); cột ngắn (mã, hồ sơ,
  hộp, ngày) `cellStyle: 'white-space: nowrap'`. `.p-tag` đã `nowrap` toàn cục.
- ⚠️ Repo **không** có cấu hình Prettier: đừng chạy `npx prettier --write` — mặc định 80 cột ngắt mỗi thuộc tính một dòng, lệch hẳn kiểu template
  hiện có (một thẻ trên ít dòng dài). Định dạng tay theo file xung quanh.
- Thông điệp người dùng tiếng Việt, lấy từ `SuccessMessage` / `ErrorMessage` khi có sẵn.

## Kiểm trước khi báo xong

```bash
npx ng build admin --configuration development
npm run build:admin
```

Build sạch, không vượt budget. Tính năng gọi API thì chạy `sh.be.api` và thử trên trình duyệt ít nhất một luồng.

> **Tiếp:** [06-them-man-hinh.md](06-them-man-hinh.md) — các bước thêm một màn hình gọi API BE.
