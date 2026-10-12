# Nhật ký hệ thống (Core)

> **Phần 4/6** · trước: [03-vai-tro-mac-dinh.md](03-vai-tro-mac-dinh.md) · mục lục: [README.md](README.md)

Mọi thao tác ghi nghiệp vụ để lại một `AuditLog`. Module `Core` chỉ **đọc** nhật ký qua API; việc ghi nằm trong
`BaseService.AddAuditLog` và được gọi từ các service nghiệp vụ.

## Trường của một bản ghi

| Trường | Ý nghĩa |
|---|---|
| `Action` | Tên thao tác bằng tiếng Việt, lấy từ `AuditLogConstants` (ví dụ `Tạo người dùng`) |
| `ActionType` | `Create`, `Update`, `Delete`, `Login` |
| `Content` | Câu mô tả đọc được, ví dụ `Cập nhật vai trò của người dùng admin` |
| `OldData`, `NewData` | JSON của giá trị trước và sau; `null` với thao tác tạo hoặc xoá tương ứng |
| `UserId` | Người thao tác; `null` với thao tác của hệ thống khi khởi động |
| `Source` | Nguồn ghi (thường là tên seeder khi khởi tạo) |
| `CreatedDate` | Giờ Việt Nam tại thời điểm ghi |

## Các thao tác được ghi

| Thao tác | ActionType | Ghi bởi |
|---|---|---|
| Tạo / sửa / xoá người dùng | Create / Update / Delete | `UserService` |
| Đổi mật khẩu | Update | `UserService.ChangePassword` |
| Cập nhật vai trò của người dùng | Update | `UserService.UpdateRoleUser` |
| Tạo / sửa / xoá vai trò | Create / Update / Delete | `RoleService` |
| Cập nhật quyền của vai trò | Update | `RoleService.UpdatePermission` |
| Tạo / cập nhật / xoá loại văn bản | Create / Update / Delete | `DocumentTypeService` |
| Tạo / thêm nhanh / cập nhật / xoá / xoá nhanh trường của loại văn bản | Create / Create / Update / Delete / Delete | `DocumentFieldService` |
| Đăng nhập thành công | Login | `AuthorizationController.Exchange` |
| Đăng nhập thất bại (sai tên hoặc sai mật khẩu) | Login | `AuthorizationController.Exchange` |
| Khởi tạo vai trò / quyền / tài khoản mặc định, đồng bộ danh sách quyền | Create / Update | Các seeder khi khởi động |

## Cách ghi và cùng transaction

- Service gọi `AddAuditLog(...)` **trước** `SaveChangesAsync()`, nên bản ghi audit được lưu cùng lúc với thay đổi nghiệp vụ.
  Nếu transaction rollback thì audit cũng mất, đúng như mong muốn: không có audit cho thao tác không xảy ra.
- Ngoại lệ: đăng nhập thất bại gọi `_auditLogService.Create(...)` riêng, có `SaveChanges` riêng, vì nó xảy ra **khi không có
  transaction nghiệp vụ**. Đây là chủ đích: cần ghi lại cả lần thất bại.
- `AuditLogService.Create` là public trên service nhưng **không có endpoint nào** gọi nó từ ngoài. Chỉ
  `AuthorizationController` gọi nội bộ. Đừng mở endpoint ghi nhật ký từ FE mà không hỏi: nhật ký giả là rủi ro.

⚠️ Đổi mật khẩu: `AddAuditLog` được gọi trước `UserManager.ChangePasswordAsync`, và `UserManager` tự `SaveChanges`.
Audit được lưu cùng lần đó. Đừng thêm `SaveChanges` tay vào luồng này (đã ghi ở
[../architecture/04-infrastructure-data.md](../architecture/04-infrastructure-data.md)).

## Đọc nhật ký

| Endpoint | Quyền | Ghi chú |
|---|---|---|
| `GET api/core/audit-log/paging` | `Function.Core.AuditLog.View` | Phân trang, lọc qua `FindPagingAuditLogDto` |
| `GET api/core/audit-log/{id}` | `Function.Core.AuditLog.View` | `id` kiểu `long`; không có → lỗi `201` |

Nhật ký chỉ được đọc qua API, không có API sửa hay xoá. Nếu cần dọn nhật ký cũ, làm bằng migration/script đã được duyệt,
không thêm endpoint xoá.

## Giới hạn

- Audit ghi `OldData`/`NewData` dạng JSON của DTO ẩn danh tại thời điểm gọi. Thêm trường mới vào entity không tự xuất hiện
  trong audit; phải sửa chỗ gọi `AddAuditLog`.
- Đăng nhập thành công ghi `UserId`; đăng nhập thất bại vì sai tên **không có** `UserId`, nên không lọc được theo người dùng
  cho các lần đoán tên đăng nhập.

> **Tiếp:** [05-loai-van-ban.md](05-loai-van-ban.md) — danh mục loại văn bản của module Construction.
