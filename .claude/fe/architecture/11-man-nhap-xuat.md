# Màn Nhập / Xuất dữ liệu

> **Phần 11/13** · trước: [10-man-operation.md](10-man-operation.md) · mục lục: [README.md](README.md)

Task 48 (cập nhật 2026-11-25). Admin / Leader của dự án nhập văn bản từ Google Sheet của dự án hoặc file Excel
(wizard 4 bước), và xuất theo bộ lọc + cột thêm ra file Excel hoặc ghi vào một Google Sheet (Task 56, job chạy nền).
Nhập chưa kiểm trên trình duyệt; xuất Google Sheet đã kiểm (xem cuối file).

## Route và file

| Thứ | Chỗ |
|---|---|
| Route | `/admin/construction/data-transfer`, `permissionGuard` = có `Function.Construction.Submission.Import` **hoặc** `.Export`, `canDeactivate` inline trong `construction.routes.ts` |
| Menu | "Nhập / Xuất dữ liệu" dưới "Metadata"; `permission` là **mảng key** (`AppMenu.canView`: có một là hiện) |
| Màn | `data-transfer/data-transfer.component.*`: picker cặp trong `app-page-header`, "Dự án … · N văn bản" là `description` của header (dưới tiêu đề), tab theo quyền |
| Wizard Nhập | `import-wizard/` giữ trạng thái + gọi API; bước con `import-source-step/` (nguồn, upload), `import-column-step/` (ghép cột), `import-verify-step/` (chỉ hiển thị); bước 2 Kiểu import viết thẳng trong wizard |
| Tab Xuất dữ liệu | `export-panel/`: thẻ chọn đích (Excel / Google Sheet), khối đích Sheet, danh sách "Đã xuất" chung cho cả hai đích |
| Hằng của màn | `data-transfer.constants.ts`: tên bước, thẻ nguồn / kiểu, `IImportColumnMapping`, `isColumnMappingComplete`, `getVerifyView`, `TOAST_ERROR_CODES`, `EXPORT_TARGET` / `EXPORT_TARGET_OPTIONS`, `IExportJobRow`, `formatNumber` |
| Hằng chung | `IMPORT_KIND`, `IMPORT_SOURCE`, `BACKGROUND_JOB_STATUS`, `EXPORT_COLUMNS`, `IMPORT_EXCEL_MAX_BYTES`, `JOB_POLL_MS`, `EXPORT_DOWNLOAD_URL_MS` ở `submission.constants.ts` |
| API | `SubmissionService`: `verifyImport` / `importSheet`, `uploadImportExcel`, `verifyImportExcel` / `createImportExcelJob` / `findImportJob`, `createExportJob` / `findExportJob`, `getExportSheetNames` / `createExportSheetJob` / `findExportSheetJob`; `DocumentFieldService.getSheetHeaders`, `dropdown` |

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
- Ghép cột: `p-autocomplete` gợi ý header, gõ tay được. Google đọc header bằng `sheet-header` chỉ khi có
  `Function.Construction.DocumentField.Create`; thiếu quyền / lỗi → dòng xám "Không đọc được tên cột…". Header trùng
  "Tên file / Link / Hồ sơ / Hộp" (không phân biệt hoa thường) điền sẵn vào ô **đang hiện và đang trống**.
- Bước 3 → 4 **luôn** qua chạy thử; Quay lại từ bước 4 xoá kết quả (BE không lưu chạy thử). Lỗi chạy thử → về bước 3, khung
  đỏ `whitespace-pre-line`, cao tối đa 240 px. Lỗi import / job → bước 4, nút chính thành "Thử lại" (= chạy thử lại).
- Import Google đồng bộ: khoá nút + tab, hỏi khi rời trang (`canDeactivate` + `beforeunload`). Excel tạo job rồi hỏi
  `findImportJob` mỗi `JOB_POLL_MS`, không hỏi khi rời. Xong → khối "Import xong", màn cha tải lại số văn bản.
- Body theo 48.15: ô trống gửi `''`; ghi đè gửi `linkColumnImport = ''`; Excel ghi đè **không** có `url`.
- Ô "Cột Số trang" (Task 62, tuỳ chọn) chỉ hiện ở kiểu Thêm mới, header "Số trang" điền sẵn; kiểu khác body gửi `pageCountColumn = ''`. Chạy thử Thêm mới có thêm ô số phụ "Văn bản có số trang" chỉ khi đã nhập cột. Chưa kiểm trình duyệt.
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

- Chưa kiểm trình duyệt (người dùng hẹn sau): upload thật, chạy thử / import cả 3 kiểu × 2 nguồn, job xuất, popup tải về.
  Task 54 (2026-11-24) đã kiểm trên trình duyệt: khoá / mở tab và nguồn Google khoá khi dự án không Sheet, nhưng
  upload / chạy thử / job là response giả lập (MinIO đầy, `209`).
- Xuất Google Sheet đã kiểm trên trình duyệt 2026-11-25 (API thật + Gmail thật, dự án tạm ZZTEST56F đã dọn): chọn đích; link Sheet chính của dự án → lỗi đỏ dưới ô; sửa link xoá lỗi; lấy tên sheet tự chọn tab đầu; "Ghi vào" theo dòng bắt đầu; Huỷ hộp xác nhận không tạo job; Xuất → "Đang ghi…" → "Đã ghi" + link mở Sheet, nút khoá rồi mở lại; Sheet thật đúng (dòng phía trên giữ, dòng cũ phía dưới xoá); đổi đích qua lại giữ giá trị; đổi cặp làm lại khối đích; nút khoá khi link rỗng.
  Chưa kiểm: lỗi job (dòng "Lỗi" + câu BE), 337 / 211 / 326, xuất Excel sau khi sửa (code Excel không đổi ngoài cờ `target`), màn hẹp.
- Import xong không tải lại số "Sẽ xuất" của tab Xuất (đổi lọc / đổi cặp mới cập nhật).

> **Tiếp:** [12-man-cau-hinh.md](12-man-cau-hinh.md) — màn Cấu hình.
