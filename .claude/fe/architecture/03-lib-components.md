# UI kit @sh/ui (lib-components)

> **Phần 3/6** · trước: [02-app-admin.md](02-app-admin.md) · mục lục: [README.md](README.md)

`projects/lib-components/src/my-lib-components`, cổng duy nhất là `src/public-api.ts`, import qua alias `@sh/ui`.
Chỉ chứa thứ **hiển thị và nhập liệu**: không token, không router-guard, không gọi API, không biết module BE nào.

⚠️ Đừng đưa lại vào lib: service gọi `HttpClient`, đọc localStorage token, toast (`MessageService`) hay bất cứ
gì import từ `admin`. Những thứ đó thuộc `admin/src/app/core/`.

## Component

| Nhóm | Selector |
|---|---|
| Nhập liệu (`components/form-inputs/`, nền `sh-base.ts`) | `sh-input-text`, `sh-input-number`, `sh-calendar`, `sh-select`, `sh-multi-select`, `sh-password` |
| Lọc (`components/filter/`) | `sh-input-search`, `sh-dropdown-search` |
| Bảng (`components/sh-table/`) | `sh-table`, `sh-table-filter`, `sh-setting-display-column-table` |
| Khác | `sh-button`, `sh-checkbox`, `sh-confirm`, `sh-icon-required`, `sh-loading`, `sh-breadcrumb`, `sh-not-found` |

Phần lớn bọc component PrimeNG (`sh-select` → `p-select`, `sh-table` → `p-table` + `p-paginator`…). Ô nhập liệu kế
thừa `ShBaseControl` (cài `ControlValueAccessor`, dùng được với `formControlName` / `ngModel`) và gom nhãn, dấu `*`,
tooltip, thông báo lỗi để form nào cũng giống nhau.

`sh-table` nhận `rows`, `columns: IColumn[]`, `listAction: IAction[][]`, `paginator`, `serverConfig`,
`isLoading`, `columnCacheName` (lưu cấu hình cột). Kiểu cột ở `shared/interfaces/sh-table.interface.ts`, hằng ở
`shared/const/sh-table.const.ts` — cả hai đã export qua `public-api.ts`.

⚠️ `sh-table` import từng directive (`NgIf`, `NgClass`…) thay vì `CommonModule`, vì pipe `currency` của lib trùng tên
`CurrencyPipe` trong `CommonModule`. Đừng "gọn lại" bằng `CommonModule` ở component nào dùng `| currency`.
⚠️ Directive dạng thuộc tính trơn (`pRipple`, `pTemplate`, `pButton`) thiếu import thì Angular **bỏ qua im lặng**,
build vẫn qua; thêm vào template thì thêm module PrimeNG tương ứng vào `imports`.
⚠️ `sh-input-text.html` dùng `pInputTextarea` (tên cũ), PrimeNG 21 là `pTextarea`, nên ô textarea chưa nhận style.

## Pipe

| Pipe | Tác dụng |
|---|---|
| `function` | Gọi hàm từ template, chỉ chạy lại khi tham số đổi: `fn \| function: [a, b]` |
| `currency` | `1500000` → `1.500.000` (vi-VN), rỗng / sai trả `''` |
| `truncate` | Cắt chuỗi kèm `...`: `text \| truncate: [20]` |
| `libDateTime` | Định dạng ngày bằng `moment`: `ngay \| libDateTime: 'DD/MM/YYYY HH:mm'` |
| `labelDropdown` | Từ danh sách option + value ra label |
| `libAcceptFile` | `'image'` / `'video'` / `'media'` → chuỗi `accept` của `<input type="file">` |

## Validator

`validators/validator-common.ts`: `required`, `emailValidator`, `phoneValidator`, `passwordValidator`,
`mustMatch`, `dateRangeValidator`, `rangeNumberValidator`… Lỗi trả dưới key `errorMessage` để ô `sh-*` hiện đúng
thông điệp — vì vậy validator nằm cùng UI kit chứ không ở app.

## Page

`shared/models/page.ts`: `pageNumber` đếm từ 0 trong FE, `getPageNumber()` trả số **từ 1** để gửi API (khớp
`GetSkip()` của BE). `pageSize` mặc định 25, lựa chọn `[25, 50, 100, 200]`, `keyword` cho ô tìm kiếm.
⚠️ "Lấy tất cả" dùng `pageSizeAll = 9999999`, còn BE quy ước `pageSize = -1`.
⚠️ `Utils.setParamGetList` gửi `page` / `limit` (dự án cũ), BE đọc `pageNumber` / `pageSize`: tự dựng `HttpParams`
như mẫu ở [06-them-man-hinh.md](06-them-man-hinh.md) cho tới khi sửa hàm này.

## Hằng số và tiện ích

| File | Chứa |
|---|---|
| `shared/const/base.consts.ts` | `BaseConsts` (đuôi file, thông điệp), enum định dạng ngày, `EIconConfirm`, `DefaultImage`, `SuccessMessage`, `ErrorMessage` |
| `shared/interfaces/base.interface.ts` | `IDialogConfirmConfig`, `IType`, `IEventSubmitForm`… |
| `utils.ts` | `Utils` duy nhất của FE, ưu tiên bản SMS: storage, token (`getAccessToken`, `getRefreshToken`, `getDecodedJwtPayload`), PKCE, tải file, định dạng tiền / ngày, so sánh / clone dữ liệu, `mapDTO`, `setParamGetList`, `log` |

⚠️ `getSessionStorage` / `setSessionStorage` đọc ghi chuỗi thô, muốn lưu object thì tự `JSON.stringify` / `JSON.parse`. `getLocalStorage` trả `{}` khi chưa có key, không trả `null`.
⚠️ Hàm token trong `Utils` là ngoại lệ của luật "lib không biết token", giữ theo khuôn SMS để cả app dùng một `Utils`.

⚠️ `base.consts.ts` và `utils.ts` còn nhiều hằng / hàm của dự án cũ (`LOGO_EPIC`, `StatusResonse`…); dọn theo danh
sách được duyệt, không xoá đại trà.

> **Tiếp:** [04-http-auth.md](04-http-auth.md) — xác thực, interceptor và xử lý lỗi HTTP.
