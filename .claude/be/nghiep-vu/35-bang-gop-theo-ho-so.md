# Bảng gộp theo hồ sơ khi xuất (Task 76)

> Cập nhật 2026-11-28. Tuỳ chọn `includeProfileSummary` của hai job xuất ([14](14-ho-so-xoa-xuat.md#xuất-excel-chạy-nền), [29](29-xuat-google-sheet.md)):
> thêm sheet / tab "Gom hồ sơ" là mục lục hồ sơ theo TT05, mỗi dòng một hồ sơ (Hộp + Hồ sơ) của tập văn bản đang xuất. Module đóng gói (M2) dùng lại hàm gộp.
> Nguồn luật gộp: soHoaSaoMai `projectSheetSync.service.js` dòng 274–510 (`GOM_FIELD_SOURCES`, `aggregateGomValues`, `parseLooseDate`, `buildGomHoSoMatrix`);
> **không** làm phần "giữ cột nhập tay" của nguồn đó (ĐATN ghi đè tab). Không đổi schema, không key quyền, không route mới.

## Tham số và nơi tính

| Việc | Chi tiết |
|---|---|
| Tham số | `ExportSubmissionDto.IncludeProfileSummary` (`?includeProfileSummary=`), mặc định `false` = hành vi cũ, file không đổi. Dùng ở `POST export-excel/job` và `POST export-sheet/job` |
| Hàm gộp | `SubmissionProfileSummaryDomainService.BuildProfileSummary(fieldNames, submissions)` (static thuần, không DB / Google), hằng ở `ProfileSummaryConst` |
| Dữ liệu vào | `ISubmissionQuery.FindProfileSummarySource(filter)`: cùng bộ lọc `ApplyFilter` với `FindExportTable` (đúng tập văn bản đang xuất, không lấy thêm) nhưng đọc **mọi trường** của cặp dự án + loại văn bản, không phụ thuộc `columns` đã chọn |

## Luật gộp

- Nhóm theo (Hộp, Hồ sơ) sau trim, không phân biệt hoa thường; dòng giữ cách viết của văn bản đầu tiên; sắp tự nhiên theo Hộp rồi Hồ sơ ("2" trước "10", chuỗi trống đứng đầu).
  Văn bản trống **cả** Hộp lẫn Hồ sơ bỏ qua; chỉ trống một trong hai thì **vẫn có dòng**, ô trống (chốt 2026-11-28).
- Cột cố định: Hộp số · Hồ sơ số · Thời gian bắt đầu · Thời gian kết thúc · Tổng số văn bản · Số trang. Số trang = tổng `PageCount` của văn bản có `PageCount > 0`;
  không văn bản nào có thì cộng trường tên "Số trang"; vẫn không có thì ô trống.
- Thời gian = ngày nhỏ nhất / lớn nhất đọc được từ trường có tên chứa "ngày" hoặc "thời gian" (bỏ dấu, không phân biệt hoa thường), `dd/MM/yyyy`. Đọc được `26/04/2024`,
  `5-11-2024`, `17 tháng 05 năm 2024`, `Hà Nội, ngày 12 tháng 11 năm 2024` (ngày nằm giữa chữ), `2024-04-26`; năm 1900–2100, ngày không có thật (`31/02`) hoặc không đọc được thì bỏ.
  Mỗi giá trị chỉ lấy **ngày đầu tiên** tìm thấy.
- Cột lấy từ trường: chỉ thêm khi metadata có trường trùng tên (bỏ dấu, hoa thường, khoảng trắng) với tên cột hoặc tên phụ, đúng thứ tự `ProfileSummaryConst.FIELD_COLUMNS`:
  Mã hồ sơ (Mã HS), Tiêu đề hồ sơ (Tên hồ sơ), Thời hạn bảo quản, Chế độ sử dụng, Ngôn ngữ, Từ khoá (Từ khóa) nối, Số tờ (Số lượng tờ) cộng, Tình trạng vật lý, Ký hiệu thông tin,
  Mức độ tin cậy, Mã hồ sơ gốc giấy, Chế độ dự phòng, Tình trạng dự phòng; còn lại là "phổ biến". "Ghi chú" không gộp. Metadata không có trường nào trùng thì chỉ 6 cột cố định.
- Cách gộp: "phổ biến" = giá trị gặp nhiều nhất (bằng nhau lấy giá trị gặp trước, so khớp chính xác sau trim, **có** phân biệt hoa thường vì spec chỉ nêu không phân biệt cho "nối");
  "cộng" = cộng số đọc được (`1.234` = 1234, `12,5`, `12 trang`; có cả `,` và `.` thì dấu đứng sau là thập phân), in dạng số quốc tế bỏ số 0 thừa (`1246.5`), không đọc được số nào thì trống;
  "nối" = tách theo `,` và `;`, bỏ trùng không phân biệt hoa thường, nối `"; "`.

## Excel (76.5) và Google Sheet (76.6)

- Excel: `ExportSubmissionJob` → `ExportExcelFromJob` dựng 2 sheet qua `IExcelExternal.CreateMultiSheetTable` (`ExcelTableDto`): sheet dữ liệu như cũ rồi "Gom hồ sơ"
  (tiêu đề in đậm, cố định dòng đầu, cột số canh phải: Tổng số văn bản, Số trang và cột "cộng"). 0 hồ sơ → sheet chỉ có tiêu đề. Cờ tắt đi nhánh `CreateTable` cũ.
- Google Sheet: sau khi ghi tab dữ liệu, `IGoogleSheetExternal.ReplaceTableAsync` ghi tab "Gom hồ sơ" **thay thế**: chưa có tab thì tạo (`AddSheet`), có rồi thì xoá hết
  nội dung (kể cả ô nhập tay) rồi ghi lại từ A1, nên chạy hai lần không cộng dồn. Tìm tab không phân biệt hoa thường (Google không cho hai tab chỉ khác hoa thường). Lỗi ghi tab
  gộp → job lỗi như `WriteTableAsync` (`CoreErrorAuthorizeDataSourceFailed` / `CoreErrorUpdateDataSourceFailed`, 429 / 5xx ném cho Hangfire), tab dữ liệu đã ghi giữ nguyên.
- Chặn tab chính trùng tên: bật cờ mà `sheetName` là "Gom hồ sơ" (trim, không phân biệt hoa thường) → `400` "Tab Gom hồ sơ dành cho bảng gộp, hãy chọn tab khác"
  (`SubmissionDomainService.EnsureExportSheetNotProfileSummary`, gọi trước khi đọc tên tab Google). Tắt cờ thì chọn tab đó vẫn được.

## FE (76.7)

Checkbox "Kèm bảng gộp theo hồ sơ" dưới phần chọn cột, chi tiết ở [../../fe/architecture/11-man-nhap-xuat.md](../../fe/architecture/11-man-nhap-xuat.md). BE không trả cờ ở job; FE giữ cờ trong dòng "Đã xuất" của phiên trang.

## Giới hạn đã biết

- Lưu ý: `GET export-excel` (tải ngay, không qua job) dùng chung `ExportSubmissionDto` nên nhận `includeProfileSummary` nhưng **không** thêm sheet; spec chỉ chốt cho hai API job.
- Lưu ý: Metadata có tên đúng "Gom hồ sơ" trùng tên sheet dữ liệu với sheet gộp, ClosedXML ném lỗi và job Excel báo lỗi. Spec không có luật cho ca này; đổi tên metadata hoặc tắt tuỳ chọn.
- Mọi ô của bảng gộp là chuỗi (Excel lưu dạng chữ, chỉ canh phải cột số; Google ghi `RAW`).

## Đã kiểm thực tế — 2026-11-28

- Hàm thuần (console tạm, 34/34): 5 kiểu ngày + biên (1899, 2101, 31/02, rác), đọc số (`1.234`, `12,5`, `12 trang`, `1.234,5`, `1,234.5`, `12.5`), nhóm / sắp tự nhiên, `PageCount` ưu tiên hơn trường "Số trang",
  trường "Số trang" cộng khi thiếu, hoà phiếu "phổ biến" lấy giá trị gặp trước, tên phụ, metadata không trùng tên chỉ 6 cột, 0 văn bản.
- Excel thật (BE + Hangfire + s3rver giả MinIO + DB LocalDB riêng `ZZTEST76`, đã dọn): bật cờ ra 2 sheet, 4 dòng đúng thứ tự HS1 → HS2 → HS10 → H2/HS10, văn bản trống Hộp / Hồ sơ bị bỏ, HS1 có thời gian
  12/11/2023–17/05/2024, số trang 10, mã hồ sơ phổ biến, từ khoá nối bỏ trùng, số tờ 1246.5; tiêu đề đậm, cố định dòng đầu, cột E / F / J canh phải; 0 hồ sơ chỉ có tiêu đề; tắt cờ chỉ 1 sheet như cũ.
- Luật 400 chặn tab chính: `Gom hồ sơ` và ` gom HỒ SƠ ` bị chặn khi bật cờ; tắt cờ hoặc tab khác không bị chặn bởi luật này (4/4).
- Trình duyệt: tick checkbox, xuất Excel, dòng "Đã xuất" có chip "Kèm gom hồ sơ", file tải về có đúng 2 sheet; chuyển sang đích Google Sheet checkbox vẫn tick.

- Google thật (2026-11-28, kiểm bù Task 72.3, đích là Sheet của dự án tạm đã xoá mềm nên không bị luật 202 chặn; file Sheet đã xoá khỏi Drive): xuất 2 lần với cờ bật → lần 1 tạo tab "Gom hồ sơ"
  (B1/H1 5 văn bản 9 trang, B2/H2 3 văn bản, B3/H3 5 văn bản 15 trang), ghi tay "GHI TAY" vào ô H12 giữa hai lần, lần 2 tab vẫn 4 dòng và ô tay biến mất (ghi đè, không cộng dồn); chọn tab chính "Gom hồ sơ" khi bật cờ → 400.

Lưu ý: Chưa bấm hộp xác nhận xuất Sheet (câu "Tab Gom hồ sơ cũng bị xoá") trên trình duyệt; luồng Sheet kiểm bằng API.

> **Tiếp:** [README.md](README.md) — mục lục nghiệp vụ.
