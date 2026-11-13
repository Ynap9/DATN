# Xoá dự án kèm dữ liệu và danh sách cặp dự án – loại văn bản

> **Phần 25/26** · trước: [24-don-gia-tien-cong.md](24-don-gia-tien-cong.md) · mục lục: [README.md](README.md)

Task 29 (2026-11-13). Phần CRUD dự án gốc ở [07-du-an.md](07-du-an.md).

## Xoá dự án kèm dữ liệu — `DELETE api/construction/project/admin/delete/{id:int}`

Quyền `Function.Construction.Project.Delete` **và** phải là Admin / SuperAdmin (`ProjectDomainService.EnsureCanDeleteWithDependents`)
→ người khác `400` "Chỉ Admin được xoá dự án kèm dữ liệu" (kiểm trước khi tìm dự án). Không tồn tại / đã xoá → `303`. Xoá được ở **mọi** trạng thái
(kể cả `ACCEPTING`, `DONE`, `EXPIRED`). API cũ `DELETE {id}` giữ nguyên (chỉ xoá dự án + hai bảng loại văn bản).

`ProjectService.DeleteCascade`, một transaction: khoá dòng dự án (`LockAsync`) **rồi mới** đọc dự án — hai lần xoá cùng lúc thì lần sau
nhận `303`, không ghi đè mốc xoá, không sinh audit thứ hai.

1. `ProjectRepository.SoftDeleteDependentsAsync` xoá mềm 16 bảng phụ thuộc bằng `ExecuteUpdateAsync`, theo thứ tự con trước cha:
   `SubmissionQaLogField`, `SubmissionQaLog`, `SubmissionLog`, `SubmissionData`, `SubmissionUser`, `Submission`, `QaTicket`,
   `QaReviewerMode`, `QaSamplingConfig`, `AssignmentTicketItem`, `AssignmentTicket`, `ProjectMember`, `ProjectAiConfig`,
   `ProjectDeadlineNotification`, `ProjectDocumentTypeDocumentField`, `ProjectDocumentType`.
2. `project.Delete` rồi audit `Xóa dự án {Name} kèm dữ liệu phụ thuộc`: giá trị cũ như xoá thường, giá trị mới
   `{ DeletedDate, DeletedCounts }` — số dòng đã đánh dấu theo từng bảng.
3. Lỗi bất kỳ bước nào → rollback toàn bộ, không bảng nào bị đánh dấu dở.

Luật:

- Mọi dòng cùng một `DeletedBy` / `DeletedDate` (giờ Việt Nam lấy một lần) để truy vết một lần xoá.
- Dòng **đã xoá từ trước** (thành viên đã gỡ, phiếu đã huỷ…) giữ mốc xoá cũ: query filter `!Deleted` tự loại chúng khỏi `ExecuteUpdateAsync`.
- Bốn bảng `SubmissionUser`, `ProjectDeadlineNotification`, `AssignmentTicket`, `AssignmentTicketItem` có cờ xoá mềm từ migration
  `Add_SoftDelete_ProjectDependents` (index unique của `SubmissionUser` và index mail đã gửi lọc thêm `[Deleted] = 0`).
  Các luồng khác của bốn bảng (về pool, gỡ thành viên…) vẫn xoá cứng như cũ — cờ chỉ dùng cho xoá dự án.
- **Không** đụng `SigningBatch` (lô ký không gắn dự án) và file trên MinIO.
- Sau commit mới đổi tên file Google Sheet thành `"{Name} (đã xoá)"` (`TryRenameSpreadsheetAsync`); thiếu cấu hình Google hoặc
  lỗi Google chỉ ghi log `Warning`, API vẫn thành công. Không xoá file.

Sau khi xoá: `GET {id}`, mở hồ sơ, `my-assignment`… của dự án đều `303` hoặc không còn trong danh sách; job hạn dự án bỏ qua.

## Chặn xoá loại văn bản đang dùng — `346`

`DELETE api/construction/document-type/{id}` → `346` "Loại văn bản đang được dự án sử dụng, không thể xoá" khi còn
`ProjectDocumentType` của dự án **chưa xoá** (`DocumentTypeDomainService.EnsureNotUsedByProjectAsync`). Xoá dự án trước thì xoá được.

## Danh sách cặp — `GET api/construction/project/paging`

Mỗi dòng là **một cặp (dự án, loại văn bản)** (`ProjectDocumentType`), không còn một dòng một dự án. Quyền vẫn
`Function.Construction.Project.View` — Leader / Data / QA phải được gán tay ([03](03-vai-tro-mac-dinh.md)).

Lọc: `pageNumber`, `pageSize`, `keyword` (tên dự án), `status`, `documentTypeId` (mới). Phạm vi: Admin / SuperAdmin thấy mọi cặp;
người khác chỉ thấy cặp mình có dòng `ProjectMember` chưa gỡ (kể cả đang ngưng hoạt động). Sắp dự án mới tạo trước, trong dự án
theo tên loại văn bản, loại đã xoá mềm xếp cuối nhưng **vẫn hiện** với tên cũ.

Item `GetListProjectPairDto`:

