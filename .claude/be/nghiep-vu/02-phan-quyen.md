# Phân quyền

> **Phần 2/8** · trước: [01-nguoi-dung.md](01-nguoi-dung.md) · mục lục: [README.md](README.md)

Quyền là một **cây** định danh bằng `Key`. Vai trò giữ một tập key; user có quyền = hợp quyền của các vai trò còn hoạt động.
Hằng số key nằm ở `PermissionConstants` (shared); cây được seed bởi `SeedPermission`.

## Cây quyền hiện tại

```text
Menu.Auth                          Menu quản trị hệ thống
├── Menu.Auth.User                 Menu quản lý người dùng
│   ├── Function.Auth.User.View
│   ├── Function.Auth.User.Create
│   ├── Function.Auth.User.Update
│   └── Function.Auth.User.Delete
├── Menu.Auth.Role                 Menu quản lý vai trò
│   ├── Function.Auth.Role.View
│   ├── Function.Auth.Role.Create
│   ├── Function.Auth.Role.Update
│   ├── Function.Auth.Role.Delete
│   └── Function.Auth.Role.AssignPermission
└── Menu.Auth.Permission           Menu quản lý quyền
    └── Function.Auth.Permission.View

Menu.Core                          Menu hệ thống
└── Menu.Core.AuditLog             Menu nhật ký hệ thống
    └── Function.Core.AuditLog.View

Menu.Construction                  Menu nghiệp vụ xây dựng
├── Menu.Construction.DocumentType Menu quản lý loại văn bản
│   ├── Function.Construction.DocumentType.View
│   ├── Function.Construction.DocumentType.Create
│   ├── Function.Construction.DocumentType.Update
│   └── Function.Construction.DocumentType.Delete
├── Menu.Construction.DocumentField Menu quản lý trường của loại văn bản
│   ├── Function.Construction.DocumentField.View
│   ├── Function.Construction.DocumentField.Create
│   ├── Function.Construction.DocumentField.Update
│   └── Function.Construction.DocumentField.Delete
├── Menu.Construction.Project      Menu quản lý dự án
│   ├── Function.Construction.Project.View
│   ├── Function.Construction.Project.Create
│   ├── Function.Construction.Project.Update
│   └── Function.Construction.Project.Delete
└── Menu.Construction.ProjectConfig Menu cấu hình dự án
    ├── Function.Construction.ProjectConfig.View
    ├── Function.Construction.ProjectConfig.Create
    ├── Function.Construction.ProjectConfig.Update   (gồm lấy link xác thực Google)
    └── Function.Construction.ProjectConfig.Delete
```

Quy ước đặt key: `Menu.<Module>.<Tên>` cho mục menu, `Function.<Module>.<Tên>.<Hành động>` cho thao tác.

## Luật chọn quyền cho vai trò

`PermissionGrantPolicy.ResolveGrantedKeys` nhận danh sách key được tick và trả về tập key thực sự được cấp:

- Trạng thái tick được đồng bộ qua cây bằng `TreeUtils.SyncChecked`, nên chọn cha và con theo cùng một quy tắc lan truyền.
- Kết quả là tập key đã chọn; `Role.AssignPermissions` chỉ tạo `RolePermission` cho các key trong tập này.
- Hàm này là hàm thuần (không I/O), có thể kiểm thử độc lập.

## Luật kiểm quyền khi gọi API

`[Permission(key1, key2, ...)]` trên action:

1. Chưa đăng nhập → `401`.
2. Vai trò có `SuperAdmin` → qua, không xét quyền (`PermissionGrantPolicy.IsSuperAdmin`).
3. Còn lại: user có **ít nhất một** key trong danh sách → qua (`HasAnyPermission`); không có → `403`.

Chi tiết cơ chế attribute và cách truy vấn DB: xem [../architecture/01-api.md](../architecture/01-api.md).

## Bảng endpoint → key yêu cầu

