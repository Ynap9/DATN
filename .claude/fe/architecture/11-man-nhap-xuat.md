# Màn Nhập / Xuất dữ liệu

> **Phần 11/13** · trước: [10-man-operation.md](10-man-operation.md) · mục lục: [README.md](README.md)

Task 48 (cập nhật 2026-11-27, đổi theo Task 63). Admin / Leader của dự án nhập văn bản từ link Google Sheet bất kỳ hoặc file Excel
(wizard 4 bước: Nguồn dữ liệu → Ghép trường → Kiểu import → Chạy thử & import, theo 5sdb), và xuất theo bộ lọc + cột thêm ra file Excel
hoặc ghi vào một Google Sheet (Task 56, job chạy nền). Nhập Google đã kiểm trên trình duyệt (Task 63); xuất Google Sheet đã kiểm (xem cuối file).

## Route và file

| Thứ | Chỗ |
|---|---|
| Route | `/admin/construction/data-transfer`, `permissionGuard` = có `Function.Construction.Submission.Import` **hoặc** `.Export`, `canDeactivate` inline trong `construction.routes.ts` |
| Menu | "Nhập / Xuất dữ liệu" dưới "Metadata"; `permission` là **mảng key** (`AppMenu.canView`: có một là hiện) |
| Màn | `data-transfer/data-transfer.component.*`: picker cặp trong `app-page-header`, "Dự án … · N văn bản" là `description` của header (dưới tiêu đề), tab theo quyền |
| Wizard Nhập | `import-wizard/` giữ trạng thái + gọi API; bước con `import-source-step/` (link + sheet + cột bắt đầu, upload), `import-field-step/` (ghép trường), `import-kind-step/` (kiểu + cột cố định), `import-verify-step/` (chỉ hiển thị) |
| Tab Xuất dữ liệu | `export-panel/`: thẻ chọn đích (Excel / Google Sheet), khối đích Sheet, danh sách "Đã xuất" chung cho cả hai đích |
| Hằng của màn | `data-transfer.constants.ts`: tên bước, thẻ nguồn / kiểu, `IImportColumnMapping`, `IImportFieldRow`, `toHeaderKey`, `IMPORT_FIXED_COLUMN_KEYS`, `isColumnMappingComplete`, `getVerifyView`, `TOAST_ERROR_CODES`, `EXPORT_TARGET` / `EXPORT_TARGET_OPTIONS`, `IExportJobRow`, `formatNumber` |
| Hằng chung | `IMPORT_KIND`, `IMPORT_SOURCE`, `BACKGROUND_JOB_STATUS`, `EXPORT_COLUMNS`, `IMPORT_EXCEL_MAX_BYTES`, `JOB_POLL_MS`, `EXPORT_DOWNLOAD_URL_MS` ở `submission.constants.ts` |
| API | `SubmissionService`: `getImportSheetHeaders` / `matchImportFieldMapping` / `saveImportFieldMapping`, `verifyImport` / `importSheet`, `uploadImportExcel`, `verifyImportExcel` / `createImportExcelJob` / `findImportJob`, `createExportJob` / `findExportJob`, `getExportSheetNames` / `createExportSheetJob` / `findExportSheetJob`. Wizard không còn gọi `DocumentFieldService.getSheetHeaders` (cần quyền `DocumentField.Create`; màn kiểm tra sheet của loại văn bản vẫn dùng) |

## Quyết định

- Picker dùng chung với `viewerRoles = ['Admin', 'Leader']`, **không khoá picker** khi đang import (người dùng chốt, lệch 48.4; nhắc lại 24/11: import chạy nền nên không cần khoá).
  Đổi cặp giữa chừng → wizard huỷ subscription (lượt tải file, chạy thử, import, hỏi job) và về bước 1; import Google có
  thể vẫn chạy xong ở BE nhưng màn không hiện kết quả. Tab vẫn khoá khi đang import.
- Bố cục (sửa 24/11 sau khi xem trên trình duyệt): giữ thanh bước dọc và tiêu đề khung "Tên bước · Bước x / 4"; nội dung bước
  lấp hết bề ngang cột phải (bỏ giới hạn 720 px), khung **không** đặt chiều cao tối thiểu (cao theo nội dung / thanh bước).
  "Không đọc được tên cột" ở bước 3 là hộp cảnh báo vàng. Tab Xuất: các nhóm cột thêm chia đều một hàng
  (`repeat(auto-fit, minmax(180px, 1fr))`), nút "Chọn hết" cạnh tiêu đề "Cột thêm".