| Trường | Ý nghĩa |
|---|---|
| `projectId`, `projectName`, `status`, `startedAt`, `endedAt`, `isQaEnabled` | Thông tin dự án |
| `urlGoogleSheetSync` | Chỉ Admin / Leader; Data / QA nhận `null` |
| `documentTypeId`, `documentTypeName` | Loại văn bản của cặp |
| `viewerRole` | `Admin` hoặc vai trò thành viên của người gọi trong cặp (`Leader` / `Data` / `QA`) |
| `memberCount` | Mọi `ProjectMember` chưa gỡ của cặp, kể cả Leader và người ngưng hoạt động |
| `totalBox`, `totalProfileRange` | Số Hộp / số (Hộp, Hồ sơ) khác nhau của cặp |
| `totalSubmission`, `checkedCount`, `progressPercent` | Admin / Leader: cả cặp; Data: văn bản mình giữ / đã kiểm; QA: văn bản mình phụ trách / đã chấm (`StatusQa` khác chưa chấm). `%` làm tròn 2 số |
| `qaPassedCount` | Chỉ Admin / Leader và dự án có QA; còn lại `null` |
| `filledFieldCount` | Chỉ Data: số ô có dữ liệu (trim khác rỗng) của **trường tính tiền** (`IsChargeable`, trường chưa xoá) trong văn bản mình đã kiểm; còn lại `null` |

`ProjectQuery.FindPaging`: 1 câu đếm + 1 câu lấy trang, rồi 5 câu gom theo các cặp trong trang (tên loại văn bản, số thành viên,
số liệu cả cặp, số "của tôi", số ô đã nhập). Hai câu "của tôi" nằm trong `CountMyHeldSubmissionsAsync`, dùng chung với `myProgresses` ở
chi tiết dự án (Task 30). Không cache. Phạm vi "được phân" ở đây cũng áp cho danh sách, dropdown, chi tiết, nghiệm thu ([07](07-du-an.md#phạm-vi-xem--task-30)).

`filledFieldCount` từ Task 30 (case 30.11, như 5sdb, cùng luật tiền công [24](24-don-gia-tien-cong.md)) đọc cờ `IsChargeable` **hiện tại**:
đổi cờ một trường thì số đổi theo cho mọi văn bản đã kiểm.

⚠️ Bẫy: `IgnoreQueryFilters()` gỡ filter của **cả câu truy vấn**, không riêng bảng join. Muốn lấy tên loại văn bản đã xoá mềm mà vẫn
giữ filter dự án / thành viên thì tách câu riêng (như `documentTypeNames`), đừng gắn `IgnoreQueryFilters()` vào câu chính.

## Đã kiểm thực tế

Ngày 2026-11-13, DB dev, dữ liệu tạm `ZZTEST29` đã dọn, audit test đã xoá:

- Xoá kèm dữ liệu: Data / Leader có quyền `Delete` → `400`; không quyền `403`; không token `401`; Admin xoá → 17 bảng `Deleted = 1`
  cùng mốc, thành viên gỡ từ trước giữ mốc cũ, audit có `DeletedCounts`; gọi lại / id không có → `303`; dự án `ACCEPTING` / `DONE` /
  `EXPIRED` xoá được; trigger lỗi tạm trên `ProjectAiConfig` → `500`, rollback sạch. URL sheet giả → log `Warning`; sheet thật đổi tên đúng.
  Hai request xoá đồng thời → một `200`, một `303`, đúng 1 audit, mọi bảng cùng mốc.
- `346`: loại văn bản còn dự án dùng → chặn; xoá hết dự án → xoá được.
- Paging: Admin đủ cặp, lọc + trang 2 đúng; Leader + Data cùng dự án ra đúng vai trò từng dòng; Data 20/5 = 25%, ô đã nhập 20;
  QA 3/5 = 60%; Data ngưng hoạt động vẫn thấy cặp; `memberCount = 4`; dự án không QA `qaPassedCount = null`; người ngoài dự án rỗng;
  loại văn bản đã xoá hiện cuối. Số liệu khớp đếm SQL tay.
- Thời gian (DB dev qua tunnel, mỗi câu SQL ~110–150 ms, 62 hồ sơ): Admin ~1,3 s, Leader ~1,6–1,9 s (9 câu SQL: 7 paging + 2 kiểm quyền).
  Đa phần là độ trễ mạng; chưa đo trên dữ liệu lớn.
- Task 30 (`ZZTEST30` đã dọn): JSON paging trước / sau khi tách hàm đếm khớp 5/5 người gọi (trừ `filledFieldCount` của Data);
  `filledFieldCount` 16 → 12 (chỉ trường tính tiền), tắt thêm 1 cờ → 8, ô rỗng / toàn khoảng trắng và trường đã xoá không tính, khớp SQL tay.

## Giới hạn

⚠️ Không có API khôi phục dự án đã xoá; khôi phục tay phải lọc đúng `DeletedDate` của lần xoá để không bật lại dòng đã xoá từ trước.

⚠️ Đổi tên Google Sheet là bước ngoài transaction: lỗi thì tên file không đổi, chỉ có log.

> **Tiếp:** [26-thu-hoi-chuyen-phieu.md](26-thu-hoi-chuyen-phieu.md) — thu hồi / chuyển phiếu nhập liệu và phụ trách QA, danh sách phiếu.