| Endpoint | Key yêu cầu |
|---|---|
| `GET api/auth/user/paging` | `Menu.Auth` |
| `GET api/auth/user/me` | — (chỉ cần đăng nhập) |
| `POST api/auth/user` | `Menu.Auth` |
| `PUT api/auth/user` | `Menu.Auth` |
| `DELETE api/auth/user/{userid}` | `Menu.Auth` |
| `PUT api/auth/user/role` | `Menu.Auth` |
| `PUT api/auth/user/change-password` | — (chỉ cần đăng nhập) |
| `GET api/auth/role/paging` | `Menu.Auth` |
| `GET api/auth/role/dropdown` | `Function.Auth.Role.View` |
| `GET api/auth/role/{id}` | `Function.Auth.Role.View` |
| `GET api/auth/role/{id}/tree` | `Function.Auth.Role.View` |
| `POST api/auth/role` | `Function.Auth.Role.Create` |
| `PUT api/auth/role/{id}` | `Function.Auth.Role.Update` |
| `DELETE api/auth/role/{id}` | `Function.Auth.Role.Delete` |
| `PUT api/auth/role/permission` | `Function.Auth.Role.AssignPermission` |
| `GET api/auth/permission` | `Menu.Auth` |
| `GET api/core/audit-log/paging` | `Function.Core.AuditLog.View` |
| `GET api/core/audit-log/{id}` | `Function.Core.AuditLog.View` |
| `GET api/construction/document-type/paging`, `GET .../{id}`, `GET .../{id}/document-fields` | `Function.Construction.DocumentType.View` |
| `POST api/construction/document-type` | `Function.Construction.DocumentType.Create` |
| `PUT api/construction/document-type/{id}` | `Function.Construction.DocumentType.Update` |
| `DELETE api/construction/document-type/{id}` | `Function.Construction.DocumentType.Delete` |
| `GET api/construction/document-field/paging`, `GET .../dropdown`, `GET .../{id}` | `Function.Construction.DocumentField.View` |
| `POST api/construction/document-field`, `POST .../quick-create`, `GET .../sheet-header` | `Function.Construction.DocumentField.Create` |
| `PUT api/construction/document-field/{id}`, `PUT .../{id}/setting`, `PUT .../{id}/order-table-view`, `PUT .../{id}/order-list-view` | `Function.Construction.DocumentField.Update` |
| `DELETE api/construction/document-field/{id}`, `DELETE .../quick-delete` | `Function.Construction.DocumentField.Delete` |
| `GET api/construction/project/{id}` | `Function.Construction.Project.View` |
| `POST api/construction/project` | `Function.Construction.Project.Create` |
| `PUT api/construction/project/{id}` | `Function.Construction.Project.Update` |
| `DELETE api/construction/project/{id}` | `Function.Construction.Project.Delete` |
| `GET api/construction/project-config`, `GET .../{id}` | `Function.Construction.ProjectConfig.View` |
| `POST` / `PUT {id}` / `DELETE {id}` `api/construction/project-config` | `...ProjectConfig.Create` / `.Update` / `.Delete` |
| `GET api/construction/project-config/google-authorization-url` | `Function.Construction.ProjectConfig.Update` |
| `GET api/construction/project-config/google-callback` | — (không cần token, kiểm `state` đã ký) |

## Quyền người dùng: đang lệch so với cây

⚠️ **Bốn key `Function.Auth.User.*` đã được seed nhưng không controller nào dùng.** Toàn bộ thao tác user (tạo, sửa, xoá,
gán vai trò, danh sách) đang kiểm bằng `Menu.Auth`. Hệ quả: ai có `Menu.Auth` (ví dụ vai trò `Admin`) làm được mọi thao tác
user, kể cả xoá, và không thể tách quyền xem / tạo / xoá riêng.

Cách sửa là đổi key trong `[Permission]` sang `Function.Auth.User.*`. Đây là **đổi hành vi phân quyền**, phải hỏi trước, và
đi kèm việc gán các key mới cho vai trò đang có: `SeedRolePermission` chỉ cấp quyền đầy đủ cho `SuperAdmin` và `Admin` khi
khởi tạo, các vai trò khác phải gán tay.

## Đừng đổi tên key đã seed

⚠️ `SeedPermission` chạy mỗi lần khởi động: key nào **không có** trong `DefaultPermissions` sẽ bị xoá cứng, **kèm mọi
`RolePermission` gắn key đó**. Đổi tên một hằng trong `PermissionConstants` vì vậy làm mất quyền đã gán cho vai trò,
không báo lỗi gì. Muốn đổi tên: thêm key mới, gán lại cho vai trò, rồi mới bỏ key cũ khỏi `DefaultPermissions` — và
nên hỏi trước vì đây là thay đổi dữ liệu đang chạy.

> **Tiếp:** [03-vai-tro-mac-dinh.md](03-vai-tro-mac-dinh.md) — các vai trò và luật tạo / xoá vai trò.
