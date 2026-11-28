# Thêm một màn hình gọi API BE

> **Phần 6/13** · trước: [05-conventions.md](05-conventions.md) · mục lục: [README.md](README.md)

Lưu ý: `PUT api/v1/auth/user` chỉ sửa chính người đang đăng nhập (`UpdateUserDto` không có id), nên màn Người dùng không có nút Sửa.

Mỗi trang đặt tên và breadcrumb bằng `app-page-header` (`shared/components/page-header`), nút chính truyền qua `ng-content`; topbar chỉ hiện ngày.

Lưu ý: Token màu `--app-*` khai ở `:root` từ Task 64, dùng được cả trong dialog gắn vào `body` và khi JS (chart.js) đọc `getComputedStyle`; riêng kích thước sidebar / topbar vẫn ở `.layout-wrapper` ([05](05-conventions.md)).

Lưu ý: `sh-select` phát `valueChanges` ngay lúc khởi tạo, nên lọc danh sách dùng output `(onChange)` để tránh gọi API hai lần.

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
7. **Menu**: thêm mục vào `APP_MENU_ITEMS` ở `app/app.menu.ts`, ẩn/hiện theo `permission` = một key quyền BE (`Menu.Auth.Role`…) hoặc mảng key (có một trong số đó là hiện — `AppMenu.canView`).
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
| `construction` | **Loại / trường văn bản**, **dự án**, cấu hình dự án, **danh sách văn bản**, giao QA | `api/v1/construction/...` |
| `digital-signature` | Mẫu chữ ký, lô ký, plugin | `api/v1/digital-signature/...` |
| `packaging` | **Cấu hình đóng gói TT05** | `api/v1/packaging/...` |
| `operation` | Nhóm nhân viên, thành viên dự án | `api/v1/operation/...` |
| `ai` | Nhà cung cấp AI, **Prompt OCR** (tab trong chi tiết Metadata) | `api/v1/ai/...` (`ai/prompt-template`) |

Route và quyền chi tiết của từng action: controller trong `sh.be/sh.be.api/Controllers/Module/` và nghiệp vụ ở
[../../be/nghiep-vu/README.md](../../be/nghiep-vu/README.md).

Ghi chú riêng của từng màn module `construction` (Metadata, Dự án, Danh sách văn bản): [07-man-construction.md](07-man-construction.md).

**Cấu hình đóng gói** (Task 37, `features/packaging/package-config/`, quyền `Function.Packaging.Config.View` cho cả xem và xuất):
chỉ đọc. Chuẩn và sheet lấy từ `config/dropdown`, không viết cứng, không ghi lên URL. Bảng `pageSize = -1`, không phân trang.
Dải sheet theo loại metadata dính đáy; đổi chuẩn thì giữ sheet cùng `metadataType` và xoá từ khoá. Danh mục mã / Mô tả là ô
cắt 1 dòng + `pTooltip`. Xuất Excel gửi `metadataTypes` lặp lại (`HttpParams`), mỗi loại một sheet. Lệch quy ước: cột Bắt buộc
dùng `STATUS` (p-tag) theo CASE 37.8, không dùng `StatusBadge`. Cột Độ dài kiểu `NUMBER`: null → "—" mờ, 1000 → "1.000" (Task 64). Tên chuẩn BE có đuôi `(mã)`, FE bỏ đuôi khi hiển thị.
Lưu ý: PrimeNG gắn `overflow: auto` **inline** vào `.p-datatable-table-container`, nên CSS ghi đè thành `visible` không ăn. Muốn bảng
cuộn trong khung với tiêu đề dính thì cho chính khung đó giãn flex (`min-height: 0`), đừng đẩy cuộn ra `app-data-table`.

## Checklist

1. Model, route, tên trường copy từ BE, không đoán?
2. Model ở `models/`, service ở `services/`, màn hình ở `features/<module>/<tinh-nang>/`, không import màn hình khác?
3. Component standalone kế thừa `BaseComponent`, dùng `DataTable`, nút ghi có `submitting`, route gắn vào `<MODULE>_ROUTES`?
4. Không tự toast lỗi HTTP, không tự gắn token? Menu ẩn/hiện đúng key quyền?
5. Build sạch, đã thử với BE thật? Tài liệu `.claude/fe/` nào cần cập nhật?

> **Tiếp:** [07-man-construction.md](07-man-construction.md) — ghi chú từng màn module `construction`.
