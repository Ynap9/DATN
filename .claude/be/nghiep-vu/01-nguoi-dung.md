# Người dùng (Auth)

> **Phần 1/15** · mục lục: [README.md](README.md)

Quản lý tài khoản đăng nhập. Logic nằm ở `UserService` (applications) và `UserDomainService` (domain); ghi xuống
DB qua `UserRepository` (dùng `UserManager<AppUser>`).

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu | Ghi chú |
|---|---|---|---|
| Danh sách có phân trang | `GET api/auth/user/paging` | `Menu.Auth` | Qua `IUserQuery`, trả DTO |
| Xem thông tin của tôi | `GET api/auth/user/me` | đăng nhập | Lấy theo user trong token |
| Tạo người dùng | `POST api/auth/user` | `Menu.Auth` | Gán vai trò và gửi mail thông tin đăng nhập |
| Sửa hồ sơ | `PUT api/auth/user` | `Menu.Auth` | Chỉ sửa **chính người đang đăng nhập**; không nhận `Id` |
| Xoá người dùng | `DELETE api/auth/user/{userid}` | `Menu.Auth` | Xoá mềm user và các `UserRole` của user đó |
| Đổi mật khẩu | `PUT api/auth/user/change-password` | đăng nhập | Chỉ đổi cho chính mình |
| Gán vai trò | `PUT api/auth/user/role` | `Menu.Auth` | Thay **toàn bộ** vai trò của một user |

## Tạo người dùng

1. Mật khẩu: nếu `IsRandomPassword` thì sinh chuỗi 8 ký tự bằng `CryptoUtils`; nếu không thì kiểm mật khẩu và xác nhận khớp nhau.
2. Tên đăng nhập và email phải chưa tồn tại → nếu trùng thì lỗi `409`.
3. Danh sách vai trò được khử trùng lặp. **Phải có ít nhất một vai trò**, và mọi Id phải là vai trò còn hoạt động; thiếu thì lỗi `105`.
4. Trong transaction: tạo user → gán vai trò → ghi audit → gửi mail → commit. Lỗi ở bất kỳ bước nào thì rollback.

## Sửa hồ sơ

- Chỉ các trường không rỗng mới được cập nhật; trường rỗng giữ nguyên giá trị cũ.
- Tên đăng nhập hoặc email mới không được trùng với user **khác** (so sánh với giá trị hiện tại trước).
- Audit lưu cả giá trị cũ và mới.

## Đổi mật khẩu

1. Mật khẩu mới phải hợp lệ và khớp xác nhận.
2. Mật khẩu hiện tại phải đúng → lỗi `101` nếu sai.
3. Mật khẩu mới phải khác mật khẩu hiện tại → lỗi `400`.
4. Đổi qua `UserManager`, nên `SecurityStamp` đổi theo và token cũ không còn đáng tin.

## Gán vai trò

- Xoá toàn bộ `UserRole` cũ của user rồi thêm danh sách mới (đã khử trùng lặp) trong một transaction.
- Audit ghi `RoleIds` trước và sau khi đổi.
- Quy trình này không làm gì với quyền trực tiếp; quyền của user luôn suy ra từ vai trò (xem [02-phan-quyen.md](02-phan-quyen.md)).

## Đăng nhập lấy token

Đi qua `POST /connect/token` (`AuthorizationController.Exchange`), grant `password`:

1. Tìm user theo tên đăng nhập. Không có → ghi audit "Đăng nhập thất bại" (không có `UserId`) và trả `404` "Tài khoản không tồn tại".
2. Sai mật khẩu → ghi audit "Đăng nhập thất bại" có `UserId`, trả `101`.
3. Đúng → token có claim `sub` = Id, `name` = họ tên, `username`, `SecurityStamp`, và một claim `role` cho mỗi vai trò còn hoạt động.
4. Ghi audit "Đăng nhập" thành công.

Các grant khác (`refresh_token`, `client_credentials`, `authorization_code`) và cấu hình OpenIddict: xem [../architecture/01-api.md](../architecture/01-api.md).

## Giới hạn hiện tại

⚠️ **Không chặn xoá chính mình, cũng không chặn xoá SuperAdmin cuối cùng.** `UserService.Delete` không kiểm tra
người gọi hay vai trò của user bị xoá, nên một admin có thể tự khoá mình khỏi hệ thống. Cách tránh: thêm kiểm tra vào
domain (`UserDomainService`), đừng chỉ chặn ở FE.

⚠️ **Không có API sửa hồ sơ người khác.** `UpdateUserDto` không có `Id`; `Update` luôn lấy user từ token. Muốn admin sửa
user khác phải thêm endpoint có `Id` và đổi DTO — đổi DTO là đổi contract với FE, hỏi trước.

⚠️ **Mật khẩu được gửi qua email dạng rõ.** `SendAccountCreatedAsync` nhận mật khẩu gốc để đưa vào mail. Đây là hành vi
hiện tại, không phải lỗi cú pháp; đừng "sửa" bằng cách bỏ mail mà không hỏi lại luồng cấp tài khoản.

⚠️ **Thông báo đăng nhập tiết lộ tên đăng nhập có tồn tại hay không.** Trả `404` riêng cho tên sai và `101` cho mật khẩu sai
cho phép đoán tên hợp lệ. Đổi mã lỗi là đổi contract FE; hỏi trước.

⚠️ **Chưa thấy cơ chế khoá tài khoản khi sai mật khẩu nhiều lần.** `Exchange` không đếm lần sai, và `Program.cs` không
có cấu hình lockout (đã tìm, không thấy). Đây là ghi nhận từ đọc code, chưa kiểm thử tấn công thực tế.

⚠️ Mail gửi **trước** `CommitAsync` trong `Create` — xem bẫy đã ghi trong [../architecture/02-applications.md](../architecture/02-applications.md).

> **Tiếp:** [02-phan-quyen.md](02-phan-quyen.md) — cây quyền và luật kiểm quyền.
