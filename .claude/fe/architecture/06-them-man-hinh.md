# Thêm một màn hình gọi API BE

> **Phần 6/6** · trước: [05-conventions.md](05-conventions.md) · mục lục: [README.md](README.md)

⚠️ `PUT api/v1/auth/user` chỉ sửa chính người đang đăng nhập (`UpdateUserDto` không có id), nên màn Người dùng không có nút Sửa.

Mỗi trang đặt tên và breadcrumb bằng `app-page-header` (`shared/components/page-header`), nút chính truyền qua `ng-content`; topbar chỉ hiện ngày.

⚠️ Biến `--app-*` khai báo trên `.layout-wrapper`, còn dialog gắn vào `body` nên không nhận được: style trong dialog dùng token PrimeNG (`--p-text-color`, `--p-content-border-color`).

⚠️ `sh-select` phát `valueChanges` ngay lúc khởi tạo, nên lọc danh sách dùng output `(onChange)` để tránh gọi API hai lần.

Ví dụ dưới: màn hình danh sách vai trò, gọi `GET api/v1/auth/role/paging` của `RoleController`.

## Các bước

1. **Đối chiếu BE**: mở controller (route, `[Permission]`), DTO vào/ra trong
   `sh.be.applications/Module/<M>/Dtos/`. Không đoán tên trường.
2. **Thư mục màn hình**: `features/auth/role/`.
3. **Model**: `models/role.model.ts`, trường camelCase đúng JSON BE.
4. **Service**: `services/role.service.ts`, mỗi endpoint một method trả `Observable`.
5. **Component trang**: `role-list.component.ts` kế thừa `BaseComponent`, dùng `SharedImports`, `DataTable`; form tạo/sửa mở bằng `_dialogService.open`.
6. **Route**: thêm `{ path: 'role', component: RoleListComponent }` vào `AUTH_ROUTES` trong
   `features/auth/auth.routes.ts` ⇒ URL `/admin/auth/role`.
7. **Menu**: thêm mục vào `APP_MENU_ITEMS` ở `app/app.menu.ts`, ẩn/hiện theo key quyền BE (`Menu.Auth.Role`…).
8. **Build** (xem [05-conventions.md](05-conventions.md)), chạy BE, thử trên trình duyệt.

```text
models/role.model.ts
services/role.service.ts
features/auth/
  auth.routes.ts
  role/             role-list.component.ts|html  role-form/
```

## Mẫu service

```typescript
@Injectable({ providedIn: 'root' })
export class RoleService {
    api = apiUrl('auth/role');
    http = inject(HttpClient);

    findPaging(query: IFindPagingRole) {
        return this.http.get<IBaseResponsePaging<IViewRole>>(`${this.api}/paging`, { params: { ...query } });
    }
}
```

## Bảng route BE hiện có

| Module FE | Màn hình (**đậm** = FE đã có) | Route BE |
|---|---|---|
| `auth` | **Người dùng**, **vai trò**, quyền | `api/v1/auth/user`, `.../role`, `.../permission` |
| `core` | **Nhật ký**, job nền | `api/v1/core/audit-log`, `.../background-job` |
| `construction` | **Loại / trường văn bản**, dự án, cấu hình dự án, hồ sơ, giao QA | `api/v1/construction/...` |
| `digital-signature` | Mẫu chữ ký, lô ký, plugin | `api/v1/digital-signature/...` |
| `packaging` | Cấu hình đóng gói TT05 | `api/v1/packaging/...` |
| `operation` | Nhóm nhân viên, thành viên dự án | `api/v1/operation/...` |
| `ai` | Nhà cung cấp AI | `api/v1/ai/...` |

Route và quyền chi tiết của từng action: controller trong `sh.be/sh.be.api/Controllers/Module/` và nghiệp vụ ở
[../../be/nghiep-vu/README.md](../../be/nghiep-vu/README.md).

Loại / trường văn bản, giao diện gọi là **Metadata**, nằm trong mục menu **Cấu hình dự án**
(`/admin/construction/project-config` → `.../metadata`, `.../metadata/:id`; hằng `METADATA_URL`). Menu không có mục "Hồ sơ".

- Route nhận `Menu.Construction.DocumentType` **hoặc** `Function.Construction.DocumentType.View`; menu theo View, vì tích một Function con
  không cấp key Menu cha.
- Form trường không có ô mã: FE gửi `field = name` (mã trường BE không dùng ở luồng nào); tên cột import để trống thì gửi bằng tên.
- Kéo thả gửi `targetOrder` = thứ tự đang đứng ở vị trí thả; tắt khi đang tìm kiếm vì danh sách lọc không liền thứ tự.

## Checklist

1. Model, route, tên trường copy từ BE, không đoán?
2. Model ở `models/`, service ở `services/`, màn hình ở `features/<module>/<tinh-nang>/`, không import màn hình khác?
3. Component standalone kế thừa `BaseComponent`, dùng `DataTable`, nút ghi có `submitting`, route gắn vào `<MODULE>_ROUTES`?
4. Không tự toast lỗi HTTP, không tự gắn token? Menu ẩn/hiện đúng key quyền?
5. Build sạch, đã thử với BE thật? Tài liệu `.claude/fe/` nào cần cập nhật?
