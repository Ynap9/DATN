# Màn hình module construction

> **Phần 7/13** · trước: [06-them-man-hinh.md](06-them-man-hinh.md) · mục lục: [README.md](README.md)

Quyết định và bẫy riêng của từng màn đã làm (cập nhật 2026-11-21); cách thêm màn mới chung ở [06-them-man-hinh.md](06-them-man-hinh.md).

## Metadata

**Metadata** (loại / trường văn bản), là mục menu riêng **Metadata** ngay dưới "Dự án" trong nhóm Hồ sơ thi công
(`/admin/construction/metadata`, `.../metadata/:id`; hằng `METADATA_URL`, Task 39). Màn Cấu hình (`/admin/construction/project-config`, Task 50) xem [12-man-cau-hinh.md](12-man-cau-hinh.md). Menu không có mục "Hồ sơ".

- Danh sách có Số trường, Số dự án đang dùng, Ngày tạo; "Xóa" ẩn khi `projectCount > 0` (BE vẫn chặn `346`).
- Chi tiết: tiêu đề có đường kẻ tách nội dung; 4 nút 32px ở đầu trang — Kiểm tra Google Sheet, Tải template (nền trắng viền xám, tải có
  vòng xoay) | Thêm nhanh (viền + chữ màu chủ đạo), Thêm trường (đặc); điện thoại chia lưới 2 cột. Hàng công cụ cao 38px: ô tìm 360px bên trái |
  "Thứ tự theo" dạng bảng / danh sách bên phải + icon `?` (không khung nút) mở popup giải thích hai dạng (nội dung lấy từ
  `DOCUMENT_TYPE_VIEWS[].description`, có nhãn Mặc định và lối "Đổi dạng mặc định"). Bảng trường căn giữa mọi cột trừ Tên trường, Tên cột import.

- Route nhận `Menu.Construction.DocumentType` **hoặc** `Function.Construction.DocumentType.View`; menu theo View, vì tích một Function con
  không cấp key Menu cha.
- Form trường không có ô mã: FE gửi `field = name` (mã trường BE không dùng ở luồng nào); tên cột import để trống thì gửi bằng tên.
- Kéo thả gửi `targetOrder` = thứ tự đang đứng ở vị trí thả; tắt khi đang tìm kiếm vì danh sách lọc không liền thứ tự.
- Tab "Trường" | "Prompt OCR" (Task 21B): tab Prompt chỉ hiện khi có `Function.Ai.PromptTemplate.View`, giữ ở `?tab=prompt` (`replaceUrl`),
  4 nút đầu trang chỉ thuộc tab Trường; component tab dựng lại mỗi lần mở để thấy ngay trường vừa tắt OCR. Xuất bản khi form đang sửa
  hoặc chưa có nháp (hay ghi chú trong dialog khác form — BE publish giữ note cũ khi rỗng) thì hỏi trước rồi lưu nháp + publish (Huỷ
  không tạo nháp); lỗi 909 / 910 chỉ báo, không tự tải lại. Rời tab / trang
  khi form dirty hỏi qua `confirmDiscardPrompt` (Promise có nhánh từ chối — `confirmAction` không có) + `CanDeactivateFn` ở route `metadata/:id`.
  Lưu ý: Lưới trường dựng lại `FormArray` mỗi lần tải editor: `@for` phải `track group` (đối tượng), đừng track `documentFieldId` —
  DOM được giữ lại nên `formControlName` vẫn gắn control cũ, bấm vào form không ghi nhận và lưu gửi giá trị cũ.

## Dự án

**Dự án** (Task 36, `/admin/construction/project`; hằng `PROJECT_URL`, `PROJECT_STATUSES`):

- Danh sách đọc `GET project/paging` — mỗi dòng một cặp dự án – metadata, số liệu theo vai trò người xem; dự án 0 metadata không hiện
  nên form bắt chọn ≥ 1 metadata. Sửa từ danh sách gọi `GET {id}` trước để có đủ `documentTypeIds`.
