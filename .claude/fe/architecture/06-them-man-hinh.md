# Thêm một màn hình gọi API BE

> **Phần 6/6** · trước: [05-conventions.md](05-conventions.md) · mục lục: [README.md](README.md)

⚠️ Làm **sau** khi plan [../plans/khop-voi-be.plan.md](../plans/khop-voi-be.plan.md) xong ít nhất bước envelope
và đăng nhập; trước đó mọi response BE đều bị đọc là lỗi.

Ví dụ dưới: màn hình danh sách vai trò, gọi `GET api/v1/auth/role/paging` của `RoleController`.

## Các bước

1. **Đối chiếu BE**: mở controller (route, `[Permission]`), DTO vào/ra trong
   `sh.be.applications/Module/<M>/Dtos/`. Không đoán tên trường.
2. **Model**: `admin/src/app/modules/auth/role/models/role.model.ts`, trường camelCase đúng JSON BE.
3. **Service**: `.../role/services/role.service.ts`, mỗi endpoint một method trả `Observable`.
4. **Component trang**: kế thừa `HComponentBase`, dùng `h-table` + `h-input-search`; gọi service, kiểm
   `checkStatusResponse`, đổ `data.items` vào bảng, `data.totalItems` vào `page.totalItems`.
5. **Module + routing**: `role.module.ts`, `role-routing.module.ts`; gắn lazy vào `AdminRoutingModule`.
6. **Menu**: thêm mục vào `AppMenu`, ẩn/hiện theo key quyền BE (`Menu.Auth.Role`…).
7. **Build** (xem [05-conventions.md](05-conventions.md)), chạy BE, thử trên trình duyệt.

## Mẫu service

```typescript
@Injectable({ providedIn: 'root' })
export class RoleService {
    private readonly baseUrl = `${environment.api}/api/v1/auth/role`;

    constructor(private _http: HttpClient) {}

    findPaging(page: Page): Observable<IApiResponse<IPaging<IRole>>> {
        const params = new HttpParams()
            .set('pageNumber', page.getPageNumber())
            .set('pageSize', page.getPageSize())
            .set('keyword', page.keyword);
        return this._http.get<IApiResponse<IPaging<IRole>>>(`${this.baseUrl}/paging`, { params });
    }
}
```

`IApiResponse<T>` và `IPaging<T>` là kiểu khớp BE (`{ status, data, code, message }`, `{ items, totalItems }`)
sẽ được thêm vào `lib-components/.../shared/interfaces/response.interface.ts` theo plan khớp BE.

## Bảng route BE hiện có

| Màn hình | Route BE | Quyền |
|---|---|---|
| Người dùng | `api/v1/auth/user`: `GET me`, `GET paging`, `POST`, `PUT`, `DELETE {userid}`, `PUT change-password`, `PUT role` | `Function.Auth.User.*` |
| Vai trò | `api/v1/auth/role`: `GET paging`, `GET dropdown`, `GET {id}`, `GET {id}/tree`, `POST`, `PUT {id}`, `DELETE {id}`, `PUT permission` | `Function.Auth.Role.*` |
| Quyền | `api/v1/auth/permission` | `Function.Auth.Permission.View` |
| Nhật ký | `api/v1/core/audit-log/paging`, `{id}` | `Function.Core.AuditLog.View` |

Route chi tiết của từng action: xem controller trong `sh.be/sh.be.api/Controllers/Module/`.

## Checklist

1. Model, route, tên trường copy từ BE, không đoán?
2. Component kế thừa `HComponentBase`, khai báo trong module lazy, `standalone: false`?
3. Đọc response bằng `checkStatusResponse`, không tự toast lỗi HTTP?
4. Menu ẩn/hiện đúng key quyền?
5. Build sạch, đã thử với BE thật? Tài liệu `.claude/fe/` nào cần cập nhật?