- Hai tab ẩn bằng `[class.hidden]` chứ không `@if`, để đổi tab không mất tiến độ wizard / danh sách file đã xuất.
- Dự án `ACCEPTING` / `DONE` / `EXPIRED` → khung xám thay wizard; tab Xuất vẫn dùng được.
- Upload: FE chặn đuôi khác `.xlsx` và > 20 MB trước khi gửi; `observe: 'events'` để hiện %.
- Bước 1 (63.2, 63.3): thẻ Google không bao giờ khoá, mặc định chọn Google, ô link điền sẵn `urlGoogleSheetSync` của dự án (rỗng nếu chưa gắn). Link của dự án thì tự gọi
  `getImportSheetHeaders` một lần (chờ có tên sheet của dự án để chọn sẵn); lỗi hiện chữ đỏ dưới ô link, không toast; sửa link xoá danh sách + sheet và huỷ lượt gọi dở.
  "Bắt đầu đọc từ cột" (Google và Excel, 1 … số cột của sheet) co lại khi đổi sheet; "Tải file mẫu" ẩn khi thiếu `DocumentType.View`.
- Bước 2 (63.4, 63.5): `matchImportFieldMapping` → bảng trường ↔ cột (giá trị đầu = `matchedColumnIndex`, lựa chọn là vị trí cột nên header trùng tên hiện "Ghi chú (cột thứ 6)"). Thiếu cột: không khoá nút, bấm Tiếp tục mới viền đỏ + cuộn tới dòng;
  một cột cho hai trường / tên cột lặp k lần mà ghép < k trường (BE `309`) thì khoá. "Tiếp tục" luôn `saveImportFieldMapping` đủ mọi trường. Ghép giữ trong wizard; đổi link / sheet / file / cột bắt đầu ở bước 1 → tải lại và xoá ô cột cố định không còn trong header (63.10).
- Bước 3 (63.6, 63.7): thẻ kiểu + ô cột cố định theo kiểu (Thêm mới có link bắt buộc + Số trang; Bổ sung link tuỳ chọn; Ghi đè chọn trường trong các trường đã ghép, `columnImport` = cột đã ghép của trường). Gợi ý chỉ từ cột bắt đầu; cột cố định trùng cột trường → dòng vàng, không chặn.
- Bước 3 → 4 **luôn** qua chạy thử; Quay lại từ bước 4 xoá kết quả (BE không lưu chạy thử). Lỗi chạy thử → ở bước 3, khung
  đỏ `whitespace-pre-line`, cao tối đa 240 px. Lỗi import / job → bước 4, nút chính thành "Thử lại" (= chạy thử lại). Số chạy thử là số ô có dữ liệu BE đếm, FE không tự đếm: ô trống / chỉ có khoảng trắng không tính, chữ "null" / "undefined" là dữ liệu nên vẫn tính (chốt 27/11).
- Import Google đồng bộ: khoá nút + tab, hỏi khi rời trang (`canDeactivate` + `beforeunload`). Excel tạo job rồi hỏi
  `findImportJob` mỗi `JOB_POLL_MS`, không hỏi khi rời. Xong → khối "Import xong", màn cha tải lại số văn bản.
- Body (63.9): ô trống gửi `''`; ghi đè gửi `linkColumnImport = ''`; Google cả 3 kiểu gửi `url` + `sheetName` của bước 1; Excel gửi `fileKey` + `sheetName` (không `url`); cả hai nguồn gửi `startColumn`.
- Ô "Cột Số trang" (Task 62, tuỳ chọn) chỉ hiện ở kiểu Thêm mới, header "Số trang" điền sẵn; kiểu khác body gửi `pageCountColumn = ''`. Chạy thử Thêm mới có thêm ô số phụ "Văn bản có số trang" chỉ khi đã nhập cột.