- Form **Tạo** có 2 nút gạt chỉ đặt lúc tạo, mặc định bật: "Có kiểm tra QA" (`isQaEnabled`) và "Tạo Google Sheet đồng bộ"
  (`isGoogleSheetEnabled`, Task 54). Tắt Sheet → dự án không có `urlGoogleSheetSync`, chỉ nhập bằng Excel, ẩn nút "Mở Google
  Sheet"; bật mà chưa kết nối Google → BE trả `304`, form giữ nguyên dữ liệu. Form **Sửa** không có và không gửi 2 trường này.
- Ngày gửi chuỗi local `YYYY-MM-DDT00:00:00` của `sh-calendar`, **không** `toISOString` (lệch UTC lùi 1 ngày).
- Xoá dùng `DELETE admin/delete/{id}` (kèm dữ liệu), chỉ Admin / SuperAdmin có quyền Delete; không gọi `DELETE {id}` cũ.
- Bấm tên / thẻ dòng mở thẳng danh sách văn bản đúng metadata của dòng (`project/:id/metadata/:documentTypeId`, Task 46) — màn chi tiết
  dự án cũ đã bỏ. Mục ⋯ đầu tiên "Báo cáo tiến độ" chỉ hiện khi `row.viewerRole` là Admin / Leader.

## Báo cáo tiến độ

Task 46, `/admin/construction/project/:id` (`project/project-report/`), chỉ để xem — không có lối sang danh sách văn bản.

- Chỉ Admin / Leader: không có metadata nào mình quản lý → toast "Chỉ quản lý dự án xem được báo cáo tiến độ" rồi về danh sách dự án.
- Query string `?documentTypeId=&range=7|30|all` là nguồn trạng thái; thiếu / sai → metadata quản lý đầu tiên + 30 ngày, ghi lên URL
  (`replaceUrl`) rồi mới tải để không gọi số hai lần. Không có "Tất cả" metadata; quản lý một metadata thì hiện tên, không dropdown.
- `buildRange`: 7 / 30 ngày tính lùi từ hôm nay; Toàn bộ = từ `startedAt` nhưng không quá 366 ngày (trần BE). Nhãn khoảng lấy
  `fromDate` / `toDate` trong response.

| Khối | Nguồn | Theo |
|---|---|---|
| KPI | `acceptance` + `daily` (+N 7 ngày) + `qa-stats/summary` (đạt lần đầu) | metadata đang chọn |
| Tiến độ theo ngày | `progress-stats/daily` | metadata + khoảng |
| Nghiệm thu | `project/{id}/acceptance` | toàn dự án |
| Trạng thái theo metadata | `acceptance` (mọi metadata, kể cả "(đã gỡ)") | toàn dự án, bỏ qua khoảng |
| Chất lượng QA | `qa-stats/summary` | metadata + khoảng |
| Trường hay sai · Năng suất | `qa-stats/fields` · `qa-stats/entry-users` | metadata + khoảng, `@defer (on viewport)` |

- Mỗi khối một luồng `switchMap` riêng qua `bindReportBlock` (`project-report.constants.ts`): đổi lọc huỷ request cũ, lỗi chỉ đánh dấu
  khối đó ("Chưa tải được …" + Thử lại), có số rồi thì làm mờ chứ không nháy skeleton. Hai khối `@defer` nhận `filter`; trang tạo object
  `statsFilter` mới mỗi lần đổi lọc / Làm mới nên khối tự tải lại, không cần `reloadToken`.
