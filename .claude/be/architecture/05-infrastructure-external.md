# Tầng Infrastructure External — sh.be.infrastructure.external

> **Phần 5/8** · trước: [04-infrastructure-data.md](04-infrastructure-data.md) · mục lục: [README.md](README.md)

Dịch vụ bên thứ ba không phải database. Chỉ tham chiếu `sh.be.applications` để implement cổng ra mà
application khai báo.

## Thư mục

| Thư mục | Chứa gì |
|---|---|
| `Mail/Implements/MailService.cs` | Implement `IMailService`: gửi SMTP bằng cấu hình `EmailSettings` |
| `Mail/Templates/AccountCreatedMailTemplate.cs` | HTML email báo tạo tài khoản |

## IMailService

Interface nằm ở `sh.be.applications/Base/Interfaces/IMailService.cs`:

| Method | Dùng khi |
|---|---|
| `SendAsync(to, subject, body)` | Gửi email tuỳ ý |
| `SendAccountCreatedAsync(to, userName, password)` | Tạo user xong, gửi thông tin đăng nhập |

**Vì sao interface ở application, không ở external:** application chỉ nói "gửi mail báo tạo tài khoản"; tiêu
đề (`MailTemplateConstants`) và HTML là chi tiết của tầng ngoài. Sửa giao diện email không chạm `UserService`.

## Cấu hình

Section `Email` trong `appsettings.json`, bind vào `EmailSettings` (`sh.be.shared/Settings`):
`Smtp.Host`, `Smtp.Port`, `Smtp.UserName`, `Smtp.Password`, `Smtp.EnableSsl`, `DefaultFromAddress`,
`DefaultFromDisplayName`.

## Thêm một dịch vụ ngoài

1. Khai interface trong `sh.be.applications/Base/Interfaces/` (dùng chung) hoặc
   `Module/<TenModule>/Interfaces/` (riêng module), method mang nghĩa nghiệp vụ.
2. Implement trong `sh.be.infrastructure.external/<TenDichVu>/Implements/`.
3. Settings (nếu có) đặt ở `sh.be.shared/Settings/`, bind trong `Program.cs`.
4. Đăng ký DI trong `Program.cs`.

**Không** cho external tham chiếu `domain` hay `infrastructure.data`; **không** để application tham chiếu
external.

⚠️ Gửi mail đang chạy bên trong transaction của `UserService.Create` (xem
[02-applications.md](02-applications.md)). Dịch vụ ngoài mới đừng gọi bên trong transaction DB nếu tránh được.

> **Tiếp:** [06-shared.md](06-shared.md) — shared kernel dùng chung.
