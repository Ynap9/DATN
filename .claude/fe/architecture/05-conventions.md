# Quy ước code bắt buộc

> **Phần 5/6** · trước: [04-http-auth.md](04-http-auth.md) · mục lục: [README.md](README.md)

Áp dụng cho mọi thay đổi trong `sh.fe`. Luật chung (chỉ sửa chỗ cần thiết, không comment, không commit khi
chưa yêu cầu) giống BE — xem [../../be/architecture/07-conventions.md](../../be/architecture/07-conventions.md).

## NgModule

- Component, directive, pipe: `standalone: false`, khai báo trong **đúng một** NgModule.
- Mỗi tính năng một module lazy: `<ten>.module.ts` + `<ten>-routing.module.ts` (`RouterModule.forChild`).
- Module tính năng import `CommonModule`, `FormsModule`, `ReactiveFormsModule`, `MyLibComponentsModule`
  (không `forRoot`) và module PrimeNG nó dùng.
- Provider toàn cục (interceptor, `MessageService`, `DialogService`, `ConfirmationService`) chỉ ở `AppModule`.

## Đặt đúng chỗ

| Muốn viết | Đặt ở |
|---|---|
| Màn hình của một tính năng | `admin/src/app/modules/<module-be>/<tinh-nang>/` |
| Service gọi API của tính năng | `.../<tinh-nang>/services/<tinh-nang>.service.ts` |
| Kiểu DTO khớp BE | `.../<tinh-nang>/models/` (tên trường **camelCase đúng như JSON BE trả**) |
| Component dùng lại ở nhiều tính năng | `lib-components/.../components/` + export ở `public-api.ts` |
| Hằng số dùng chung, enum | `lib-components/.../shared/const/` |
| Validator form | `projects/shared/src/validators/validator-common.ts` |
| Layout, menu, topbar | `admin/src/app/core/layout/` |

Thư mục module FE theo **module BE** (`auth`, `core`) để một chức năng tra được hai đầu cùng tên.

## Tên

- File kebab-case: `user-list.component.ts`; class PascalCase + hậu tố (`UserListComponent`, `UserService`).
- Thư mục `core/layout/` đang dùng kiểu Angular mới (`app-topbar.ts`, class `AppTopbar`): giữ nguyên ở đó,
  code mới dùng kiểu có hậu tố như trên.
- Selector: `app-` cho component của admin, `h-` cho component thư viện.
- Field private của class: `_camelCase` cho service inject (`_userService`), như code sẵn có.

## Gọi API

- Service `@Injectable({ providedIn: 'root' })`, base URL từ `environment.api`, route **copy đúng** từ
  controller BE, có version (`api/v1/auth/role/paging`…; `/connect/token` không có version).
- Service trả `Observable`; component subscribe kèm `takeUntil(this.destroyed$)` hoặc dùng `confirmAction` /
  `awaitApi` của helper.
- Đọc kết quả bằng `checkStatusResponse(res)` trước khi dùng `res.data`.
- Không tự gắn header `Authorization`; không tự toast lỗi HTTP (interceptor đã làm).

## Form và giao diện

- Form reactive (`FormBuilder`), kiểm bằng `checkInValidForm` trước khi gửi.
- Dùng component `h-*` cho input, bảng, nút trước khi tự viết bằng PrimeNG thô.
- Style: Tailwind cho bố cục; SCSS chung ở `projects/shared/assets/layout`; style riêng component dưới 8 kB.
- Thông điệp người dùng tiếng Việt, lấy từ `SuccessMessage` / `ErrorMessage` khi có sẵn.

## Kiểm trước khi báo xong

```bash
npx ng build admin --configuration development
npm run build:admin
```

Build sạch, không vượt budget. Tính năng gọi API thì chạy `sh.be.api` và thử trên trình duyệt ít nhất một luồng.

> **Tiếp:** [06-them-man-hinh.md](06-them-man-hinh.md) — các bước thêm một màn hình.
