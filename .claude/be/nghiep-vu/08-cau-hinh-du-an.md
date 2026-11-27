# Cấu hình dự án (Construction)

> **Phần 8/22** · trước: [07-du-an.md](07-du-an.md) · mục lục: [README.md](README.md)

Bảng key-value `construction.ProjectConfig` (`Name`, `Value`, `Description`, cột tạo / sửa / xoá mềm). Hiện dùng để lưu tài khoản
Google mà hệ thống dùng tạo, đổi tên, xoá Google Sheet của dự án ([07-du-an.md](07-du-an.md)).

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Danh sách (không phân trang, sắp theo `Name`) | `GET api/construction/project-config` | `Function.Construction.ProjectConfig.View` |
| Xem chi tiết | `GET api/construction/project-config/{id}` | `Function.Construction.ProjectConfig.View` |
| Tạo | `POST api/construction/project-config` | `Function.Construction.ProjectConfig.Create` |
| Sửa | `PUT api/construction/project-config/{id}` | `Function.Construction.ProjectConfig.Update` |
| Xoá | `DELETE api/construction/project-config/{id}` | `Function.Construction.ProjectConfig.Delete` |
| Lấy link xác thực Google | `GET api/construction/project-config/google-authorization-url` | `Function.Construction.ProjectConfig.Update` |
| Google gọi lại sau khi đồng ý / huỷ (302 về màn Cấu hình) | `GET api/construction/project-config/google-callback?code=&state=&error=` | **không cần token** (`[AllowAnonymous]`) |

Body tạo / sửa: `{ "name", "value", "description" }`. Danh sách trả `{ id, name, value, description, createdDate, modifiedDate }`:
`POST` chỉ ghi `createdDate`, nên dòng chưa sửa lần nào có `modifiedDate = null` (FE lấy `modifiedDate ?? createdDate`, Task 50).

## Các dòng cấu hình Google — `ProjectConfigConst`

| `Name` | Giá trị | Bí mật |
|---|---|---|
| `GoogleClientId` | OAuth Client ID (Web application) | |
| `GoogleClientSecret` | OAuth Client secret | Có |
| `GoogleRedirectUri` | URL callback ở trên (có version: `.../api/v1/construction/project-config/google-callback`), **trùng từng ký tự** với Authorized redirect URI trên Google Cloud. Lưu ý: Giá trị cũ không có `v1` giờ trả `404` — sửa cả dòng cấu hình lẫn Google Cloud | |
| `GoogleFolderId` | Id thư mục Drive chứa sheet dự án | |
| `GoogleRefreshToken` | Hệ thống tự ghi khi xác thực xong; cũng nhập tay được | Có |

