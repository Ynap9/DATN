# Vai trò và quyền mặc định

> **Phần 3/14** · trước: [02-phan-quyen.md](02-phan-quyen.md) · mục lục: [README.md](README.md)

## Vai trò mặc định

Hằng số trong `RoleConstants`; `SeedRole` tạo các vai trò còn thiếu khi khởi động (idempotent).

| Hằng | Tên trong DB | Tên hiển thị | Quyền khi khởi tạo |
|---|---|---|---|
| `ROLE_SUPER_ADMIN` | `SuperAdmin` | Super Administrator | Mọi quyền (bỏ qua kiểm quyền khi gọi API) |
| `ROLE_ADMIN` | `Admin` | Admin | Mọi quyền (`SeedRolePermission`) |
| `ROLE_LEADER` | `Leader` | Quản lý | Chưa có |
| `ROLE_DATA` | `Data` | Nhân viên nhập liệu | Chưa có |
| `ROLE_QA` | `QA` | Nhân viên QA | Chưa có |
| `ROLE_USER` | `User` | Người dùng | Chưa có |

Bốn vai trò `Leader`, `Data`, `QA`, `User` chưa có quyền nào sau khi seed. Muốn dùng phải gán quyền bằng API
`PUT api/auth/role/permission`.

## Tài khoản khởi tạo

`SeedUser` tạo khi chưa có:

- Vai trò `SuperAdmin`;
- tài khoản `admin` (email `admin@example.com`), gán vai trò `SuperAdmin`.

⚠️ **Mật khẩu của `admin` được ghi cứng trong `SeedUser`** (`"123456Aa@"`). Seeder chỉ tạo user khi chưa có, nên đổi
mật khẩu qua API không ảnh hưởng tới seed; nhưng mật khẩu mặc định vẫn nằm trong source. Trên môi trường thật phải đổi
ngay sau khi triển khai. Đừng xoá dòng tạo user để "sạch code" — seeder sẽ không có tài khoản quản trị nào để đăng nhập.

## Tạo vai trò

- Id do **client gửi lên** (`CreateRoleDto.Id`), không tự sinh. Hệ thống chỉ kiểm tra Id đã tồn tại chưa (`EnsureNotExists`) → lỗi `409`.
- Tên vai trò **không được kiểm trùng**. Hai vai trò khác Id có thể cùng tên, và `GetRoleNames` trả về cả hai.
- `Role.Create` luôn gán `IsSystem = 1`, nên cờ này không phân biệt được vai trò hệ thống và vai trò người dùng tạo.

⚠️ Hai điểm trên là hệ quả của thiết kế hiện tại. Sửa (kiểm trùng tên, sinh Id tự động) là thay đổi schema hoặc
contract với FE — hỏi trước.

## Sửa vai trò

Chỉ đổi `Name` và `Description`. Quyền của vai trò đổi qua API riêng `PUT api/auth/role/permission`.

## Gán quyền cho vai trò

`PUT api/auth/role/permission` nhận cây `UpdateRolePermissionDto`:

1. Lấy các node có `IsChecked`, khử trùng lặp.
2. Lấy toàn bộ quyền đang hoạt động, chạy `Role.AssignPermissions` (qua `PermissionGrantPolicy`) để ra tập key cuối.
3. Trong transaction: xoá hết `RolePermission` cũ của vai trò, thêm tập mới, ghi audit với danh sách key cũ và mới.

Xem luật chọn key tại [02-phan-quyen.md](02-phan-quyen.md#luật-chọn-quyền-cho-vai-trò).

## Xoá vai trò

- Xoá mềm: `Role.Delete` đặt `Deleted = true`. Global query filter ẩn vai trò khỏi mọi truy vấn, kể cả `GetRoleNames`,
  nên user đang giữ vai trò đó **mất vai trò ngay** mà không cần xoá `UserRole`.
- Không kiểm tra vai trò còn đang gán cho user nào, và không chặn xoá vai trò `SuperAdmin`.

⚠️ Xoá `SuperAdmin` bằng API là có thể làm mất toàn bộ quyền quản trị, và không có đường lấy lại ngoài sửa DB tay.
Đây là chỗ nên thêm kiểm tra trong `RoleDomainService` trước khi mở API này rộng hơn.

## Đọc cây quyền của vai trò

`GET api/auth/role/{id}/tree` trả toàn bộ cây quyền đang hoạt động, mỗi node có `IsChecked` theo quyền hiện tại của vai trò.
FE dùng cây này để render form gán quyền; `Key` của node là giá trị gửi lại khi lưu.

> **Tiếp:** [04-nhat-ky-he-thong.md](04-nhat-ky-he-thong.md) — nhật ký các thao tác trên.
