# HTTP, xác thực và xử lý lỗi

> **Phần 4/6** · trước: [03-lib-components.md](03-lib-components.md) · mục lục: [README.md](README.md)

Các lớp nằm ở `lib-components/src/my-lib-components/shared/services/auth/` và `shared/guard/`.

## Token

`TokenService` lưu trong **localStorage**:

| Key | Giá trị |
|---|---|
| `access_token` | JWT từ `POST /connect/token` của BE |
| `refresh_token` | Refresh token (OpenIddict chỉ cấp khi request có scope `offline_access`) |

`AppAuthService.connectAuthorize()` xoá token rồi `window.location.href = '/auth/login'`.

## HandleInterceptorService

Mọi request đi qua, theo thứ tự:

1. **Chống bấm đúp**: request `POST`/`PUT`/`PATCH` trùng URL với request ghi đang chạy ⇒ trả `EMPTY` (bị nuốt).
   Muốn gửi song song cùng URL ⇒ thêm param `enableMultileRequest`.
2. **Gắn token**: có token ⇒ header `Authorization: Bearer <token>`, trừ khi param `unAuthorize=true`.
3. **Dọn param điều khiển**: bỏ `unAuthorize`, `enableMultileRequest`, `skipErrorToast` khỏi query trước khi gửi.
4. **Bắt lỗi HTTP**: `401` khi đang có token ⇒ `connectAuthorize()` (về trang login); mã khác ⇒ toast lỗi
   (`404` "Đường dẫn không tồn tại", `403` message từ body, còn lại thông điệp chung), trừ khi `skipErrorToast=true`.

```typescript
const params = new HttpParams().set('skipErrorToast', true).set('unAuthorize', true);
this.http.get(url, { params });
```

⚠️ Chỉ có **lỗi HTTP** mới qua nhánh bắt lỗi. BE trả lỗi nghiệp vụ bằng **HTTP 200** + `status = 0`, nên lỗi
nghiệp vụ phải được service/component đọc qua `checkStatusResponse`, interceptor không thấy.

⚠️ Chưa có refresh token: hết hạn là `401` rồi về login, dù BE hỗ trợ grant `refresh_token`.

## Hợp đồng với BE

| Mặt | BE trả | FE đang đọc |
|---|---|---|
| Envelope | `{ status, data, code, message }` | `statusCode`, `message` |
| Thành công | `status = 1`, `code = 200` | `statusCode === 1` |
| Lỗi nghiệp vụ | HTTP 200, `status = 0`, `code` = mã BE (`103`, `105`…) | toast `message` |
| Danh sách phân trang | `data: { items, totalItems }` | `data: { results, totalResults, page, limit }` |
| Thiếu token / hết hạn | HTTP 401 | về login |
| Thiếu quyền | HTTP 403, body rỗng | toast `err.error.message` (rỗng) |

Mọi dòng lệch đã ghi trong [../plans/khop-voi-be.plan.md](../plans/khop-voi-be.plan.md); BE là nguồn chân lý,
FE sửa theo BE.

## AuthGuard

`projects/shared/guard/auth.guard.ts`, `canActivate` + `canActivateChild`. Chỉ kiểm có `access_token` trong
localStorage. Không kiểm hạn token, không kiểm quyền theo route.

## GlobalErrorHandler

Bắt lỗi JavaScript toàn cục; lỗi `Loading chunk N failed` (deploy bản mới khi tab cũ còn mở) ⇒ reload trang.
Lỗi khác chỉ `console.log`.

> **Tiếp:** [05-conventions.md](05-conventions.md) — quy ước code bắt buộc.
