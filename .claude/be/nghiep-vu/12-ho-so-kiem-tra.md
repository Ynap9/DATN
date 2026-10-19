# Hồ sơ — xem, lọc, kiểm tra

> **Phần 12/12** · trước: [11-ho-so-import.md](11-ho-so-import.md) · mục lục: [README.md](README.md)

Sau khi import ([11-ho-so-import.md](11-ho-so-import.md)), người kiểm tra mở màn hình hồ sơ của **một dự án + một loại văn bản**:
xem bảng, lọc, mở từng hồ sơ (PDF bên trái qua `proxy-file`, form trường bên phải), sửa dữ liệu rồi bấm **Kiểm tra**.
Không có phân việc: mọi user có quyền đều thấy toàn bộ hồ sơ của dự án.

## Các chức năng

| Endpoint (`api/construction/submission/...`) | Quyền | Việc |
|---|---|---|
| `GET paging` | `Function.Construction.Submission.View` | Danh sách phân trang + lọc; `customData` = các cột của bảng |
| `GET {id}` | `...Submission.View` | Chi tiết hồ sơ + form trường; `includePosition = true` thì có `position` / `totalItems` |
| `GET {id}/next` | `...Submission.View` | Hồ sơ kế tiếp trong bộ lọc (nút "Sau"), trả chi tiết như `GET {id}` |
| `GET {id}/previous` | `...Submission.View` | Hồ sơ liền trước trong bộ lọc (nút "Trước"), trả chi tiết như `GET {id}` |
| `GET count` | `...Submission.View` | `total`, `totalChecked`, `totalNotChecked` theo bộ lọc |
| `PUT {id}/check` | `Function.Construction.Submission.Check` | Sửa dữ liệu + chuyển `CHECKED` + ghi lịch sử |
| `GET {id}/logs` | `...Submission.View` | Lịch sử kiểm tra, mới nhất trước |

Route có ràng buộc `{id:int}` để `paging`, `count`, `proxy-file` không khớp nhầm route `{id}`.

## Bộ lọc chung

Năm API đọc nhận cùng bộ lọc query: `projectId`, `documentTypeId` (bắt buộc), `statusCheck` (bỏ trống = tất cả), `keyword`,
`documentFieldId1` + `fieldValue1`, `documentFieldId2` + `fieldValue2`. Mọi API lọc qua **một** hàm `SubmissionQuery.ApplyFilter`,
nên `count.total` luôn bằng `paging.totalItems`, và vị trí, hồ sơ liền kề cũng khớp với danh sách.

- Thứ tự: `Submission.Id` tăng dần (= thứ tự import).
- `keyword`: chứa trong tên file, Hồ sơ hoặc Hộp; không phân biệt hoa thường (theo collation DB).
- Cặp trường / giá trị: hồ sơ phải có `SubmissionData` của trường đó với `Data` **bằng đúng** giá trị đã trim. Thiếu một vế thì bỏ
  qua cặp đó. Có hai cặp thì phải thoả cả hai.
- Trước khi lọc: kiểm dự án / loại văn bản bằng một query; chỉ khi sai mới kiểm chi tiết để trả đúng mã `303` / `301` / `312`.
  Dự án `DONE` vẫn xem được.

## Danh sách

- `pageSize` phải từ `1` đến `200` (`SubmissionConst.MAX_PAGE_SIZE`), không nhận `-1` → `400`.
- `customData`: trường thuộc cấu hình dự án + loại văn bản và có `IsShowTableView = true`, sắp theo `OrderTableView`.
- Mỗi dòng có `submissionDatas` theo thứ tự cột: `id`, `documentFieldId`, `data`, `indexRow`, `indexGoogleSheet` và `documentField`
  đầy đủ (cùng khuôn `GetListDocumentFieldDto` của API trường văn bản). Trường chưa có dữ liệu thì không có phần tử.
  Thông tin trường lấy **một lần** cho cả trang rồi gắn vào từng ô, không join lặp lại trong SQL.

## Chi tiết và vị trí

