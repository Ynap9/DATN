# Màn Nhập / Xuất dữ liệu

> **Phần 11/12** · trước: [10-man-operation.md](10-man-operation.md) · mục lục: [README.md](README.md)

Task 48 (cập nhật 2026-11-23). Admin / Leader của dự án nhập văn bản từ Google Sheet của dự án hoặc file Excel
(wizard 4 bước), và xuất Excel theo bộ lọc + cột thêm (job chạy nền). **Chưa kiểm trên trình duyệt**, mới build sạch.

## Route và file

| Thứ | Chỗ |
|---|---|
| Route | `/admin/construction/data-transfer`, `permissionGuard` = có `Function.Construction.Submission.Import` **hoặc** `.Export`, `canDeactivate` inline trong `construction.routes.ts` |
| Menu | "Nhập / Xuất dữ liệu" dưới "Metadata"; `permission` là **mảng key** (`AppMenu.canView`: có một là hiện) |
| Màn | `data-transfer/data-transfer.component.*`: picker cặp trong `app-page-header`, tab theo quyền, "Dự án … · N văn bản" |
| Wizard Nhập | `import-wizard/` giữ trạng thái + gọi API; bước con `import-source-step/` (nguồn, upload), `import-column-step/` (ghép cột), `import-verify-step/` (chỉ hiển thị); bước 2 Kiểu import viết thẳng trong wizard |
| Tab Xuất | `export-panel/` |
| Hằng của màn | `data-transfer.constants.ts`: tên bước, thẻ nguồn / kiểu, `IImportColumnMapping`, `isColumnMappingComplete`, `getVerifyView`, `TOAST_ERROR_CODES`, `IExportJobRow`, `formatNumber` |
| Hằng chung | `IMPORT_KIND`, `IMPORT_SOURCE`, `BACKGROUND_JOB_STATUS`, `EXPORT_COLUMNS`, `IMPORT_EXCEL_MAX_BYTES`, `JOB_POLL_MS`, `EXPORT_DOWNLOAD_URL_MS` ở `submission.constants.ts` |
| API | `SubmissionService`: `verifyImport` / `importSheet`, `uploadImportExcel`, `verifyImportExcel` / `createImportExcelJob` / `findImportJob`, `createExportJob` / `findExportJob`; `DocumentFieldService.getSheetHeaders`, `dropdown` |

## Quyết định

- Picker dùng chung với `viewerRoles = ['Admin', 'Leader']`, **không khoá picker** khi đang import (người dùng chốt, lệch 48.4).
  Đổi cặp giữa chừng → wizard huỷ subscription (lượt tải file, chạy thử, import, hỏi job) và về bước 1; import Google có
  thể vẫn chạy xong ở BE nhưng màn không hiện kết quả. Tab vẫn khoá khi đang import.
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
- Tab Xuất: lọc trạng thái dùng `sh-select` để trống = "Tất cả" (có nút xoá); ô QA và nhóm cột QA ẩn khi dự án không QA
  (cột QA đã tick không gửi). "Sẽ xuất" = `count.total` — BE `count` đã lọc theo `statusCheck` / `statusQa` / `keyword`.
- Danh sách "File đã xuất" chỉ trong phiên trang (BE không có API liệt kê job). "Tải về" quá 15 phút → gọi lại job lấy link mới.
- Lỗi chung (48.18): `211` / `337` toast câu BE thay vì khung đỏ, `337` thêm `reload()` picker; HTTP 400 toast
  "Dữ liệu gửi lên không hợp lệ" ở `httpErrorInterceptor` (áp cho cả app).

⚠️ Bẫy: `[class]` binding đi kèm `class` tĩnh trên cùng thẻ thì Angular gộp hai bên; đừng lặp class trạng thái ở cả hai.

⚠️ Bẫy: khung lỗi dùng `whitespace-pre-line` phải đặt `{{ }}` sát thẻ mở / đóng, xuống dòng trong template sẽ thành dòng trống.

⚠️ Bẫy: "Tải về" sau 15 phút mở link **sau** khi API trả, trình duyệt có thể chặn popup — bấm lại là mở (chưa kiểm).

## Chưa làm / chưa kiểm

- Chưa kiểm trình duyệt (người dùng hẹn sau): upload thật, chạy thử / import cả 3 kiểu × 2 nguồn, job xuất, popup tải về.
- Import xong không tải lại số "Sẽ xuất" của tab Xuất (đổi lọc / đổi cặp mới cập nhật).

> **Tiếp:** [12-man-cau-hinh.md](12-man-cau-hinh.md) — màn Cấu hình.
