# Plan: khớp sh.fe với hợp đồng sh.be

> **Trạng thái:** 📝 chờ duyệt — ghi nhận 2026-10-11, cập nhật 2026-10-28 sau khi dựng lại kiến trúc FE (mục 10,
> 11, 12 đã xong). BE là nguồn chân lý; FE sửa theo BE. Hợp đồng BE:
> [../../be/architecture/01-api.md](../../be/architecture/01-api.md), [../../be/architecture/06-shared.md](../../be/architecture/06-shared.md).

## Input

- BE chạy được, `POST /connect/token` (password grant) và các API đọc đã kiểm ngày 2026-10-11.
- FE đã phân tầng `core` / `shared` / `features` + UI kit `@sh/ui` ([../architecture/README.md](../architecture/README.md)),
  build sạch, chưa có màn hình nghiệp vụ, đăng nhập chưa gọi API.

## Chỗ lệch

| # | Mặt | BE | FE hiện tại | Ảnh hưởng |
|---|---|---|---|---|
| 1 | Envelope | `{ status (1/0), data, code, message }` | Chưa có kiểu, chưa có hàm đọc (hàm đọc `statusCode` cũ đã bỏ) | Chưa đọc được response |
| 2 | Phân trang (ra) | `data.items`, `data.totalItems` | Chưa có kiểu | Chưa đổ được bảng |
| 3 | Phân trang (vào) | `pageSize = -1` là lấy tất cả | `Page.pageSizeAll = 9999999` | Chạy được nhưng sai quy ước |
| 4 | Đăng nhập | `POST /connect/token`, form-urlencoded, `grant_type=password`, `client_id`; sai ⇒ HTTP 400 `invalid_grant` | `LoginComponent.onSubmit` không gọi API | Không đăng nhập được |
| 5 | Client OAuth | `client-web2` (public, không secret) | `environment` không có `clientId` | Thiếu cấu hình |
| 6 | Refresh token | Grant `refresh_token`, FE gửi `scope=openid offline_access` | Hết hạn ⇒ về login | Phải đăng nhập lại |
| 7 | User hiện tại | `GET api/v1/auth/user/me` → `ViewMeDto` (`id`, `userName`, `fullName`, `email`, `roles[].permissions[]`) | `ICurrentUser` đủ 4 trường đầu, chưa ai gọi `setCurrentUser` | Không có thông tin user |
| 8 | Quyền / menu | Key `Menu.*`, `Function.*` trong `PermissionConstants` | Menu rỗng, chưa có hằng quyền phía FE | Chưa ẩn/hiện theo quyền |
| 9 | Mã lỗi | `ErrorCodes` theo dải module (101–109, 301–339…) | Chưa có bảng mã phía FE | Chỉ hiện được `message` BE trả |
| 10 | 403 | Body rỗng | ✅ Toast câu cố định trong `httpErrorInterceptor` | — |
| 11 | Alias import | — | ✅ `@/*`, `@sh/ui`, `@env/*` | — |
| 12 | Đăng xuất | — | ✅ `AuthService.logout()` xoá token + user | — |

## Steps

1. `core/http/api.models.ts`: `IApiResponse<T>`, `IPaging<T>`; hàm `unwrapApiResponse` ném `ApiError` khi
   `status = 0` để data-access của tính năng dùng (mục 1, 2, 9).
2. `environment.clientId = 'client-web2'`; `core/auth/auth.api.ts` gọi `/connect/token` (form-urlencoded, đặt
   `SKIP_ERROR_TOAST`, hiện `error_description`), `setTokens`, điều hướng `HOME_PATH` (mục 4, 5).
3. Gọi `user/me` sau đăng nhập, `setCurrentUser` kèm danh sách key quyền (mục 7).
4. `shared/permission/`: hằng quyền khớp `PermissionConstants`, directive `*hasPermission`, `permissionGuard`;
   dựng menu theo quyền (mục 8).
5. Refresh token trong `httpErrorInterceptor` khi `401` (mục 6).
6. Sửa `Page.pageSizeAll` theo quy ước `-1` (mục 3).
7. `npx ng build admin --configuration development` + `npm run build:admin`; đăng nhập thật với BE.

## Expected output

Đăng nhập bằng `admin` vào được `/admin`, menu hiện đúng theo quyền, toast hiển thị đúng message lỗi nghiệp vụ
từ BE; sẵn sàng làm màn hình theo [../architecture/06-them-man-hinh.md](../architecture/06-them-man-hinh.md).

## Điểm cần chú ý

- Không đổi BE để chiều FE; nếu thấy hợp đồng BE bất hợp lý thì hỏi trước (skill `task-workflow`).
- Không nhúng secret của `client-web` vào FE; dùng `client-web2`.
- Token để ở localStorage: đơn giản nhưng lộ trước XSS; giữ nguyên trong phạm vi đồ án, ghi rõ khi bảo vệ.
