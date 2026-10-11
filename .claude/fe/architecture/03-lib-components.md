# Thư viện lib-components

> **Phần 3/6** · trước: [02-app-admin.md](02-app-admin.md) · mục lục: [README.md](README.md)

`projects/lib-components/src/my-lib-components`. Một NgModule `MyLibComponentsModule` khai báo và export mọi
component, pipe; `forRoot()` thêm `LibHelperService`. Export công khai qua `src/public-api.ts`.

## Component

| Nhóm | Selector |
|---|---|
| Nhập liệu (`components/form-inputs/`, nền `h-base.ts`) | `h-input-text`, `h-input-number`, `h-calendar`, `h-select`, `h-multi-select`, `h-password-component` |
| Lọc (`components/filter/`) | `h-input-search`, `h-dropdown-search` |
| Bảng (`components/h-table-component/`) | `h-table`, `h-table-filters`, `h-setting-display-column-table` |
| Khác | `h-button`, `h-checkbox`, `h-confirm`, `h-icon-required`, `h-loading-component`, `h-breadcrumb-component`, `h-not-found-component`, `h-login-fe` |

`h-table` nhận `rows`, `columns: IColumn[]`, `listAction: IAction[][]`, `paginator`, `serverConfig`,
`isLoading`, `columnCacheName` (lưu cấu hình cột). Kiểu cột ở `shared/interfaces/h-table.interface.ts`, hằng ở
`shared/const/h-table.const.ts`.

## Pipe

`function`, `labelDropdown`, `truncate`, `currency`, `imageResize`, `libAcceptFile`, `libDateTime`.

## HComponentBase

Lớp trừu tượng cho component **trang**. Constructor nhận `Injector`, tự lấy `LibHelperService`, `TokenService`.

| Thành viên | Dùng để |
|---|---|
| `page: Page` | Trạng thái phân trang, từ khoá tìm kiếm |
| `isLoading`, `submitted`, `onClickSubmit`, `destroyed$` | Cờ giao diện, huỷ subscription |
| `checkInValidForm(form)`, `isInvalid`, `isInvalidMessage`, `markControls` | Kiểm form reactive |
| `messageSuccess` / `messageError` / `messageWarn` | Toast PrimeNG |
| `handleResponseInterceptor(res, msg?)`, `checkStatusResponse(res, msg?)` | Đọc envelope response, báo toast |
| `confirmAction(message, icon?)` | Dialog xác nhận, trả `Promise<boolean>` |
| `formatCalendarItemSendApi(date)` | Định dạng ngày gửi API (`YYYY-MM-DDTHH:mm:ss`) |
| `getUser()` | Đọc `userInfo` từ localStorage |

```typescript
export class UserListComponent extends HComponentBase implements OnInit {
    constructor(injector: Injector, private _userService: UserService) {
        super(injector);
    }
}
```

⚠️ `handleResponseInterceptor` và `checkStatusResponse` đọc `response.statusCode`, còn BE trả `status`. Gọi
API BE thật thì hai hàm này luôn coi là lỗi — phải sửa theo plan khớp BE trước khi dựng màn hình đầu tiên.

## LibHelperService

Gom service PrimeNG và Angular (`MessageService`, `DialogService`, `Router`, `HttpClient`…) cho
`HComponentBase`. Có thêm `dialogConfirmRef`, `downloadFile`, `downloadFilePost`, `mapDTO`, `getContentHtml`.

⚠️ Hai danh sách `exceptionCodeWarnings`, `exceptionErrorCodeNothingPass` là mã lỗi của dự án cũ (4104, 7120…),
không có trong `ErrorCodes` của BE. Đừng thêm mã mới vào đây khi chưa đối chiếu BE.

## Page

`shared/models/page.ts`: `pageNumber` đếm từ 0 trong FE, `getPageNumber()` trả số **từ 1** để gửi API (khớp
`GetSkip()` của BE). `pageSize` mặc định 25, lựa chọn `[25, 50, 100, 200]`, `keyword` cho ô tìm kiếm.
⚠️ "Lấy tất cả" dùng `pageSizeAll = 9999999`, còn BE quy ước `pageSize = -1`.

## Hằng số và kiểu

| File | Chứa |
|---|---|
| `shared/const/base.consts.ts` | `BaseConsts` (key token, đường login, thông điệp), enum định dạng ngày, `SuccessMessage`, `ErrorMessage`, `StatusResonse` |
| `shared/const/error-code.const.ts` | Mã HTTP và tên; thêm một lớp `ErrorMessage` trùng tên |
| `shared/interfaces/response.interface.ts` | `IResponseList`, `IResponseItem`, `IResponseItemArray` |
| `utils.ts` | `Utils`: localStorage, sessionStorage, tải file, định dạng tiền, `log` |

⚠️ `ErrorMessage` khai hai lần (`base.consts.ts` và `error-code.const.ts`) với nội dung gần giống nhau. Import
từ `base.consts.ts` (đã export qua `public-api.ts`).

> **Tiếp:** [04-http-auth.md](04-http-auth.md) — token, interceptor và xử lý lỗi.
