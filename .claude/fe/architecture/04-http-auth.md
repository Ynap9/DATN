# Xác thực, HTTP và xử lý lỗi

> **Phần 4/6** · trước: [03-lib-components.md](03-lib-components.md) · mục lục: [README.md](README.md)

Nằm ở `admin/src/app/core/auth/` và `core/http/`. Lib `@sh/ui` không có phần nào ở đây.

## core/auth

| File | Làm gì |
|---|---|
| `service/auth.service.ts` | `login`, `refreshAccessToken`, `logout` (xoá storage, `SharedService.clearAll()`, về login) |
| `guard/auth.guard.ts` | Gắn ở route `admin`: `getMe` lỗi ⇒ về login kèm `redirect_uri`; thành công ⇒ nạp vai trò, quyền vào `SharedService` |
| `guard/permission.guard.ts` | Gắn ở route con với `data: { permissions: [...] }`: SuperAdmin hoặc có ít nhất một key thì qua, không thì về `auth/access` |

⚠️ `core/auth` đang import `@/services/shared.service`, `@/services/user.service`, lệch luật tầng trong [README.md](README.md).

## core/http — interceptor dạng hàm

Đăng ký theo thứ tự trong `app.config.ts`:

| Interceptor | Làm gì |
|---|---|
| `baseUrlInterceptor` | URL bắt đầu bằng `/api/` hoặc `/connect/` được ghép `environment.api`; service chỉ ghi đường dẫn |
| `httpErrorInterceptor` | Lỗi HTTP ⇒ toast (`401`, `403`, `404` có câu riêng, còn lại câu chung), trừ khi đặt `SKIP_ERROR_TOAST` |
| `authInterceptor` | Bật `withCredentials` cho request `/api/`, trừ khi đặt `SKIP_AUTH`; `401` ⇒ refresh một lần qua `AuthService.refreshAccessToken` rồi gửi lại, không refresh được ⇒ `logout()` |

Token nằm trong cookie `HttpOnly` do BE đặt, JS không đọc được. FE dùng client `client-web2`, không có secret.

⚠️ `authInterceptor` phải đứng sau `httpErrorInterceptor` trong mảng để `401` được thử refresh trước khi hiện toast.

Cờ điều khiển là `HttpContextToken` trong `http-context.ts`, **không** đi kèm query string:

```typescript
const context = new HttpContext().set(SKIP_ERROR_TOAST, true).set(SKIP_AUTH, true);
this._http.get(url, { context });
```

`DownloadService.get(url)` / `post(url, body)` trả `HttpResponse<Blob>`; lưu file bằng `Utils.makeDownload`.

⚠️ Chỉ **lỗi HTTP** mới qua `httpErrorInterceptor`. BE trả lỗi nghiệp vụ bằng **HTTP 200** + `status = 0`, nên lỗi
nghiệp vụ phải được đọc ở tầng data-access của tính năng (sẽ có `unwrapApiResponse` theo plan khớp BE).
⚠️ Đăng nhập sai BE trả HTTP **400** `{ error: 'invalid_grant', error_description }`; lời gọi `/connect/token` phải
đặt `SKIP_ERROR_TOAST` và tự hiện `error_description`, nếu không sẽ ra toast câu chung.

## GlobalErrorHandler

`core/error/global-error-handler.ts`: `console.error` mọi lỗi; lỗi tải chunk (`Loading chunk N failed` hoặc
`Failed to fetch dynamically imported module` của builder esbuild) ⇒ reload trang.

## Hợp đồng với BE

| Mặt | BE trả | FE hiện tại |
|---|---|---|
| Envelope | `{ status, data, code, message }` | Chưa có kiểu, chưa có hàm đọc |
| Lỗi nghiệp vụ | HTTP 200, `status = 0`, `code` = mã BE (`103`, `105`…) | Chưa đọc |
| Danh sách phân trang | `data: { items, totalItems }` | Chưa có kiểu |
| Thiếu token / hết hạn | HTTP 401 | `logout()` |
| Thiếu quyền | HTTP 403, body rỗng | Toast "Bạn không có quyền thực hiện thao tác này" |

Phần còn thiếu ghi trong [../plans/khop-voi-be.plan.md](../plans/khop-voi-be.plan.md); BE là nguồn chân lý.

> **Tiếp:** [05-conventions.md](05-conventions.md) — quy ước code bắt buộc.
