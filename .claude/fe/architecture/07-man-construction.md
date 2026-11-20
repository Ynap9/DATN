# Màn hình module construction

> **Phần 7/7** · trước: [06-them-man-hinh.md](06-them-man-hinh.md) · mục lục: [README.md](README.md)

Quyết định và bẫy riêng của từng màn đã làm; cách thêm màn mới chung ở [06-them-man-hinh.md](06-them-man-hinh.md).

## Metadata

**Metadata** (loại / trường văn bản), là mục menu riêng **Metadata** ngay dưới "Dự án" trong nhóm Hồ sơ thi công
(`/admin/construction/metadata`, `.../metadata/:id`; hằng `METADATA_URL`, Task 39). Đường `project-config` để dành cho màn Cấu hình dự án sau này. Menu không có mục "Hồ sơ".

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
  ⚠️ Lưới trường dựng lại `FormArray` mỗi lần tải editor: `@for` phải `track group` (đối tượng), đừng track `documentFieldId` —
  DOM được giữ lại nên `formControlName` vẫn gắn control cũ, bấm vào form không ghi nhận và lưu gửi giá trị cũ.

## Dự án

**Dự án** (Task 36, `/admin/construction/project`, `.../project/:id`; hằng `PROJECT_URL`, `PROJECT_STATUSES`):

- Danh sách đọc `GET project/paging` — mỗi dòng một cặp dự án – metadata, số liệu theo vai trò người xem; dự án 0 metadata không hiện
  nên form bắt chọn ≥ 1 metadata. Sửa từ danh sách gọi `GET {id}` trước để có đủ `documentTypeIds`.
- Ngày gửi chuỗi local `YYYY-MM-DDT00:00:00` của `sh-calendar`, **không** `toISOString` (lệch UTC lùi 1 ngày).
- Xoá dùng `DELETE admin/delete/{id}` (kèm dữ liệu), chỉ Admin / SuperAdmin có quyền Delete; không gọi `DELETE {id}` cũ.
- Nghiệm thu: `a` = tổng − đã kiểm, `b` = đã kiểm − QA đạt (khớp `340` của BE); sau mỗi lần chuyển gán dữ liệu trả về rồi tải lại `GET {id}`.
- Lối vào màn văn bản: Data / QA qua cột Thao tác "Mở văn bản" của bảng Tiến độ của tôi; người xem hết (`myProgresses = null`) qua
  tag Metadata — chỉ thành link ở metadata có trong `viewerRoles` (Task 42).

## Danh sách văn bản

Task 42, `/admin/construction/project/:id/metadata/:documentTypeId` (`features/construction/submission/`), quyền
`Menu.Construction.Submission` **hoặc** `Function.Construction.Submission.View`.

- Vai trò lấy từ `viewerRoles` của `GET project/{id}` (Admin / Leader → Quản lý, Data → Nhập liệu, QA → QA), **không** suy từ
  `myProgresses = null` — Leader ở metadata A kiêm Data ở B sẽ bị coi là Quản lý cả B. Không có dòng → gọi `count` một lần: 312 / 301
  toast rồi về chi tiết dự án, còn lại (kể cả 820) hiện trạng thái trống "chưa được giao" + nút Về dự án.
- Query string là nguồn trạng thái duy nhất, đặt **đúng tên tham số BE** (`statusCheck`, `statusQa`, `keyword`, `documentFieldId1`,
  `fieldValue1`, `isEmpty1`, … `pageNumber`, `pageSize`) + `view=qa` cho Quản lý; ghi bằng `replaceUrl`, đổi lọc / tab / chế độ về trang 1.
  F5 hay quay lại dựng lại đúng tab, lọc, trang. Tab Chờ QA = `statusCheck=1&statusQa=0`.
- Số trên tab: Nhập liệu gọi `count` không gửi trạng thái; QA gọi `qa/paging` `pageSize=1` hai lần (Chờ QA có `statusCheck=1`, Đạt /
  Không đạt không trạng thái) vì QA Không đạt đưa văn bản về Bị trả. "Tiếp tục văn bản đang làm" chỉ gọi lại khi đổi chế độ / Làm mới.
- Cột động từ `customData` của `paging`, khai `locked` để metadata thêm / bớt trường không xoá cấu hình các cột cố định. `tableKey`
  tách `construction-submission-<id>` / `construction-submission-qa-<id>` và hai thẻ `app-data-table` riêng (`@if`): DataTable chỉ đọc
  cấu hình cột theo `tableKey` lúc khởi tạo, dùng chung một thẻ thì bảng QA không nạp cấu hình của nó.
- Mở văn bản: Task 43 / 44 chưa có màn nên chỉ toast "Màn ... đang làm"; đường dẫn để sẵn ở hằng `WORK_PATHS` của component.
- ⚠️ `p-autocomplete` không khai `optionValue` thì model phát `option.value` khi gợi ý có trường `value` (DTO `field-values` có) —
  đừng bắt `ngModelChange` để lấy object, dùng `(onSelect)` / `(onClear)`. Cũng đừng bật `forceSelection` khi gợi ý dựng lại sau mỗi lần
  đổi URL: sự kiện `change` dò lại danh sách rỗng rồi `clear()`, xoá luôn giá trị vừa chọn. "Phải chọn từ gợi ý" giữ bằng việc chỉ
  `onSelect` mới ghi lên URL.

> **Tiếp:** hết bộ tài liệu — quay lại [README.md](README.md).