- `fields`: **mọi** trường thuộc cấu hình dự án + loại văn bản của hồ sơ, sắp theo `OrderListView`, kể cả `IsShowListView = false`
  (FE tự ẩn) để gửi lại form không mất dữ liệu. Trường chưa có dữ liệu: `submissionDataId = null`, `data = ""`.
- Hồ sơ không tồn tại → `305`; hồ sơ thuộc dự án / loại văn bản khác bộ lọc → `319`.
- `includePosition = false` (mặc định): `position`, `totalItems` là `null`. `true`: tính bằng một câu SQL; `position` đếm từ `1`,
  hồ sơ nằm ngoài bộ lọc thì `position = null`. FE chỉ cần xin vị trí một lần khi mở màn hình hoặc đổi bộ lọc, sau đó tự cộng / trừ 1.

## Hồ sơ liền kề

- Theo keyset: `next` = hồ sơ có `Id` nhỏ nhất lớn hơn `{id}` trong bộ lọc, `previous` ngược lại. Thời gian như nhau ở đầu hay cuối
  danh sách, và không nhảy cóc khi có người import thêm.
- Hồ sơ `{id}` **không cần** thoả bộ lọc, cũng không cần còn tồn tại: lọc "chưa kiểm tra", kiểm xong hồ sơ hiện tại (nó ra khỏi bộ lọc)
  vẫn đi tiếp được sang hồ sơ chưa kiểm tra kế tiếp.
- Hết hồ sơ → `317` (FE tắt nút). Hai route riêng thay cho một route có tham số `direction`: không có giá trị sai để bắt `400`.

## Kiểm tra hồ sơ

Body: `{ projectId, documentTypeId, datas: [{ documentFieldId, data }], note }`.

- Hồ sơ luôn chuyển sang `CHECKED`, kể cả đã `CHECKED` (tái kiểm tra). Mỗi lần bấm sinh đúng **một** `SubmissionLog` + **một**
  `AuditLog`, kể cả không sửa ô nào.
- `Description` của log: `Người dùng: {UserName} đã kiểm tra hồ sơ.` hoặc `... đã tái kiểm tra hồ sơ.`.
- Dữ liệu:
  - trường đã có dữ liệu → so sau khi trim, khác thì ghi (cho phép xoá trắng); giữ nguyên `IndexGoogleSheet`, `IndexRow`;
  - trường chưa có dữ liệu → giá trị rỗng thì bỏ qua, khác rỗng thì thêm mới (`IndexRow` = `OrderFieldImport`, `IndexGoogleSheet` rỗng);
  - trường không gửi lên → giữ nguyên.
- Lỗi, không ghi gì: hồ sơ không tồn tại `305`; sai dự án / loại văn bản `319`; dự án không tồn tại `303`; dự án `DONE` `316`
  (**trừ SuperAdmin**); trường gửi trùng `318`; trường không thuộc cấu hình dự án + loại văn bản `311`; ghi chú quá 1000 ký tự `400`.
- `changedDataCount` = số ô sửa + số ô thêm. Audit `Kiểm tra hồ sơ`, `Update`, lưu `StatusCheck` và giá trị cũ / mới từng trường.
- Ghi bằng **một** `SaveChanges` (EF tự bọc transaction): `Submission`, `SubmissionData`, `SubmissionLog`, `AuditLog` cùng thành công
  hoặc cùng không. Lý do bỏ `BeginTransactionAsync` ở [../architecture/02-applications.md](../architecture/02-applications.md).
- Sau khi kiểm, import overwrite với `overwriteChecked = false` bỏ qua hồ sơ này.

## Lịch sử kiểm tra

Phân trang, sắp `CreatedDate` rồi `Id` giảm dần; mỗi dòng có `userName`, `fullName` (join bảng `Users` lúc đọc), `oldStatus`,
`newStatus`, `description`, `note`, `changedDataCount`. `totalItems` là tổng thật. Hồ sơ không tồn tại → `305`.

## Đã kiểm thực tế