Hai key hạn dự án, thiếu thì job dùng mặc định ([15-han-du-an.md](15-han-du-an.md#cấu-hình)): `DeadlineWarningDays` (số ngày báo trước,
mặc định `3`), `DeadlineMailEnabled` (`true` / `false`, mặc định `true`).

## Luật nghiệp vụ

- `Name` bắt buộc, trim, **duy nhất** trong các dòng chưa xoá → trùng `409`, rỗng `400`. Xoá là xoá mềm; không tồn tại → `304`.
- Giá trị bí mật **đang có giá trị** trả ra API dạng `******`; rỗng / toàn khoảng trắng thì trả nguyên, để FE biết dòng có mà chưa
  lưu secret / chưa kết nối (`MappingProfile`, Task 50). Audit luôn ghi `******`. Sửa mà gửi `value = "******"` cho dòng bí mật thì **giữ giá trị cũ**
  (`ProjectConfig.Update`), để FE gửi lại form không ghi đè secret.
- Dùng cấu hình: `ProjectConfigDomainService.GetRequiredValuesAsync` — thiếu dòng hoặc `Value` rỗng → `304` "Thiếu cấu hình dự án: <tên>".
- Audit `Tạo / Cập nhật / Xóa cấu hình dự án`, `Xác thực tài khoản Google cho cấu hình dự án`.

## Xác thực Google

1. Người có quyền `Update` gọi `google-authorization-url` → nhận link Google (`access_type=offline`, `prompt=consent`, scope `drive` +
   `spreadsheets`), `state` = Id người gọi đã ký bằng ASP.NET Data Protection, hết hạn sau 10 phút (`AuthorizationStateProtector`).
2. Mở link, đăng nhập Gmail sở hữu thư mục, đồng ý → Google chuyển trình duyệt về `GoogleRedirectUri`.
3. Callback kiểm `state` (sai / hết hạn → `400`), đổi `code` lấy refresh token (`207` nếu Google từ chối), ghi dòng `GoogleRefreshToken`
   (chưa có thì tạo), audit ghi đúng người đã lấy link.
4. Callback không trả JSON mà chuyển trình duyệt (`302`) về màn Cấu hình `<AuthServer:AppUrl>/admin/construction/project-config`
   (Task 50A), để người dùng khỏi thấy trang chữ thô:

| Kết quả | Query thêm vào |
|---|---|
| Thành công | `?google=success` |
| Google gửi `error=access_denied` (bấm Huỷ, so không phân biệt hoa thường): không gọi service, không kiểm `state` vì không ghi gì | `?google=error&message=Bạn đã huỷ cấp quyền Google` |
| Google gửi `error` khác (`invalid_scope`, `server_error`…, Task 54A): log cảnh báo kèm `error`, không gọi service | `?google=error&message=Kết nối Google thất bại, thử lại sau` (không đưa mã lỗi Google lên URL) |
| Lỗi nghiệp vụ (`400` / `207` / `304`) | `?google=error&message=<câu lỗi>` (cắt 200 ký tự rồi mới `EscapeDataString`) |
| Lỗi khác | Log lỗi, `?google=error&message=Kết nối Google thất bại, thử lại sau`, không đưa stack lên URL |

Lưu ý: Callback không cần token nên chỉ được tin `state` đã ký: đừng bỏ bước kiểm `state`, nếu không ai cũng có thể gắn tài khoản Google
của họ vào hệ thống. Khoá Data Protection lưu trong hồ sơ người dùng Windows chạy app; đổi máy / user chạy app thì link cũ hết hiệu lực.

Lưu ý: `AuthServer:AppUrl` chỉ khai trong `appsettings.json` = `http://localhost:4200`. Deploy mà không ghi đè (vd. biến môi trường
`AuthServer__AppUrl`) thì callback đưa người dùng về localhost. Đừng thêm key cấu hình riêng cho callback: dùng chung `AppUrl`.

## Đã kiểm thực tế

Ngày 2026-10-15: tạo 4 dòng Google; trùng tên `409`; tên rỗng `400`; danh sách che `GoogleClientSecret`; sửa bằng `******` giữ đúng secret
trong DB; audit ghi `******`; id không tồn tại `304`; link xác thực đúng `redirect_uri`, `access_type`, `prompt`, scope; callback `state`
giả hoặc thiếu → `400`; `state` thật + `code` giả → Google `invalid_grant` → `207`; tạo dự án khi chưa có refresh token → `304`.
Chưa kiểm đồng ý thật trên trình duyệt (cần người dùng đăng nhập Google).

Ngày 2026-11-23 (Task 50A, API thật, 10/10): mọi ca đều `302` về `/admin/construction/project-config`. Thiếu `state` / `state` giả → câu
`400`; `state` thật + `code` giả → câu `207`; `error=access_denied` (kể cả `state` giả) → "Bạn đã huỷ cấp quyền Google"; xoá tạm dòng
`GoogleClientId` → "Thiếu cấu hình dự án: GoogleClientId", tạo lại đúng giá trị cũ. `GoogleRefreshToken` không đổi. Chưa kiểm đồng ý thật
(`?google=success`).

Ngày 2026-11-24 (Task 50 bước 0, API thật, 11/11): danh sách có `createdDate`; secret có giá trị vẫn `******` ở danh sách và `{id}`; đặt tạm
`GoogleRefreshToken` rỗng / toàn khoảng trắng bằng SQL → trả nguyên; trả lại đúng giá trị cũ (so SHA-256), `modifiedDate` không đổi.
Bước này sửa BE ngoài ranh giới "chỉ 50A" của spec Task 50, người dùng đã đồng ý.

Ngày 2026-11-24 (Task 54A, API thật): `error=access_denied` và `ACCESS_DENIED` → câu huỷ; `error=server_error` → "Kết nối Google thất bại,
thử lại sau", log có `error=server_error`, URL không chứa mã lỗi.

## Giới hạn

Lưu ý: `Value` lưu **dạng rõ** trong DB (kể cả secret, refresh token); chỉ API và audit được che. Ai đọc được DB là dùng được tài khoản Google.

Lưu ý: Đổi tên một dòng cấu hình Google (ví dụ `GoogleFolderId`) làm tạo dự án báo `304` — tên là khoá mà code tra cứu.

> **Tiếp:** [09-mau-chu-ky.md](09-mau-chu-ky.md) — mẫu chữ ký cho lô ký số: cấu hình, ảnh dấu đỏ / chữ ký tay, toạ độ khối.