- Dự án không QA: KPI 3 cột, biểu đồ chỉ đường Đã kiểm, ẩn 3 khối QA và không gọi API của chúng; không còn chữ "QA" trên màn.
- Thiếu `ProgressStats` / `QaStats` → khối giữ khung, ghi "Bạn chưa được cấp quyền xem mục này", không dựng component nên không gọi API.
- Nghiệm thu: luật nút / chặn / dialog lý do / PDF chuyển nguyên từ màn chi tiết cũ (`a` = tổng − đã kiểm, `b` = đã kiểm − QA đạt, khớp
  `340`); chuyển xong khối phát `changed`, trang tải lại dự án + acceptance. Câu "Tự vào nghiệm thu khi …" chỉ hiện ở bước Đang triển khai.
- Màu dữ liệu khai một lần ở `REPORT_COLORS`; số định dạng `Intl` vi-VN (`formatNumber`, `formatPercent`), không đăng ký locale `vi`.
- Lưu ý: chart.js vẽ canvas nên đọc token bằng `readCssVar(element, name)` trên phần tử của khối; đừng đọc `document.documentElement`
  — token `--app-*` không ở `:root`, ra chuỗi rỗng và chart tô đen.

## Danh sách văn bản

Task 42, `/admin/construction/project/:id/metadata/:documentTypeId` (`features/construction/submission/`), quyền
`Menu.Construction.Submission` **hoặc** `Function.Construction.Submission.View`.

- Vai trò lấy từ `viewerRoles` của `GET project/{id}` (Admin / Leader → Quản lý, Data → Nhập liệu, QA → QA), **không** suy từ
  `myProgresses = null` — Leader ở metadata A kiêm Data ở B sẽ bị coi là Quản lý cả B. Không có dòng → gọi `count` một lần: 312 / 301
  toast rồi về danh sách dự án, còn lại (kể cả 820) hiện trạng thái trống "chưa được giao" + nút Về danh sách dự án.
- Query string là nguồn trạng thái duy nhất, đặt **đúng tên tham số BE** (`statusCheck`, `statusQa`, `keyword`, `documentFieldId1`,
  `fieldValue1`, `isEmpty1`, … `pageNumber`, `pageSize`) + `view=qa` cho Quản lý; ghi bằng `replaceUrl`, đổi lọc / tab / chế độ về trang 1.
  F5 hay quay lại dựng lại đúng tab, lọc, trang. Tab Chưa QA = `statusCheck=1&statusQa=0`.
- Số trên tab: Nhập liệu gọi `count` không gửi trạng thái; QA gọi `qa/paging` `pageSize=1` hai lần (Chưa QA có `statusCheck=1`, Đạt /
  Không đạt không trạng thái) vì QA Không đạt đưa văn bản về Trả lại. "Tiếp tục văn bản đang làm" chỉ gọi lại khi đổi chế độ / Làm mới.
- Cột động từ `customData` của `paging`, khai `locked` để metadata thêm / bớt trường không xoá cấu hình các cột cố định. `tableKey`
  tách `construction-submission-<id>` / `construction-submission-qa-<id>` và hai thẻ `app-data-table` riêng (`@if`): DataTable chỉ đọc
  cấu hình cột theo `tableKey` lúc khởi tạo, dùng chung một thẻ thì bảng QA không nạp cấu hình của nó.
- Đổi tab / lọc / trang / chế độ huỷ request `paging` / `qa/paging` / `count` cũ (`_pageSubscription`, `_countSubscription`, Task 61): chỉ response lần gọi cuối
  được áp. Huỷ **trước** rồi mới đặt `loading = true`, vì `finalize` của request cũ chạy khi huỷ và sẽ tắt spinner của request mới.
- Mở văn bản: Nhập liệu sang màn ở [08-man-nhap-lieu.md](08-man-nhap-lieu.md) (mang bộ lọc + tab, `state.fromList` để nút ← quay
  lại bằng `location.back()`); QA sang màn ở [09-man-qa.md](09-man-qa.md), cũng mang bộ lọc; Lịch sử mở dialog. Đường dẫn ở hằng `WORK_PATHS`.
