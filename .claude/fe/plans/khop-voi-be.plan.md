# Plan: khớp sh.fe với hợp đồng sh.be

> **Trạng thái:** 📝 chờ duyệt — ghi nhận ngày 2026-10-11, chưa sửa code. BE là nguồn chân lý; FE sửa theo BE.
> Hợp đồng BE: [../../be/architecture/01-api.md](../../be/architecture/01-api.md),
> [../../be/architecture/06-shared.md](../../be/architecture/06-shared.md).

## Input

- BE chạy được, `POST /connect/token` (password grant) và các API đọc đã kiểm ngày 2026-10-11.
- FE là bộ khung: build dev sạch, chưa có màn hình nghiệp vụ, đăng nhập chưa gọi API.

## Chỗ lệch

| # | Mặt | BE | FE hiện tại | Ảnh hưởng |
|---|---|---|---|---|
| 1 | Envelope | `status` (1/0) | `checkStatusResponse`, `handleResponseInterceptor`, `IResponse*` đọc `statusCode` | Mọi response bị coi là lỗi |
| 2 | Phân trang (ra) | `data.items`, `data.totalItems` | `IResponseList`: `results`, `totalResults`, `page`, `limit` | Bảng rỗng |
| 3 | Phân trang (vào) | `pageSize = -1` là lấy tất cả | `Page.pageSizeAll = 9999999` | Chạy được nhưng sai quy ước |
| 4 | Đăng nhập | `POST /connect/token`, form-urlencoded, `grant_type=password`, `client_id` | `HLoginFeComponent.onSubmit` không gọi API | Không đăng nhập được |
| 5 | Client OAuth | Client có sẵn: `client-web`, `client-web2`… | `environment` không có `clientId` | Thiếu cấu hình |
| 6 | Refresh token | Hỗ trợ grant `refresh_token` (cần scope `offline_access`) | Hết hạn ⇒ về login | Phải đăng nhập lại |
| 7 | User hiện tại | `GET api/auth/user/me` → `ViewMeDto` (`id`, `userName`, `fullName`, `email`, `roles[].permissions[]`) | `IUser` (`role`, `branch_id`, `_id`…), `getUser()` đọc `userInfo` localStorage chưa ai ghi | Không có thông tin user |
| 8 | Quyền / menu | Key `Menu.*`, `Function.*` trong `PermissionConstants` | Menu rỗng, chưa có hằng quyền phía FE | Chưa ẩn/hiện theo quyền |
| 9 | Mã lỗi | `ErrorCodes` 101–108, 201… | `exceptionCodeWarnings` là mã dự án cũ; `code > 1 && < 1000` tính thông điệp chung nhưng biến không được dùng | Thông điệp lỗi không đúng ý |
| 10 | 403 | Body rỗng | Toast `err.error.message` | Toast rỗng |
| 11 | Alias import | — | `@lib-components/*` trỏ thư mục không tồn tại | Không dùng được alias |
| 12 | Đăng xuất | — | Topbar chưa có | Không đăng xuất được |

## Steps

1. Thêm `IApiResponse<T>`, `IPaging<T>` khớp BE; sửa `checkStatusResponse` / `handleResponseInterceptor` đọc
   `status` (mục 1, 2, 9).
2. Thêm `clientId` vào environment; `AuthService.login` gọi `/connect/token`, lưu token, điều hướng `/admin`
   (mục 4, 5).
3. Gọi `user/me` sau đăng nhập, lưu thông tin user và danh sách key quyền; sửa `IUser` (mục 7).
4. Hằng quyền phía FE khớp `PermissionConstants`; dựng menu theo quyền; thêm đăng xuất (mục 8, 12).
5. Refresh token trong interceptor khi `401` (mục 6) — **hỏi trước** vì cần thêm scope `offline_access`.
6. Sửa `Page.pageSizeAll`, thông điệp 403, `paths` trong `tsconfig.json` (mục 3, 10, 11).
7. `npx ng build admin --configuration development` + `npm run build:admin`; đăng nhập thật với BE.

## Expected output

Đăng nhập bằng `admin` vào được `/admin`, menu hiện đúng theo quyền, toast hiển thị đúng message lỗi nghiệp vụ
từ BE; sẵn sàng làm màn hình theo [../architecture/06-them-man-hinh.md](../architecture/06-them-man-hinh.md).

## Điểm cần chú ý

- Sửa `checkStatusResponse` ảnh hưởng mọi nơi gọi nó; hiện chưa có màn hình nào dùng nên rủi ro thấp.
- Không đổi BE để chiều FE; nếu thấy hợp đồng BE bất hợp lý thì hỏi trước (skill `task-workflow`).
- Token để ở localStorage: đơn giản nhưng lộ trước XSS; giữ nguyên trong phạm vi đồ án, ghi rõ khi bảo vệ.
