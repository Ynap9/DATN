# Import: link Sheet gửi lên, ghép trường, đếm chạy thử

> **Phần 30/30** · trước: [29-xuat-google-sheet.md](29-xuat-google-sheet.md) · mục lục: [README.md](README.md)

Task 63A (2026-11-27, kèm [số trang](#số-trang-khi-import-thêm-mới) của Task 62) sửa [import hồ sơ](11-ho-so-import.md) cho giống 5sdb + soHoaSaoMai: đọc **đúng link người dùng dán**, lưu cách ghép
trường ↔ cột sheet theo dự án + loại văn bản, mọi import đều phải ghép đủ trường, số chạy thử đếm đúng số ô có dữ liệu sẽ ghi. Không migration,
không key quyền mới, không mã lỗi mới; 3 API mới và body import đổi (FE cũ gọi append / blank Google sẽ nhận `400` tới khi Task 63 FE xong).

## Đọc link gửi lên

- 4 API append / blank (chạy thử + import) đổi body sang `ImportSheetSubmissionDto` = body cũ + `url`, `sheetName` (overwrite đã có). Cả 3 kiểu
  đọc đúng `url` + `sheetName`, không còn đọc `Project.UrlGoogleSheetSync`. Dán lại Sheet của chính dự án vẫn được; dự án không gắn Sheet vẫn import
  Google được (không còn `348`, mã vẫn nằm trong `ErrorCodes` nhưng không nơi nào ném).
- `url` trống → `400` "Chưa nhập link Google Sheet"; `sheetName` trống → `400` "Chưa chọn sheet" (kiểm trước khi đọc). Link không đọc được →
  `202` / `203` / `204` như trước.
- **Chặn Sheet đồng bộ của dự án khác → `202`** "Google Sheet đang là Sheet đồng bộ của dự án khác, không dùng để nhập" (ở 3 kiểu + `sheet-headers`):
  nếu không, Leader dự án này đọc và nhập được dữ liệu dự án khác qua tài khoản dịch vụ. Ngoài spec; Sheet của chính dự án không bị chặn.
  Kiểm chạy tuần tự ngay sau `337`, **trước** lúc đọc sheet song song với query cấu hình (kết nối DB tắt MARS, hai query không chạy cùng lúc).
- Audit 3 kiểu ghi `SheetUrl`, `SheetName` (append / blank: trong `NewValues`) và mô tả " từ Google Sheet {tên sheet}" (overwrite trước đây ghi null).
- Append: message "Dữ liệu thêm mới" = số ô **có dữ liệu** (`NonEmptyCellCount`), không còn kèm số dòng ghi DB; audit giữ `NewDataCount` và thêm `NonEmptyCellCount`.

## Bắt đầu đọc từ cột (`startColumn`)

`ImportSubmissionDto.StartColumn` (tính từ 1, mặc định 1) có ở cả 3 kiểu, Google lẫn Excel (job mang theo trong body), và ở body `match` / lưu ghép.
Tiêu đề luôn ở dòng 1; `ApplyStartColumn` đổi header các cột **trước** cột bắt đầu thành rỗng nhưng giữ nguyên vị trí cột, nên cột cố định (Tên file,
Link, Hộp, Hồ sơ) và trường ghép chỉ tìm được từ cột bắt đầu. `< 1` → `400` "Cột bắt đầu phải từ 1"; lớn hơn số cột → `400` "Sheet chỉ có n cột".
Không lưu vào DB (mỗi lượt nhập lại). 5sdb dùng số này làm **dòng** tiêu đề và chỉ áp ở bước ghép: ĐATN không chép lỗi đó.

## Ba API mới (`api/construction/submission/import/...`)

Cùng quyền `Function.Construction.Submission.Import` + Admin / Leader của dự án (`337`) + dự án importable + loại văn bản thuộc dự án như import.

| API | Việc |
|---|---|
| `GET sheet-headers?projectId&documentTypeId&url` | Trả `[{ sheetName, headers }]` (header dòng 1) qua `GetSheetHeadersAsync`; thay `document-field/sheet-header` (cần quyền `DocumentField.Create`) |
| `POST field-mapping/match` | Body `{ projectId, documentTypeId, headers, startColumn }`. Trả **mọi** trường cấu hình của cặp theo `OrderTableView`, `Id`: `{ documentFieldId, name, fieldImport, matchedHeader, matchedColumnIndex }`. Không ghi DB |
| `PUT field-mapping` | Body `{ projectId, documentTypeId, headers, startColumn, fields: [{ documentFieldId, fieldImport, columnIndex }] }`; trả cùng DTO với `matchedHeader = ""` |

- **Luật khớp** (`SubmissionImportDomainService.MatchFieldHeaders`): duyệt theo `OrderFieldImport` rồi `DocumentFieldId` như import, mỗi header dùng tối đa
  một lần: (1) header khớp `fieldImport` đang lưu (`IsFieldImportMatch`: không phân biệt hoa thường, gộp khoảng trắng); (2) không có → header khớp **tên trường**;
  (3) không có → `""`, `matchedColumnIndex = null`. Trả đúng chữ header trên sheet; `matchedColumnIndex` là vị trí trong mảng `headers` FE gửi để chọn đúng một
  trong các cột trùng tên. Hai trường cùng tên cột: cột chỉ gán cho trường đứng trước.
- **Luật lưu** (một transaction, khoá `LockImportAsync` như import — import đang chạy thì chờ ≤ 5 giây, hết giờ `310`): body phải có **đủ mọi trường** của cặp, mỗi trường có
  `fieldImport` + `columnIndex` (không có "Không import"). Thứ tự kiểm: `documentFieldId` lặp → `400` "Trường gửi trùng"; trường không thuộc cặp → `311`; thiếu trường /
  `fieldImport` rỗng / `columnIndex` null hoặc âm → `400` "Chưa chọn cột cho trường: {tên}, …"; `columnIndex` trước cột bắt đầu → `400`; hai trường cùng `columnIndex` →
  `400` "Cột {x} được ghép cho nhiều trường: {tên 1}, {tên 2}" (cùng chữ nhưng khác cột thì được); tên cột trùng k lần trên `headers` mà ghép ít hơn k trường → `309` như import.
- Lưu `FieldImport` (Trim) + `OrderFieldImport = columnIndex + 1` vào `ProjectDocumentTypeDocumentField` của **dự án này**, không đụng `DocumentField.FieldImport` gốc
  hay dự án khác cùng loại. Vị trí cột đổi theo sheet dù tên giữ nguyên nên FE gửi đủ mọi trường mỗi lần qua bước ghép. Header trùng tên gán đúng theo thứ tự cột
  nhờ `OrderFieldImport` (`ResolveMappedColumns` lấy cột khớp đầu tiên chưa dùng).
- Audit `Ghép cột import cho dự án` (`Update`), Old / New = `{ DocumentFieldId, FieldImport, OrderFieldImport }` **chỉ trường có đổi** (`UpdateFieldImport` trả `false` khi
  tên cột và thứ tự đều giữ nguyên, so đúng chữ). Không trường nào đổi → không audit, không `SaveChanges`, vẫn `200`.

## Ghép đủ trường và đếm chạy thử

- **Mọi import** (3 kiểu, Google + Excel, chạy thử + job) kiểm trong `LoadProjectSheetAsync` / `LoadOverwriteSheetAsync`: mọi trường của cặp phải có `FieldImport` khác rỗng và
  tìm được cột trên sheet; thiếu → `307` "Chưa ghép đủ cột cho trường: {tên}, …. Hãy ghép ở bước Ghép trường." (trước đây trường thiếu cột bị bỏ qua êm). Cặp không có trường
  nào → `400` "Loại văn bản chưa có trường nào". Overwrite cũng chạy `309` và kiểm này (trước chỉ kiểm cột ghi đè).
- **Ô không có dữ liệu** (`SubmissionImportDomainService.NormalizeImportValue`, một hàm duy nhất): Trim; rỗng, "null", "undefined" (không phân biệt hoa thường) → `""`. Mọi chỗ đọc ô
  (trường, Tên file, Link, Hộp, Hồ sơ) qua `GetImportCell`. Dòng có tên file "null" bị bỏ như dòng trống; link "null" → `314`. Ô rỗng vẫn lưu `Data = ""` (giữ dòng cho blank
  điền sau), không bao giờ lưu chữ "null". `SubmissionData.FillIfBlank` / `Overwrite` dùng cùng luật: ô DB đang "null" / "undefined" là trống.
- **Số chạy thử = số import ghi** (cùng kế hoạch, chỉ đếm ô có dữ liệu — số này dùng để tính tiền): append = ô trường đã ghép có giá trị ở hồ sơ sẽ tạo; blank = ô sheet có dữ liệu
  sẽ điền vào trường đang trống **hoặc chưa có dòng `SubmissionData`** (5sdb quên trường hợp sau); overwrite = ô có giá trị **khác** giá trị đang có (5sdb đếm cả ô bằng giá trị cũ).

## Số trang khi import Thêm mới

Task 62 (2026-11-27): số trang chỉ lấy từ cột "Số trang" trong sheet, **không** tải PDF, không job, không migration (cột `Submission.PageCount` đã có từ `8f012a9`).

- `pageCountColumn` (tuỳ chọn) chỉ ở **append**, cả Google lẫn Excel. Rỗng = hồ sơ mới `PageCount = null`. Tên cột nhập mà sheet không có → `307`; blank / overwrite gửi lên thì BE bỏ qua (không đọc, không `307`).
- Đọc ô (`SubmissionImportColumns.GetPageCount`): chỉ nhận toàn chữ số hoặc dấu nghìn đúng nhóm 3 chữ số (`1.234`, `1,234`); `12,5` / `12.5` là số lẻ nên sai. Kết quả phải từ 1 đến `PAGE_COUNT_MAX` (100000). Ô trống hoặc sai → `null`, hồ sơ vẫn tạo, **không** báo lỗi.
- Hồ sơ đã có (trùng khoá) bị bỏ qua như cũ nên không đổi số trang. Verify trả thêm `totalPageCount` (0 khi không ghép cột); message import nối ", Có số trang: N văn bản" khi N > 0; audit append thêm `PageCountCount`.
- Danh sách dự án có `totalPages` ([25](25-xoa-du-an-danh-sach-cap.md)). Để sau: xuất Excel / Google Sheet chưa có cột Số trang; Task 22B (OCR) và màn tải PDF sẽ ghi số trang thật qua `SetPageCount`.
- Đã kiểm 2026-11-27, API thật trên DB dev (`ZZTEST62`, Sheet qua API dự án, đã dọn): 13/13 đạt. 14 dòng mẫu (`1234`, `1.234`, `1,234`, `5`, `100000` nhận; `abc`, `0`, `-3`, `12,5`, `12,5 trang`, `100001`, `12.5`, `null`, ô trống → null); tên cột sai `307`; import lại với số khác giữ số cũ; blank / overwrite không đổi; `totalPages` Admin 103.707, Data chỉ văn bản được giao, xoá mềm không cộng; 5.000 dòng import 7,5 s (có cột) / 7,6 s (không cột), verify 2,2–3,1 s mỗi kiểu. ⚠️ Chưa kiểm import Excel (MinIO đầy, `209`) và 100.000 dòng (đo 5.000 để khỏi ghi nặng DB dùng chung).

## Khác 5sdb

| | 5sdb | ĐATN |
|---|---|---|
| Append / blank đọc | `project.UrlGoogleSheetSync` dù FE dán link khác | Đúng link người dùng gửi, cả 3 kiểu |
| Ô "null" / "undefined" | Đếm là có dữ liệu | Coi là trống |
| So header | Đúng chữ | Khớp lỏng, thử tên cột đã lưu rồi tên trường |
| "Bắt đầu đọc từ cột" | BE dùng làm dòng tiêu đề, chỉ ở bước ghép | Đúng nghĩa cột, áp ở ghép, chạy thử và import |
| Ghi đè | FE không gửi cột Tên file / Hồ sơ / Hộp nên BE không tìm được văn bản | Vẫn bắt chọn Tên file |

Giữ khác chủ đích: Hộp / Hồ sơ / Link (khi bổ sung) không bắt buộc; có thêm nguồn Excel.

## Đã kiểm thực tế

Ngày 2026-11-27, gọi API thật trên DB dev (`ZZTEST63A`, 5 dự án + 2 user Leader / Data, Sheet tạo qua API dự án; file Sheet đã xoá khỏi Drive, dữ liệu đã dọn) — **39/39 đạt** (Google Sheet):
dự án không gắn Sheet import append từ link khác (3 hồ sơ, 5 ô, DB không có "null" / "undefined"); dán link khác với dự án có Sheet và dán lại Sheet của chính nó; blank = 4 (ô "null" trong DB coi là
trống, trường chưa có dòng được thêm); overwrite = 1; audit 3 kiểu; `400` url / sheetName trống ở 6 API; `203`; `sheet-headers` Admin / Leader / Data `403`; `202` ở `sheet-headers` và 3 kiểu;
`match` (gộp khoảng trắng, hai trường cùng tên cột, thử tên trường); lưu ghép (audit đúng 2 dòng, gửi lại không audit, `311`, trùng, thiếu, cùng cột, `309`, hai cột "Ghi chu" import đúng F→trường 1, H→trường 2);
`307` thiếu cột ở 3 kiểu; `startColumn` (bỏ cột trước, `309` biến mất, `307` cột cố định, `400` hai đầu, `match` / lưu ghép).

Lưu ý: **Chưa kiểm:** import Excel (MinIO dùng chung đầy ổ, `209`; Excel dùng chung `Load*SheetAsync` nên cùng luật), lưu ghép lúc import cùng cặp đang chạy (chờ khoá), thiếu quyền Leader dự án khác (`337`).
Lưu ý: Chạy thử Excel / Google bằng body cũ (thiếu `startColumn`) vẫn đúng vì mặc định 1; `PUT field-mapping` không gửi `headers` bị `400` "Sheet chỉ có 0 cột".
Lưu ý: `GET sheet-headers` với `url` rỗng bị model binding chặn `400` (HTTP) trước khi tới kiểm của service; kiểm `400` "Chưa nhập link Google Sheet" chỉ chạy với body POST.

> **Tiếp:** hết — quay lại [README.md](README.md).