Ngày 2026-10-19, gọi API thật bằng `admin` sau khi chạy migration `Add_SubmissionLog` (seeder đã thêm quyền `Check`) — 18/18 đạt:
không token → `401`; `paging` và `count` có / không có bộ lọc (keyword, `statusCheck`, hai cặp trường, giá trị có khoảng trắng) trả
rỗng đúng khi chưa có hồ sơ, EF dịch được `EXISTS` và câu đếm gộp; `pageSize` `0` / `-1` / `500` → `400`; `303`, `301`, `312`;
chi tiết (có / không `includePosition`), lịch sử, kiểm tra với id không tồn tại → `305`; neighbor `next` / `previous` → `317`;
`direction` sai → `400`.

Cùng ngày, chèn 6 hồ sơ + 3 trường test bằng SQL (gắn `ZZTEST`, kiểm xong đã xoá sạch), sau khi gộp import vào `SubmissionService` — 35/35 đạt:

- Đọc: phân trang qua trang 3; lọc `statusCheck`, một / hai cặp trường, keyword; cột theo `OrderTableView`, ô theo đúng thứ tự cột;
  `count` có / không lọc; chi tiết với `position` / `totalItems` đúng khi có lọc, `position = null` khi hồ sơ không khớp lọc; `319`;
  neighbor `next` / `previous` có lọc, đi hết 5 hồ sơ rồi `317`; tài khoản thiếu quyền `View` → `403`.
- Kiểm tra: sửa một ô (ô không đổi không tính), tái kiểm tra + xoá trắng ô, thêm ô mới, kiểm không sửa gì (`changedDataCount = 0`
  vẫn sinh log); `311`, `318`, `319`, `305`, ghi chú 1001 ký tự → `400`; thiếu quyền `Check` → `403`; dự án `DONE`: người thường
  → `316`, SuperAdmin sửa được.
- Rollback: gắn tạm `CHECK` constraint làm insert `SubmissionLog` lỗi → `500`; `SubmissionData`, `ModifiedBy` của hồ sơ giữ nguyên,
  không có log. Lịch sử trả đúng `userName`, `fullName`, mô tả "kiểm tra" / "tái kiểm tra", mới nhất trước; `AuditLog` đủ 5 dòng.
- Swagger đủ 13 route của `SubmissionController` (lúc đó còn route `neighbor`).

Sau khi tách `neighbor` thành `{id}/next` và `{id}/previous`, gọi lại trên dữ liệu `ZZTEST` — 20/20 đạt: đầu / cuối danh sách →
`317`; lọc trường, `statusCheck`, keyword; đi từ hồ sơ ngoài bộ lọc hoặc id không tồn tại; đi hết danh sách cả hai chiều; `303`,
`301`, `312`; thiếu quyền `View` → `403`; route cũ `neighbor` → `404`.

## Giới hạn

⚠️ **Chưa đo tốc độ** trên dự án 20.000 hồ sơ (execution plan, import append < 60 s sau khi thêm index). Import chưa gọi lại sau khi
gộp vào `SubmissionService` (cần Google Sheet thật); code import chỉ chuyển chỗ, không đổi logic.

⚠️ Hai người cùng kiểm một hồ sơ: người lưu sau ghi đè người lưu trước (last write wins). Muốn chặn thì gửi kèm `ModifiedDate`
để so (optimistic concurrency) — chưa làm.

⚠️ Lọc theo giá trị trường phải đọc cột `SubmissionData.Data` (`nvarchar(max)`, không index được) cho từng hồ sơ trong phạm vi lọc.
Đừng thêm index / `INCLUDE` cột này: nhân đôi dung lượng và làm chậm bulk import. Nếu đo thấy chậm thì tính cột băm có index.

⚠️ Kiểm tra tốn 8 lượt đi – về DB: ngoài 6 lượt của luồng chính còn 1 lượt lấy vai trò (xác định SuperAdmin) và 1 lượt lấy
`UserName` qua `IUserRepository` (token không có tên) — đã chốt giữ. Nạp hồ sơ (`305`, `319`) chạy **trước** hai lượt này nên lỗi
sớm không tốn thêm. Cộng thêm chi phí `[Permission]` mỗi request ([02-phan-quyen.md](02-phan-quyen.md)).

⚠️ `Description` lưu `UserName` **tại thời điểm kiểm tra**; đổi tên đăng nhập sau đó thì mô tả cũ không đổi, còn cột `userName` /
`fullName` khi đọc lịch sử luôn là tên hiện tại.