Bẫy: ghi đè gửi `columnImport` = tên header của cột đã ghép cho trường; từ Task 67 BE đọc đúng cột đã ghép của trường (không dò lại theo tên) nên sheet có nhiều cột trùng tên vẫn không ghi đè nhầm, và FE không còn dòng vàng cảnh báo trùng tên. `columnImport` không khớp cột đã ghép → BE `307`. Đừng thêm lại cảnh báo hay gửi vị trí cột.
Bẫy: `toHeaderKey` (hoa thường + gộp khoảng trắng) phải khớp `IsFieldImportMatch` của BE, nếu không FE báo "đủ" còn BE trả `309` / `307`.
- Tab Xuất: lọc trạng thái dùng `sh-select` để trống = "Tất cả" (có nút xoá); ô QA và nhóm cột QA ẩn khi dự án không QA
  (cột QA đã tick không gửi). "Sẽ xuất" = `count.total` — BE `count` đã lọc theo `statusCheck` / `statusQa` / `keyword`.
- Danh sách "File đã xuất" chỉ trong phiên trang (BE không có API liệt kê job). "Tải về" quá 15 phút → gọi lại job lấy link mới.
- Lỗi chung (48.18): `211` / `337` toast câu BE thay vì khung đỏ, `337` thêm `reload()` picker; HTTP 400 toast
  "Dữ liệu gửi lên không hợp lệ" ở `httpErrorInterceptor` (áp cho cả app).

- Xuất Google Sheet (Task 56, tab đổi tên "Xuất dữ liệu"): người dùng chốt **không điền sẵn** link Sheet của dự án (lệch 56.3 / 56.6) vì đó là nguồn import mà job xoá từ dòng bắt đầu trở xuống; BE cũng chặn Sheet chính của dự án (202), nên người dùng dán link Sheet khác.
  Link, tên sheet, dòng bắt đầu **giữ khi đổi đích** Excel ↔ Sheet; **đổi cặp dự án / loại** (khoá `projectId-documentTypeId`, tải lại cùng cặp thì không đổi) → link rỗng, xoá danh sách sheet, dòng bắt đầu về 1.
- "Lấy danh sách sheet": lỗi hiện đỏ dưới ô link (không toast, `isResponseSucceed(res, false)`), sửa link thì danh sách + tên sheet + lỗi bị xoá và lượt gọi dở bị huỷ; thành công tự chọn sheet đầu.
- Nút "Xuất Google Sheet" khoá khi: chưa có văn bản, đang tạo job, thiếu link / tên sheet / dòng bắt đầu, hoặc (người dùng chốt, thêm vào 56.8) **còn dòng "Đang ghi…" cùng link + tên sheet** trong danh sách. Bấm → hộp xác nhận xoá-rồi-ghi (không hoàn tác) → tạo job; dòng Sheet lên đầu "Đã xuất" kèm số văn bản lúc bấm, hỏi job mỗi `JOB_POLL_MS`. Nút Excel và Sheet dùng chung cờ `isCreating`.

Bẫy: `[class]` binding đi kèm `class` tĩnh trên cùng thẻ thì Angular gộp hai bên; đừng lặp class trạng thái ở cả hai.

Bẫy: khung lỗi dùng `whitespace-pre-line` phải đặt `{{ }}` sát thẻ mở / đóng, xuống dòng trong template sẽ thành dòng trống.

Bẫy: "Tải về" sau 15 phút mở link **sau** khi API trả, trình duyệt có thể chặn popup — bấm lại là mở (chưa kiểm).

## Chưa làm / chưa kiểm