- Lưu ý: `p-autocomplete` không khai `optionValue` thì model phát `option.value` khi gợi ý có trường `value` (DTO `field-values` có) —
  đừng bắt `ngModelChange` để lấy object, dùng `(onSelect)` / `(onClear)`. Cũng đừng bật `forceSelection` khi gợi ý dựng lại sau mỗi lần
  đổi URL: sự kiện `change` dò lại danh sách rỗng rồi `clear()`, xoá luôn giá trị vừa chọn. "Phải chọn từ gợi ý" giữ bằng việc chỉ
  `onSelect` mới ghi lên URL.

### Xoá văn bản (Task 58)

`DELETE construction/submission` (BE [14](../../be/nghiep-vu/14-ho-so-xoa-xuat.md#xoá-hồ-sơ)), tất cả hoặc không. Dialog ở `submission-delete-dialog/`, gọi API ngay trong dialog.

- **58.1 Ai thấy:** `canDelete` = Admin / SuperAdmin **và** `Submission.Delete` **và** không ở "Xem theo QA" **và** (SuperAdmin hoặc dự án không `DONE` / `EXPIRED`). Sai thì màn y như cũ (không cột tick, không thanh chọn). Admin nhận biết bằng `roles.includes(AuthConstants.ADMIN_ROLE)`.
- **58.2 / 58.3 Cột tick:** chỉ thẻ `app-data-table` thường gắn `selectable` + `[(selection)]` + `[rowDisabledReason]` ([05](05-conventions.md)). Ô khoá theo `statusCheck`: Chưa kiểm ai cũng tick; Đã kiểm chỉ SuperAdmin; Trả lại không ai. Tick header chỉ chọn dòng tick được.
- **58.4 Thanh chọn:** có ≥ 1 dòng chọn thì hiện trên bảng "Đã chọn n văn bản trong trang này", "Bỏ chọn", nút đỏ "Xoá n văn bản".
- **58.5 Phạm vi:** chỉ trang đang xem. `load()` đặt `selectedRows = []` nên đổi tab / lọc / từ khoá / trang / chế độ, Làm mới và xoá xong đều bỏ chọn.
- **58.6 Xác nhận:** dialog liệt kê file (`Hồ sơ · người giữ` lấy từ `holderUserName` của `paging`, trống thì "Chưa giao"), 3 hệ quả và cảnh báo không hoàn tác. Đang gọi API thì Huỷ và nút đóng bị khoá.
- **58.7 Xong:** toast "Đã xoá n văn bản" + "và m ô dữ liệu đã nhập", rồi `refresh()` (bảng, số đếm tab, "Tiếp tục văn bản đang làm").
- **58.8 Lỗi 325 / 336:** BE trả **một** mã mỗi lần (336 kiểm trước 325) và Id ở cuối câu (`/Id = ([\d,\s]+)$/`). Dialog đổi sang "Chưa xoá văn bản nào" liệt kê file lỗi kèm chip "Đang sửa / QA" (336) hoặc "Đã kiểm" (325); "Bỏ chọn k văn bản này" chỉ bỏ các Id đó khỏi lựa chọn, **không** gọi lại API. Không bắt được Id thì xử lý như 58.9. Admin không gặp 325 vì ô Đã kiểm đã khoá.
- **58.9 Lỗi khác:** toast câu của BE, giữ lựa chọn, dialog vẫn mở. 316 / 323 thì đóng dialog và tải lại dự án để `canDelete` ẩn cột tick.

Đã test trình duyệt 2026-11-26 (dự án ZZTEST58, đã dọn): 12/12 ca, kể cả tooltip ô khoá, tick header chỉ 20/37 dòng, xoá thật 3 văn bản (37 → 34), SuperAdmin xoá Đã kiểm, 336 với văn bản trong phiếu QA, màn Metadata xoá trường nhanh vẫn chạy.

> **Tiếp:** [08-man-nhap-lieu.md](08-man-nhap-lieu.md) — màn Nhập liệu và dialog Lịch sử.
