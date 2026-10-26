# Hồ sơ — QA, trả lại, lịch sử QA

> **Phần 13/19** · trước: [12-ho-so-kiem-tra.md](12-ho-so-kiem-tra.md) · mục lục: [README.md](README.md)

Sau khi người nhập liệu kiểm tra hồ sơ ([12-ho-so-kiem-tra.md](12-ho-so-kiem-tra.md)), người QA mở hồ sơ, đối chiếu PDF với dữ liệu
rồi chấm **Đạt** hoặc **Không đạt (kèm lý do)**. Không đạt thì hồ sơ bị **trả lại** cho người nhập liệu sửa. Bản gốc `nhaplieu.be`
không có QA — nghiệp vụ mới hoàn toàn. Không có phân công: ai có quyền đều QA được mọi hồ sơ, kể cả hồ sơ chính mình kiểm.

## Hai trạng thái độc lập

| Cột của `Submission` | `0` | `1` | `2` |
|---|---|---|---|
| `StatusCheck` | Chưa kiểm tra (`NOT_CHECKED`) | Đã kiểm tra (`CHECKED`) | Trả lại (`RETURNED`) |
| `StatusQa` | Chưa QA (`QA_NOT_REVIEWED`) | Đạt (`QA_PASSED`) | Không đạt (`QA_FAILED`) |

Cột QA khác: `QaReason` (lý do Không đạt đang hiệu lực), `QaBy` / `QaDate` (người, lúc QA gần nhất). Migration `Add_SubmissionQa`
thêm cột `int NOT NULL` default `0` nên hồ sơ có từ trước là "Chưa QA".

## Chuyển trạng thái (`StatusCheck` / `StatusQa`)

| Thao tác | Trước | Sau | Ghi chú |
|---|---|---|---|
| Import thêm mới | – | 0 / 0 | |
| Kiểm tra | 0 / 0 | 1 / 0 | |
| Tái kiểm tra không đổi ô nào | 1 / x | 1 / x | |
| Tái kiểm tra có sửa ô, đã QA | 1 / 1 | 1 / 0 | Cần QA lại (18C R9): xoá `QaReason`, giữ `QaBy` / `QaDate` |
| QA Đạt | 1 / 0, 1 | 1 / 1 | Xoá `QaReason`; lý do gửi kèm bị bỏ qua |
| QA Không đạt | 1 / 0, 1 | 2 / 2 | Lưu `QaReason` đã trim |
| Kiểm tra lại hồ sơ bị trả lại | 2 / 2 | 1 / 0 | Xoá `QaReason` (còn trong lịch sử QA), giữ `QaBy` / `QaDate` |
| QA hồ sơ chưa kiểm tra / đang trả lại | 0 / x, 2 / x | lỗi `320` | |

Luật nằm ở `Submission.ReviewQa` và `Submission.Check`. Hồ sơ `1 / 2` không thể có vì Không đạt luôn kèm trả lại.
Xoá hồ sơ không phải chuyển trạng thái: chỉ được khi `StatusCheck` là `0` hoặc `2`, trừ SuperAdmin ([14-ho-so-xoa-xuat.md](14-ho-so-xoa-xuat.md)).
Xuất Excel không đổi trạng thái nào.
Import overwrite với `overwriteChecked = false` chỉ bảo vệ hồ sơ `CHECKED`: hồ sơ đang trả lại **bị** ghi đè (đang chờ sửa).
Import blank / overwrite không đổi `StatusQa`.

## Các chức năng

Thống kê QA theo trường, theo người nhập và dòng thời gian một văn bản: [17-thong-ke-qa.md](17-thong-ke-qa.md).