- Task 63 đã kiểm trình duyệt 2026-11-27 (API thật + Sheet thật, dự án tạm ZZTEST63 đã dọn): link dự án điền sẵn + tự lấy sheet; dự án không Sheet vẫn chọn được Google, link sai → chữ đỏ không toast; dán Sheet tự do lấy được sheet; ghép (lệch tên → "Chưa chọn cột", "Ghi chú (cột thứ 9 / 10)", bấm Tiếp tục thiếu cột → dòng đỏ, một cột hai trường → hai dòng đỏ + khoá, tên cột lặp ghép thiếu → dòng đỏ); ghép đã lưu điền sẵn khi vào lại; cột bắt đầu = 3 bỏ cột A, B và xoá ô cột cố định; ghi đè chỉ chọn trường đã ghép; chạy thử Thêm mới 4 văn bản / 20 ô (dòng tên "null" bỏ — luật trước Task 67, nay tên file "null" là tên thật) + import thật 4 hồ sơ, 100 trang, ghép lưu DB.
  Chưa kiểm ở lượt này (phần còn lại xem kiểm bù Task 69.4 bên dưới): Excel (upload / job, MinIO đầy).
  Kiểm thêm 2026-11-27 sau Task 67 (API thật + Sheet thật, dự án tạm ZZTEST63 đã dọn): Thêm mới với ô rỗng / dấu cách / NBSP / U+200B, `abc`, `null`, `undefined`, ` NULL ` → chạy thử 4 ô = import ghi 4, DB lưu `null` / `undefined` / `NULL` nguyên văn, dòng xám mới hiện đúng; Ghi đè: không còn dòng vàng cảnh báo trùng tên, chạy thử 2 ô = ghi 2 (ô `null` ghi đè được, ô chỉ U+200B và ô bằng giá trị cũ không tính); Import tiếp giữ link + sheet.
- Kiểm bù Task 63 ngày 2026-11-27 (Task 69.4, API thật + Sheet thật, dự án tạm ZZTEST69 đã dọn, 2 file Sheet đã xoá khỏi Drive): Bổ sung chạy thử 5 ô = import ghi 5 ô (1 điền chỗ trống + 4 thêm), ô đã có dữ liệu không bị ghi đè; đổi cặp ở bước 2 → về bước 1, link = Sheet của dự án mới; "Import tiếp" giữ link + sheet, ghép đọc lại ("Đã ghép 2 / 2"); đổi sang sheet khác thì ghép cũ bỏ ("Đã ghép 0 / 2"); ghi đè sheet có 2 cột trùng tên: lựa chọn "Cot F (cột thứ 5)" / "(cột thứ 6)", ghép thiếu → dòng đỏ "Cột Cot F xuất hiện 2 lần…", ghi đè trường F2 chạy thử 2 ô và ghi đúng giá trị cột thứ 6, không đụng F1 (sau Task 67); tài khoản Leader thật chỉ thấy cặp mình là Leader, import Bổ sung chạy thử 2 ô + ghi 2 ô; "Tải file mẫu" ẩn với tài khoản thiếu `DocumentType.View`, có quyền thì hiện (API trả xlsx 200, chưa bấm tải trên trình duyệt). Chưa kiểm: đổi cặp ở bước 3, Excel (upload / job, MinIO đầy).
  Lưu ý: cách ghép lưu theo cặp (dự án, loại văn bản), không theo sheet: lưu ghép trên sheet này rồi chuyển sang sheet có tên cột khác thì phải chọn lại cột ("Đã ghép 0 / 2"). Đúng thiết kế "lần sau tự điền", không phải lỗi.
- Chưa kiểm trình duyệt (người dùng hẹn sau): upload thật, job xuất, popup tải về.
  Task 54 (2026-11-24) đã kiểm trên trình duyệt: khoá / mở tab và nguồn Google khoá khi dự án không Sheet, nhưng
  upload / chạy thử / job là response giả lập (MinIO đầy, `209`).
- Xuất Google Sheet đã kiểm trên trình duyệt 2026-11-25 (API thật + Gmail thật, dự án tạm ZZTEST56F đã dọn): chọn đích; link Sheet chính của dự án → lỗi đỏ dưới ô; sửa link xoá lỗi; lấy tên sheet tự chọn tab đầu; "Ghi vào" theo dòng bắt đầu; Huỷ hộp xác nhận không tạo job; Xuất → "Đang ghi…" → "Đã ghi" + link mở Sheet, nút khoá rồi mở lại; Sheet thật đúng (dòng phía trên giữ, dòng cũ phía dưới xoá); đổi đích qua lại giữ giá trị; đổi cặp làm lại khối đích; nút khoá khi link rỗng.
  Chưa kiểm: lỗi job (dòng "Lỗi" + câu BE), 337 / 211 / 326, xuất Excel sau khi sửa (code Excel không đổi ngoài cờ `target`), màn hẹp.
- Import xong không tải lại số "Sẽ xuất" của tab Xuất (đổi lọc / đổi cặp mới cập nhật).

> **Tiếp:** [12-man-cau-hinh.md](12-man-cau-hinh.md) — màn Cấu hình.
