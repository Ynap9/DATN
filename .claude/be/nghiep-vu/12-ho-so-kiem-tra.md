# Hồ sơ — xem, lọc, kiểm tra

> **Phần 12/22** · trước: [11-ho-so-import.md](11-ho-so-import.md) · mục lục: [README.md](README.md)

Sau khi import ([11-ho-so-import.md](11-ho-so-import.md)), người kiểm tra mở màn hình hồ sơ của **một dự án + một loại văn bản**:
xem bảng, lọc, mở từng hồ sơ (PDF bên trái qua `proxy-file`, form trường bên phải), sửa dữ liệu rồi bấm **Kiểm tra**.
Phạm vi xem / kiểm do Vận hành quyết định ([Phạm vi và phân việc](#phạm-vi-và-phân-việc)): nhân viên chỉ thấy văn bản mình giữ. Xoá hồ sơ và xuất Excel nằm ở [14-ho-so-xoa-xuat.md](14-ho-so-xoa-xuat.md).

## Các chức năng

| Endpoint (`api/construction/submission/...`) | Quyền | Việc |
|---|---|---|
| `GET paging` | `Function.Construction.Submission.View` | Danh sách phân trang + lọc; `customData` = các cột của bảng |
| `GET {id}` | `...Submission.View` | Chi tiết hồ sơ + form trường; `includePosition = true` thì có `position` / `totalItems` |
| `GET {id}/next` | `...Submission.View` | Hồ sơ kế tiếp trong bộ lọc (nút "Sau"), trả chi tiết như `GET {id}` |
| `GET {id}/previous` | `...Submission.View` | Hồ sơ liền trước trong bộ lọc (nút "Trước"), trả chi tiết như `GET {id}` |
| `GET count` | `...Submission.View` | `total`, `totalChecked`, `totalNotChecked`, `totalReturned` theo bộ lọc (`statusCheck` trống: `total` = tổng ba số) |
| `PUT check` | `Function.Construction.Submission.Check` | Sửa dữ liệu + chuyển `CHECKED` + ghi lịch sử (`id` hồ sơ nằm trong body) |
| `GET {id}/logs` | `...Submission.View` | Lịch sử kiểm tra, mới nhất trước |

Route có ràng buộc `{id:int}` để `paging`, `count`, `proxy-file` không khớp nhầm route `{id}`.

## Bộ lọc chung

Năm API đọc (và hai API QA ở [13-ho-so-qa.md](13-ho-so-qa.md)) nhận cùng bộ lọc query: `projectId`, `documentTypeId` (bắt buộc),
`statusCheck` (`0` chưa kiểm tra, `1` đã kiểm tra, `2` trả lại; bỏ trống = tất cả), `statusQa` (`0` / `1` / `2`, bỏ trống = tất cả), `keyword`,
`documentFieldId1` + `fieldValue1`, `documentFieldId2` + `fieldValue2`. Mọi API lọc qua **một** hàm `SubmissionQuery.ApplyFilter`,
nên `count.total` luôn bằng `paging.totalItems`, và vị trí, hồ sơ liền kề cũng khớp với danh sách.

- Thứ tự: `Submission.Id` tăng dần (= thứ tự import).
- `keyword`: chứa trong tên file, Hồ sơ hoặc Hộp; không phân biệt hoa thường (theo collation DB).
- `qaTicketId` (Task 18C): chỉ văn bản có dòng `SubmissionUser` Role `QA` thuộc phiếu đó. Người gọi phải xem được phiếu (luật ở
  [18-qa-cham-mau.md](18-qa-cham-mau.md#18c--phiếu-qa)), không thì `327`; áp cho cả paging, count, next / previous, chi tiết, tree, xuất Excel.
  `assignmentTicketId` (Task 25, mở từ "Phiếu của tôi"): chỉ văn bản có `SubmissionUser` Role `Data` thuộc phiếu đó; không kiểm người gọi.
- Cặp trường / giá trị: hồ sơ phải có `SubmissionData` của trường đó với `Data` **bằng đúng** giá trị đã trim. Thiếu một vế thì bỏ
  qua cặp đó. Có hai cặp thì phải thoả cả hai.
- Trước khi lọc: kiểm dự án / loại văn bản bằng một query; chỉ khi sai mới kiểm chi tiết để trả đúng mã `303` / `301` / `312`.
  Dự án `DONE` vẫn xem được.

## Danh sách

- `pageSize` phải từ `1` đến `200` (`SubmissionConst.MAX_PAGE_SIZE`), không nhận `-1` → `400`.
- `customData`: trường thuộc cấu hình dự án + loại văn bản và có `IsShowTableView = true`, sắp theo `OrderTableView`.
- Mỗi dòng (và chi tiết `GET {id}`, `next`, `previous`) có `statusCheck`, `statusQa`, `qaReason`.
- Mỗi dòng có `submissionDatas` theo thứ tự cột: `id`, `documentFieldId`, `data`, `indexRow`, `indexGoogleSheet` và `documentField`
  đầy đủ (khuôn `GetListDocumentFieldDto`, lấy **một lần** cho cả trang rồi gắn vào từng ô). Trường chưa có dữ liệu thì không có phần tử.

## Chi tiết và vị trí

- `fields`: **mọi** trường thuộc cấu hình dự án + loại văn bản của hồ sơ, sắp theo `OrderListView`, kể cả `IsShowListView = false`
  (FE tự ẩn) để gửi lại form không mất dữ liệu. Trường chưa có dữ liệu: `submissionDataId = null`, `data = ""`.
- Hồ sơ không tồn tại → `305`; hồ sơ thuộc dự án / loại văn bản khác bộ lọc → `319`.
- `includePosition = false` (mặc định): `position`, `totalItems` là `null`. `true`: tính bằng một câu SQL; `position` đếm từ `1`,
  hồ sơ nằm ngoài bộ lọc thì `position = null`. FE chỉ cần xin vị trí một lần khi mở màn hình hoặc đổi bộ lọc, sau đó tự cộng / trừ 1.

## Hồ sơ liền kề

- Theo keyset: `next` = hồ sơ có `Id` nhỏ nhất lớn hơn `{id}` trong bộ lọc, `previous` ngược lại; nhanh như nhau ở đầu / cuối, không nhảy cóc.
- Hồ sơ `{id}` **không cần** thoả bộ lọc, cũng không cần còn tồn tại: kiểm xong hồ sơ hiện tại (ra khỏi lọc "chưa kiểm tra") vẫn đi tiếp được.
- Hết hồ sơ → `317` (FE tắt nút). Hai route riêng thay cho một route có tham số `direction`: không có giá trị sai để bắt `400`.

## Kiểm tra hồ sơ

Body: `{ id, projectId, documentTypeId, datas: [{ documentFieldId, data }], note }` (trước 2026-10-21 là `PUT {id}/check`, id ở route).

- Hồ sơ luôn chuyển sang `CHECKED`, kể cả đã `CHECKED` (tái kiểm tra) hoặc đang `RETURNED`. Mỗi lần bấm sinh đúng **một** `SubmissionLog`
  + **một** `AuditLog`, kể cả không sửa ô nào.
- `Description` của log: `Người dùng: {UserName} đã kiểm tra hồ sơ.`, `... đã tái kiểm tra hồ sơ.` hoặc `... đã kiểm tra lại hồ sơ bị trả lại.`.
- Hồ sơ bị trả lại: `StatusQa` về `0`, xoá `QaReason` để QA chấm lại. Tái kiểm tra hồ sơ đã QA mà sửa ≥ 1 ô cũng về `0` (log "… sửa N
  trường sau QA — cần QA lại.", 18C R9); không sửa ô nào thì giữ `StatusQa`. Kết quả trả thêm `statusQa` ([13-ho-so-qa.md](13-ho-so-qa.md)).
- Dữ liệu:
  - trường đã có dữ liệu → so sau khi trim, khác thì ghi (cho phép xoá trắng); giữ nguyên `IndexGoogleSheet`, `IndexRow`;
  - trường chưa có dữ liệu → giá trị rỗng thì bỏ qua, khác rỗng thì thêm mới (`IndexRow` = `OrderFieldImport`, `IndexGoogleSheet` rỗng);
  - trường không gửi lên → giữ nguyên.
- Lỗi, không ghi gì: hồ sơ không tồn tại `305`; sai dự án / loại văn bản `319`; dự án không tồn tại `303`; dự án `DONE` `316`, hết hạn `323`
  (**trừ SuperAdmin**); trường gửi trùng `318`; trường không thuộc cấu hình dự án + loại văn bản `311`; ghi chú quá 1000 ký tự `400`.
- `changedDataCount` = số ô sửa + số ô thêm. Audit `Kiểm tra hồ sơ`, `Update`, lưu `StatusCheck` và giá trị cũ / mới từng trường.
- Một transaction (Task 25): lưu `Submission`, `SubmissionData`, `SubmissionLog`, `AuditLog` → `AssignCheckedToQaAsync` → lưu → (dự án không QA, Task 28: tự vào nghiệm thu khi văn bản cuối được kiểm, [22](22-nghiem-thu-du-an.md#tự-chuyển-trong-reviewqa--check)) → commit; lỗi ở đâu cũng không ghi gì.
- Sau khi kiểm, import overwrite với `overwriteChecked = false` bỏ qua hồ sơ này.

## Phạm vi và phân việc

Luật phân việc thuộc Operation: Thi công chỉ gọi `ISubmissionAssignmentService` (implement `SubmissionAssignmentService`, Task 25, thân
Task 15); implement được `SaveChanges`, không mở / commit transaction. Không gọi ở `summary`, `logs` dự án, thống kê QA, xuất Excel.
Lỗi: `820` không giữ văn bản / đã bị gỡ khỏi cặp, `826` người nhập kiểm lại văn bản đã QA đạt, `812` chờ khoá cặp quá 5 giây. Luật đủ ở
[23-chia-phieu.md](23-chia-phieu.md#nối-vào-luồng-kiểm-tra--qa).

| Method | Gọi ở |
|---|---|
| `ResolveRestrictedUserIdAsync` (khác `null` = chỉ văn bản có `SubmissionUser.UserId` này) | `paging`, `count`, `next` / `previous`, `tree`, `field-values`, `qa/paging`, vị trí ở `{id}` / `{id}/qa` |
| `EnsureCanViewAsync` (nạp hồ sơ trước) | `{id}`, `{id}/qa`, `{id}/logs`, `{id}/qa-logs`, `{id}/timeline` |
| `EnsureCanCheckAsync` · `AssignCheckedToQaAsync` | `check`: trước khi dựng kế hoạch · sau lần lưu đầu, trong transaction |
| `ApplyQaResultToAssignmentAsync` | `qa`: sau lưu log QA + trường sai, trước chốt phiếu FULL và tự nghiệm thu ([13](13-ho-so-qa.md)) |

## Lịch sử kiểm tra

Phân trang, sắp `CreatedDate` rồi `Id` giảm dần; mỗi dòng có `userName`, `fullName` (join bảng `Users` lúc đọc), `oldStatus`,
`newStatus`, `description`, `note`, `changedDataCount`. `totalItems` là tổng thật. Hồ sơ không tồn tại → `305`.

## Tra cứu và thống kê

Bốn API chỉ đọc, quyền `Function.Construction.Submission.View`, không audit; dự án / loại văn bản sai → `303` / `301` / `312`.

| Endpoint | Việc |
|---|---|
| `GET field-values?documentFieldId=` + bộ lọc chung | Giá trị khác nhau của một trường cho dropdown lọc: `[{ value, submissionCount }]`, sắp theo `value`, tối đa 200 (`FIELD_VALUE_MAX_ITEMS`), bỏ ô rỗng |
| `GET tree` + bộ lọc chung | Cây Hồ sơ → Hộp: `{ profileRanges: [{ profileRange, count, boxes: [{ box, count }] }], total }`, `count` = `total`, `totalNotChecked`, `totalChecked`, `totalReturned`, `totalQaPassed`, `totalQaFailed` |
| `GET logs` | Lịch sử kiểm tra cả dự án + loại văn bản: lọc `userId`, `oldStatus`, `fromDate`, `toDate`; mỗi dòng như `{id}/logs` + `fileName`, `profileRange`, `box` |
| `GET summary` | Tiến độ: số hồ sơ theo `StatusCheck` / `StatusQa`, `checkedPercent`, `totalFields`, `totalChargeableFields`, `totalFilledCells`, `totalChargeableFilledCellsOfChecked` |

- `field-values`: `keyword` tìm **trong giá trị** (không phải tên file); lọc liên hoàn — bỏ cặp lọc của chính trường đang lấy, vẫn qua
  **một** `ApplyFilter` (tham số `includeKeyword`, `excludedDocumentFieldId`). Trường không thuộc cấu hình → `311`.
- `tree`: một câu `GROUP BY (ProfileRange, Box)`, cây dựng trong bộ nhớ (Hộp cộng lên Hồ sơ, lên tổng) nên tổng mọi nút = `count.total`.
  Sắp **chuỗi** (`Hộp 10` trước `Hộp 2`); Hồ sơ / Hộp rỗng giữ `""`.
- `logs`: lọc `oldStatus` (`0` kiểm tra lần đầu, `1` tái kiểm tra, `2` kiểm tra lại hồ sơ bị trả lại) thay cho `newStatus` của spec — `NewStatus`
  luôn là `1`. `toDate` tính trọn ngày; `fromDate > toDate` → `400`. Tên file lấy `IgnoreQueryFilters` nên hồ sơ đã xoá vẫn thấy tên.
  Index `SubmissionLog (ProjectId, DocumentTypeId, CreatedDate)`, migration `Add_SubmissionLog_ProjectIndex`.
- `summary`: ô chỉ tính trường thuộc cấu hình, `Data` khác rỗng, hồ sơ chưa xoá; ô tính tiền = trường `IsChargeable` của hồ sơ `CHECKED`
  (hồ sơ trả lại không tính). Không tính tiền (cần đơn giá theo người — module phân công). `checkedPercent` làm tròn 2 số lẻ, chưa có hồ sơ → `0`.

## Đã kiểm thực tế

Ngày 2026-10-19, API thật bằng `admin` sau migration `Add_SubmissionLog` — 18/18 đạt: không token `401`; `paging` / `count` có / không lọc
(keyword, `statusCheck`, hai cặp trường) trả rỗng đúng, EF dịch được `EXISTS` và câu đếm gộp; `pageSize` `0` / `-1` / `500` → `400`; `303`,
`301`, `312`; chi tiết, lịch sử, kiểm tra với id không tồn tại → `305`; neighbor → `317`; `direction` sai → `400`.

Cùng ngày, chèn 6 hồ sơ + 3 trường test bằng SQL (gắn `ZZTEST`, kiểm xong đã xoá sạch), sau khi gộp import vào `SubmissionService` — 35/35 đạt:

- Đọc: phân trang qua trang 3; lọc `statusCheck`, một / hai cặp trường, keyword; cột theo `OrderTableView`, ô theo đúng thứ tự cột;
  `count` có / không lọc; chi tiết với `position` / `totalItems` đúng khi có lọc, `position = null` khi hồ sơ không khớp lọc; `319`;
  neighbor `next` / `previous` có lọc, đi hết 5 hồ sơ rồi `317`; tài khoản thiếu quyền `View` → `403`.
- Kiểm tra: sửa một ô (ô không đổi không tính), tái kiểm tra + xoá trắng ô, thêm ô mới, kiểm không sửa gì (`changedDataCount = 0`
  vẫn sinh log); `311`, `318`, `319`, `305`, ghi chú 1001 ký tự → `400`; thiếu quyền `Check` → `403`; dự án `DONE`: người thường
  → `316`, SuperAdmin sửa được.
- Rollback: gắn tạm `CHECK` constraint làm insert `SubmissionLog` lỗi → `500`; `SubmissionData`, `ModifiedBy` của hồ sơ giữ nguyên,
  không có log. Lịch sử đúng `userName`, `fullName`, mô tả, mới nhất trước; `AuditLog` đủ 5 dòng; Swagger đủ 13 route (lúc đó còn `neighbor`).

Sau khi tách `neighbor` thành `{id}/next`, `{id}/previous` — 20/20 đạt: đầu / cuối danh sách `317`; lọc trường, `statusCheck`, keyword; đi từ
hồ sơ ngoài bộ lọc hoặc id không tồn tại; đi hết cả hai chiều; `303`, `301`, `312`; thiếu `View` → `403`; route cũ `neighbor` → `404`.

Tra cứu và thống kê, 2026-10-22 trên `test-data-ho-so.sql` — 37/37 đạt: `field-values` đủ giá trị, số đếm, sắp `value`, keyword không phân biệt
hoa thường, lọc liên hoàn, bỏ cặp lọc trùng trường, `311` / `303` / `312`; `tree` đúng hình, tổng = `count.total`; `logs` mới nhất trước, lọc
`userId`, `oldStatus`, trọn ngày, `400`, trang 2, hồ sơ đã xoá mềm; `summary` khớp SQL tay; không token `401`, `viewer` → `403`.

Task 25, 2026-11-12 (dự án 25 hồ sơ × 4 trường + phiếu Data 20 văn bản gắn `ZZTEST25`, đã xoá) — đạt hết: 109 API đọc + xuất Excel giống hệt bản gốc
chạy song song; `assignmentTicketId` ra 20 / 25 ở mọi API; Check, QA Đạt / Không đạt lưu đúng; `337` cho người ngoài và Leader `IsActive = 0`; thân tạm: Resolve
trả D1 → chỉ văn bản D1 giữ, xem chi tiết / log bị chặn, lỗi phân việc rollback cả dữ liệu trường của Check và log QA; thân đã trả về rỗng.

Đo trên 20.000 hồ sơ × 30 trường (600.000 ô, 13.334 log, gắn `ZZPERF`, đo xong đã xoá), API chạy build Debug, SQL Server 2022 Docker, trung vị 7 lần
gọi cả HTTP. Một request nhẹ (`count` 16 hồ sơ) đã tốn ~136 ms (≈ 5 lượt đi – về: `[Permission]`, kiểm dự án, query) nên mục tiêu < 50 ms không đạt được ở môi trường này:

| API | Đo | Mục tiêu | API | Đo | Mục tiêu |
|---|---|---|---|---|---|
| `field-values` không lọc (135 giá trị) | **1.235 ms** | < 300 | `tree` không lọc / lọc | 227 / 205 ms | < 200 |
| `field-values` lọc `statusCheck` + keyword | 319 ms | < 300 | `logs` trang 50 / lọc 7 ngày | 242 / 181 ms | < 50 |
| `field-values` 18.000 giá trị, lấy 200 | 344 ms | < 300 | `summary` | 382 ms | < 500 |

`summary` lúc đầu 4.528 ms: join `SubmissionData` × `Submission` × `DocumentField` cho từng ô (3,4 triệu logical reads). Viết lại: lọc `Submission` theo dự án
trong join, `GROUP BY (DocumentFieldId, StatusCheck)` rồi cộng trường cấu hình / tính tiền trong bộ nhớ → ~13.000 reads.

## Giới hạn

⚠️ `field-values` không lọc chậm (~1 s ở DB): plan **sort theo `Data` (`nvarchar(max)`)** rồi seek `Submission` cho từng ô; nhiều ô cùng giá trị thì
không dừng sớm được. Đo tay: `GROUP BY CAST(Data AS nvarchar(4000))` còn 114 ms, nhưng EF cần thêm `DbFunction` trong `ShDbContext` và giá trị
dài hơn 4.000 ký tự bị cắt khi gộp — chưa làm, chờ chốt. Đừng thêm index / `INCLUDE` cột `Data` (xem dưới).

⚠️ **Chưa đo tốc độ** trên dự án 20.000 hồ sơ (execution plan, import append < 60 s sau khi thêm index). Import chưa gọi lại sau khi
gộp vào `SubmissionService` (cần Google Sheet thật); code import chỉ chuyển chỗ, không đổi logic.

⚠️ Hai người cùng kiểm một hồ sơ: người lưu sau ghi đè người lưu trước (last write wins). Muốn chặn thì gửi kèm `ModifiedDate`
để so (optimistic concurrency) — chưa làm.

⚠️ Lọc theo giá trị trường phải đọc cột `SubmissionData.Data` (`nvarchar(max)`, không index được) cho từng hồ sơ trong phạm vi lọc.
Đừng thêm index / `INCLUDE` cột này: nhân đôi dung lượng và làm chậm bulk import. Nếu đo thấy chậm thì tính cột băm có index.

⚠️ Kiểm tra tốn 8 lượt đi – về DB (6 luồng chính + vai trò + `UserName`, token không có tên — đã chốt giữ), Task 25 thêm mở / commit transaction.
Nạp hồ sơ (`305`, `319`) chạy **trước** nên lỗi sớm không tốn thêm; cộng chi phí `[Permission]` ([02-phan-quyen.md](02-phan-quyen.md)).

⚠️ `Description` lưu `UserName` **tại thời điểm kiểm tra**; đổi tên đăng nhập sau đó thì mô tả cũ không đổi, còn cột `userName` /
`fullName` khi đọc lịch sử luôn là tên hiện tại.

> **Tiếp:** [13-ho-so-qa.md](13-ho-so-qa.md) — QA hồ sơ đã kiểm tra, trả lại hồ sơ không đạt, lịch sử QA.