| Endpoint (`api/v1/construction/submission/...`) | Quyền | Việc |
|---|---|---|
| `GET qa/paging` | `Function.Construction.Submission.View` | Danh sách cho QA + `customData` `{ totalNotReviewed, totalPassed, totalFailed }` |
| `GET {id:int}/qa` | `...Submission.View` | Chi tiết cho QA: thông tin file, `fields`, QA, người kiểm gần nhất, vị trí |
| `PUT qa` | `Function.Construction.Submission.Qa` | Chấm QA: `{ id, projectId, documentTypeId, result, reason, wrongDocumentFieldIds }`. Chỉ QA của phiếu chứa văn bản, hoặc Admin / SuperAdmin (`334` / `332`, [18C](18-qa-cham-mau.md#18c--phiếu-qa)) |
| `GET {id:int}/qa-logs` | `...Submission.View` | Lịch sử QA, mới nhất trước |

Đi Sau / Trước giữa các hồ sơ chờ QA: dùng `{id}/next`, `{id}/previous` với `statusCheck=1&statusQa=0`; hai API này trả chi tiết thường
(có `statusQa`, `qaReason`), cần người kiểm / ghi chú kiểm thì gọi thêm `{id}/qa`.

## Danh sách và chi tiết

- Bộ lọc chung ([12-ho-so-kiem-tra.md](12-ho-so-kiem-tra.md#bộ-lọc-chung)) có thêm `statusQa` (bỏ trống = tất cả); vẫn **một** hàm
  `SubmissionQuery.ApplyFilter`, nên `qa/paging.totalItems` bằng `count.total` cùng bộ lọc. FE màn QA mặc định `statusCheck=1&statusQa=0`.
- `pageSize` từ `1` đến `200` (`400`); dự án / loại văn bản sai `303` / `301` / `312`.
- `customData` đếm theo bộ lọc **bỏ** `statusQa` (để 3 tab luôn có số), trong một câu SQL.
- Mỗi dòng: `qaBy`, `qaUserName`, `qaDate`, `checkedBy`, `checkedUserName`, `checkedDate` (từ `SubmissionLog` mới nhất). Không trả dữ liệu trường.
- `{id}/qa` thêm `checkNote` (ghi chú lần kiểm gần nhất); `fields`, `position` dựng cùng hàm với `GET {id}`. Lỗi `305`, `319`.
- `GET {id}`, `{id}/qa`, `next`, `previous` có `qaWrongDocumentFieldIds`: trường sai của lần trả lại gần nhất khi `StatusQa = 2`, còn lại `[]`
  — người nhập liệu biết phải sửa ô nào. Kiểm tra lại xong (`StatusQa` về `0`) thì rỗng, danh sách cũ vẫn xem được ở `qa-logs`.

## Chấm QA

- `result` chỉ nhận `1` / `2` → khác thì `400`. Hồ sơ không `CHECKED` → `320`.
- `id` hồ sơ nằm trong body (không ở route), giống `PUT check`.
- Không đạt bắt buộc **cả hai**: lý do chung (rỗng / toàn khoảng trắng → `321`, tối đa 1000 ký tự → `400`) và `wrongDocumentFieldIds` có
  ít nhất một trường (rỗng → `324`); trường gửi trùng → `318`; trường không thuộc cấu hình dự án + loại văn bản → `311`.
- Đạt: `reason` và `wrongDocumentFieldIds` gửi kèm bị bỏ qua, không kiểm, không lưu.
- Được chấm lại hồ sơ đã chấm (Đạt → Không đạt…). QA **không** sửa dữ liệu trường.
- Lỗi khác: hồ sơ không có `305`; sai dự án / loại văn bản `319`; dự án không có `303`; dự án `DONE` `316`, hết hạn `323` (**trừ SuperAdmin**).
- Mỗi lần chấm: **một** `SubmissionQaLog` (trạng thái QA và kiểm tra cũ / mới, `Reason`, mô tả `Người dùng: {UserName} đánh giá
  hồ sơ ĐẠT.` / `... KHÔNG ĐẠT và trả lại.`), các dòng `SubmissionQaLogField (SubmissionQaLogId, DocumentFieldId)` khi Không đạt
  (migration `Add_SubmissionQaLogField`), **một** `AuditLog` `QA hồ sơ`, `Update` (có `WrongDocumentFieldIds`).
- Hai lần `SaveChanges` trong **một** transaction: lưu log lấy `Id` rồi mới dựng trường sai (`SubmissionQaLog.CreateWrongFields`),
  vì entity không có navigation; lỗi ở bất kỳ bước nào → rollback cả hồ sơ, log, trường sai, audit.
- Trả `{ submissionId, statusCheck, statusQa, qaReason, wrongDocumentFieldIds, qaLogId }`.

Lịch sử QA: phân trang, `CreatedDate` rồi `Id` giảm dần, có `userName`, `fullName` (join `Users` lúc đọc), `wrongDocumentFieldIds` của
từng lần (lấy một lần cho cả trang); hồ sơ không có → `305`.

## Đã kiểm thực tế

Ngày 2026-10-21, gọi API thật trên dữ liệu `.claude/be/db/test-data-ho-so.sql` (dựng lại sạch trước và sau khi test) — 42/42 đạt:

- Không token `401`; `qa/paging` chờ QA đúng 4 hồ sơ `CHECKED`, `customData` đúng trước và sau khi chấm; `pageSize = 0` → `400`.
- `320` (chưa kiểm tra, đang trả lại); `result` 0 / 3 → `400`; thiếu lý do / toàn khoảng trắng → `321`; lý do 1001 ký tự → `400`; các lỗi không ghi gì.
- Đạt → 1/1, `QaReason` null, 1 QaLog + 1 AuditLog; Không đạt → 2/2, lý do đã trim; Đạt → Không đạt được, log `OldStatusQa = 1`.
- `check` hồ sơ trả lại → 1/0, mô tả "kiểm tra lại hồ sơ bị trả lại", `QaBy` giữ; tái kiểm tra hồ sơ đã Đạt → `StatusQa` vẫn 1 (luật cũ;
  từ 18C chặng 2 chỉ giữ khi không đổi ô nào).
- `319`, `305`; lọc `statusQa` 0 / 1 / 2 khớp `count.total`; `count` có `totalReturned`; `next` / `previous` với `statusQa=0`, hết → `317`.
- `{id}/qa` có người kiểm, người QA, 4 trường, `position` / `totalItems`; `qa-logs` mới nhất trước, `totalItems` thật.
- Thiếu quyền `Qa` → `403` khi `PUT`, quyền `View` vẫn đọc được; dự án `DONE`: người thường `316`, SuperAdmin chấm được.
- Swagger nhóm `construction` đủ 18 route của `SubmissionController` (14 cũ gồm `proxy-file` + 4 mới).

Cùng ngày, sau khi đưa `id` vào body (`PUT qa`, `PUT check`) và thêm trường sai — 51/51 đạt: thêm `324` (không chọn trường), `318`
(chọn trùng), `311` (trường `Ghi chú` ngoài cấu hình); Đạt kèm trường sai không lỗi, không lưu dòng nào; Không đạt lưu đúng 2 dòng
`SubmissionQaLogField` và trả `wrongDocumentFieldIds`; `GET {id}` hồ sơ trả lại có `qaWrongDocumentFieldIds`, kiểm tra lại xong thì rỗng;
`qa-logs` có trường sai từng lần; route cũ `PUT {id}/qa`, `PUT {id}/check` → `405`; thiếu quyền `Qa` (user `viewer`) → `403`.

## Giới hạn

⚠️ **Chưa đo tốc độ** trên 20.000 hồ sơ (mục tiêu `qa/paging` < 250 ms, `{id}/qa` < 80 ms, `PUT` < 100 ms, `qa-logs` < 50 ms).
`qa/paging` chạy subquery `SubmissionLog` mới nhất (index `(SubmissionId, CreatedDate)`) và `Users` cho từng dòng của **một trang**.

⚠️ Hai người QA cùng một hồ sơ: người lưu sau ghi đè (last write wins), giống `check`.

⚠️ Tái kiểm tra có sửa ô vẫn giữ kết quả Đạt; import blank / overwrite cũng không reset QA. Muốn bắt QA chấm lại khi dữ liệu đổi
thì sửa `Submission.Check` / policy import — đổi nghiệp vụ, hỏi trước.

⚠️ User bị xoá là xoá **cứng** (`UserManager.DeleteAsync`), nên `qaUserName` / `checkedUserName` / `userName` của người đã xoá là `null`.

> **Tiếp:** [14-ho-so-xoa-xuat.md](14-ho-so-xoa-xuat.md) — xoá hồ sơ (mở khoá theo trạng thái kiểm tra) và xuất Excel theo bộ lọc.
