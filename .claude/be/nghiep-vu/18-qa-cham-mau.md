# QA — chấm mẫu: chế độ chấm, cấu hình mẫu

> **Phần 18/19** · trước: [17-thong-ke-qa.md](17-thong-ke-qa.md) · mục lục: [README.md](README.md)

QA chấm 100% văn bản thì tốn người. Ở chế độ `SAMPLE`, mỗi phiếu QA chỉ chấm một mẫu ngẫu nhiên: mẫu đạt ngưỡng thì cả phiếu đạt,
vượt ngưỡng thì trả cả phiếu. Chuỗi task: **18B nền tảng (file này)** → 18D thuật toán bốc mẫu → 18C giao QA + chấm mẫu. 18D, 18C viết
tiếp vào cuối file này. Code nằm ở `domain/Module/Construction/QaAssignments/`, `QaAssignmentService`, `QaAssignmentController`.

## Các chức năng

| Endpoint (`api/v1/construction/qa-assignment/...`) | Quyền | Việc |
|---|---|---|
| `GET config?projectId=` | `Function.Construction.ProjectConfig.View` | Cấu hình mẫu của dự án; chưa có thì trả giá trị mặc định với `isDefault = true` |
| `PUT config` | `...ProjectConfig.Update` | Tạo hoặc sửa (upsert) `{ projectId, sampleSize, thresholdPercent, strategyCode, strategyParamsJson, stopEarlyOnReject }` |
| `GET reviewer-mode?projectId=` | `...ProjectConfig.View` | QA đã được đặt chế độ trong dự án: `userId`, `userName`, `fullName`, `mode`, `modifiedDate` |
| `PUT reviewer-mode` | `...ProjectConfig.Update` | Đặt chế độ `{ projectId, userId, mode }` |

Dùng lại quyền cấu hình dự án, không thêm key cho 4 API này. Chỉ kiểm quyền, không chặn thêm theo role Admin (chốt 2026-10-26):
hiện chỉ Admin có `ProjectConfig.Update`; gán quyền này cho Leader thì Leader cũng đặt được.

## R1. Chế độ chấm theo từng QA trong từng dự án

- `(ProjectId, UserId)` → `FULL` hoặc `SAMPLE` (`QaAssignmentConst.MODES`), bảng `QaReviewerMode`. Chưa đặt = `FULL`
  (`QaAssignmentDomainService.GetModeAsync`). `FULL` = chấm mọi văn bản được giao; `SAMPLE` = chỉ chấm mẫu của phiếu (18C).
- Người được đặt phải tồn tại (`103`) và có role `QA` (không có → `400` "Người dùng không có vai trò QA",
  `QaAssignmentDomainService.EnsureQaReviewerAsync`). Mode ngoài 2 giá trị → `400`.
- Đặt lại thì sửa dòng cũ (unique `(ProjectId, UserId)` lọc `[Deleted] = 0`), audit `Đặt chế độ chấm QA` có dữ liệu cũ / mới,
  nội dung "Đặt chế độ {mode} cho {userName} ở dự án {project}".
- `GET reviewer-mode` sắp theo `userName`; `modifiedDate` = lần đặt gần nhất (chưa sửa lần nào thì là ngày tạo). User đã mất role QA
  vẫn hiện, không lọc lại.
- Đổi chế độ chỉ áp cho phiếu giao **sau** đó: phiếu chụp chế độ lúc giao (18C).
- Task 11 của Operation (API thêm QA vào dự án) gọi lại `UpdateReviewerMode` để chọn chế độ khi thêm QA.

## R2. Cấu hình mẫu theo dự án (không bắt buộc)

Cấu hình theo **dự án**, không tách theo loại văn bản (mỗi dự án chỉ có một loại). `FULL` không cần cấu hình. Dự án chưa cấu hình
thì dùng mặc định (`QaSamplingConfig.Default`, không lưu DB):

| Trường | Ý nghĩa | Mặc định | Ràng buộc |
|---|---|---|---|
| `SampleSize` | Số văn bản bốc để chấm trong một phiếu | `20` | `1`–`5000` |
| `ThresholdPercent` | % văn bản mẫu sai tối đa để phiếu vẫn đạt | `50` | `0`–`100`, tối đa 2 chữ số thập phân, `decimal(5,2)` |
| `StrategyCode` | Mã thuật toán bốc mẫu | `SIMPLE_RANDOM` | `^[A-Z0-9_]{1,30}$` |
| `StrategyParamsJson` | Tham số riêng của thuật toán | `null` | `null` hoặc JSON object, ≤ 1000 ký tự; chuỗi rỗng / khoảng trắng coi là `null` |
| `StopEarlyOnReject` | Số sai đã vượt ngưỡng thì chốt trượt luôn, không chấm hết mẫu | `true` | – |

- Ngưỡng mặc định 50% (chốt 2026-10-26): mẫu 20 cho sai tối đa 10.
- Kiểm cỡ mẫu và ngưỡng nằm trong entity (`QaSamplingConfig.Create` / `Update`). Kiểm mã và tham số thuật toán nằm ở
  `IQaStrategyValidator` (R4). Sai → `400` kèm thông điệp cụ thể. Dự án không có → `303`.
- Unique `(ProjectId)` lọc `[Deleted] = 0`: sửa lần hai vẫn một dòng. Audit `Cập nhật cấu hình chấm mẫu`: lần đầu `Create`, sau đó
  `Update` có dữ liệu cũ / mới.
- Đổi cấu hình **không** ảnh hưởng phiếu đã giao: phiếu chụp tham số lúc giao (18C).

## R3. Cỡ mẫu và số sai cho phép

`QaSampleSizePolicy.Calculate(documentCount, sampleSize, thresholdPercent)` là hàm thuần; 18C gọi lúc giao phiếu và lúc bốc lại.

```text
n         = min(SampleSize, số văn bản của phiếu)
MaxFailed = floor(n × ThresholdPercent / 100)
```

| Phiếu | Cỡ mẫu | Ngưỡng | Kết quả `(n, MaxFailed)` |
|---|---|---|---|
| 200 văn bản | 20 | 50% | `(20, 10)` |
| 12 văn bản | 20 | 50% | `(12, 6)` |
| 200 văn bản | 20 | 5% | `(20, 1)` |
| 12 văn bản | 20 | 5% | `(12, 0)` |
| 200 văn bản | 20 | 10% / 0% | `(20, 2)` / `(20, 0)` |

## R4. Điểm móc cho thuật toán bốc mẫu

- `IQaStrategyValidator.Validate(strategyCode, paramsJson)` khai ở domain. Bản mặc định `FormatOnlyQaStrategyValidator`
  (đăng ký `AddSingleton`) chỉ kiểm hình thức như bảng R2.
- `QaAssignmentService.UpdateConfig` gọi `Validate` **trước** khi lưu. Task 18D chỉ đổi đăng ký DI sang bản kiểm theo danh sách
  thuật toán thật, **không** sửa service.
- ⚠️ Bản thay thế ở 18D phải tự kiểm độ dài `StrategyCode` (30) và `StrategyParamsJson` (1000). Entity không kiểm lại hai cột này;
  giá trị dài hơn sẽ lỗi khi lưu DB.

## Schema (migration `Add_QaAssignment`)

| Bảng | Ghi chú |
|---|---|
| `QaSamplingConfig` | Cấu hình R2, FK `Project`, xoá mềm |
| `QaReviewerMode` | Chế độ R1, FK `Project`, xoá mềm |
| `QaTicket` | Phiếu QA, tạo sẵn cho 18C (chỉ có cột và `Create`): chụp `Mode`, `SampleSize`, `MaxFailed`, `ThresholdPercent`, `StrategyCode` / `Params`, `Seed`, `StopEarlyOnReject` lúc giao; `Round` từ 1, `Status` `ACTIVE` / `PASSED` / `REJECTED` / `CANCELLED`, các bộ đếm. FK `Project`, `DocumentType`, xoá mềm |
| `SubmissionUser` | Người được giao của văn bản, dùng chung cho QA (18C) và người nhập (Operation Task 9) |

`SubmissionUser`: một dòng cho mỗi `(văn bản, vai trò)`. `Role` lấy từ `RoleConstants` (`Data` hoặc `QA`, ngoài hai giá trị thì `400`).
`TicketId` trỏ `operation.AssignmentTicket` (Data) hoặc `construction.QaTicket` (QA). `IsSample` chỉ dùng cho QA, `ReturnCount` chỉ
dùng cho Data. Unique `(SubmissionId, Role)` **không** filter, nên DB tự chặn giao trùng. Bảng `Submission` không thêm cột nào
(chốt 2026-10-26: `Submission` chỉ giữ dữ liệu văn bản).

⚠️ `TicketId` không có FK vì trỏ sang hai bảng tuỳ `Role`, nên luôn lọc kèm `Role`. **Đừng** "sửa" bằng cách thêm FK.

⚠️ `SubmissionUser` không xoá mềm: thu hồi hoặc huỷ phiếu là **xoá cứng** dòng; lịch sử nằm ở phiếu và audit. Thêm xoá mềm sẽ
làm unique không filter chặn lần giao lại.

⚠️ Operation Task 9 dùng `SubmissionUser` với role `Data` và không thêm cột vào `Submission`, nên phải merge task này **trước** Task 9.

## Mã lỗi khai sẵn cho 18C

| Mã | Hằng | Thông điệp |
|---|---|---|
| `327` | `ConstructionErrorQaTicketNotFound` | Không tìm thấy phiếu QA |
| `328` | `ConstructionErrorQaNoSubmissionToAssign` | Không có văn bản phù hợp để giao QA |
| `329` | `ConstructionErrorQaTicketTooLarge` | Một phiếu QA tối đa 5000 văn bản |
| `330` | `ConstructionErrorQaAssignConflict` | Có văn bản vừa được giao QA cho người khác, hãy thử lại |
| `331` | `ConstructionErrorQaSameAsEntry` | Người QA không được trùng người nhập liệu của văn bản |
| `332` | `ConstructionErrorQaSubmissionLocked` | Văn bản đang trong phiếu QA chấm mẫu, không thao tác riêng được |
| `333` | `ConstructionErrorQaSubmissionNotSample` | Văn bản không nằm trong mẫu cần chấm |
| `334` | `ConstructionErrorQaNotAssignee` | Văn bản không được giao QA cho bạn |
| `335` | `ConstructionErrorQaTicketStatusInvalid` | Phiếu QA không ở trạng thái cho phép thao tác này |
| `336` | `ConstructionErrorSubmissionInProgressNotDeletable` | Không được xoá văn bản đang sửa hoặc đang QA (18C) |
| `337` | `ConstructionErrorProjectNotManaged` | Bạn không được phân quản lý dự án / loại văn bản này (18C) |

Key quyền `Function.Construction.Submission.AssignQa` ("Giao QA cho văn bản") đã seed sẵn cho 18C. Admin tự có; Admin gán thêm
cho Leader.

## Kiểm thử (2026-10-26)

Gọi thật trên DB dev, dữ liệu gắn tag `ZZTEST`, xoá sạch sau khi test: **37/37 đạt**. Gồm: mặc định khi chưa cấu hình; tạo rồi sửa
vẫn một dòng, audit cũ / mới; 11 trường hợp sai → `400`; dự án không có → `303`; đặt `SAMPLE` rồi `FULL` cho QA; user không có role
QA, mode `X` → `400`; user không có → `103`; không quyền → `403`; không token → `401`. Chèn trùng `(SubmissionId, "QA")` thì DB
báo lỗi unique. `QaSampleSizePolicy` đạt cả 6 trường hợp ở bảng R3. Bộ test Task 18 vẫn đạt 50/50.

## 18C — Phiếu QA

Chặng 1 (2026-10-26): giao phiếu **FULL**, danh sách, chi tiết, lọc hồ sơ theo phiếu. Chấm theo phiếu, tự chốt, huỷ, chặn xoá làm ở
chặng 2–3; phiếu `SAMPLE` chờ 18D. Mỗi lần giao = 1 `QaTicket` của **đúng 1** QA + 1 dòng `SubmissionUser` Role `QA` cho từng văn bản.

| Endpoint (`api/v1/construction/qa-assignment/...`) | Quyền | Việc |
|---|---|---|
| `POST ticket` | `...Submission.AssignQa` | `{ projectId, documentTypeId, qaUserId, items?: [{ box, profileRange? }], count? }`, trả chi tiết phiếu |
| `GET ticket/paging` | `AssignQa` hoặc `Qa` | Lọc `projectId`, `documentTypeId`, `qaUserId`, `mode`, `status`; mới nhất trước |
| `GET ticket/{id:int}` | `AssignQa` hoặc `Qa` | Tham số chụp, vòng, số đếm, `passedCount`, `samples` (rỗng với phiếu FULL; trường mẫu `null`) |

**Giao (R1).** Gửi **đúng một** trong `items` / `count`, sai → `400`. `items`: `box` bắt buộc, `profileRange` bỏ trống = cả hộp, có thì
chỉ hồ sơ đó trong hộp đó. `count` `1`–`5000`: lấy N văn bản đầu theo `Box`, `ProfileRange`, `Id`.
- Văn bản phù hợp: chưa xoá, `StatusCheck = 1`, `StatusQa = 0`, chưa có dòng `SubmissionUser` Role `QA`. Không có → `328`, > 5000 → `329`.
- QA phải có role `QA` (`400`, không tồn tại `103`). Có văn bản mà người nhập (log kiểm tra mới nhất, luật Task 18) là chính QA → `331`.
- Dự án `DONE` / `EXPIRED`: người thường `316` / `323`; Admin, SuperAdmin giao được (`EnsureEditableProjectAsync`).
- Trước 18D: QA đang chế độ `SAMPLE` → `400` "Chưa hỗ trợ giao phiếu chế độ SAMPLE".
- Một transaction: thêm phiếu → `SaveChanges` lấy `Id` → bulk insert dòng giao → audit `Giao phiếu QA` (dữ liệu mới có danh sách
  `SubmissionIds`). Bảng `Submission` **không** đổi.
- Hai người giao trùng văn bản cùng lúc: unique `(SubmissionId, Role)` chặn, lỗi SQL `2601` / `2627` → `330`, rollback cả phiếu.
  Request đến sau khi phiếu kia đã commit thì không còn văn bản phù hợp → `328`.

⚠️ Sắp `Box`, `ProfileRange` theo **chuỗi** (collation DB, như 5sdb): `"10"` đứng trước `"2"`. Đừng ép kiểu số — thứ tự phải khớp cây
Hộp → Hồ sơ đang hiển thị.

**Phạm vi (R10).** Admin = role `Admin` hoặc `SuperAdmin`: mọi dự án. Người khác chỉ thao tác trên cặp (dự án, loại văn bản) mình là
`operation.ProjectMember` Role `Leader`, `IsActive`, chưa gỡ (Construction đọc thẳng bảng này trong `QaTicketRepository`):
giao ngoài cặp → `337`; danh sách chỉ gồm phiếu của cặp được phân **cộng** phiếu mà mình là QA; xem phiếu không được thấy → `327`
(không lộ là có phiếu). QA xem phiếu của mình không cần là `ProjectMember`.

**Lọc hồ sơ theo phiếu.** Bộ lọc chung có `qaTicketId` ([12-ho-so-kiem-tra.md](12-ho-so-kiem-tra.md#bộ-lọc-chung)); kiểm quyền xem
phiếu như trên, không thêm key quyền. Đây là cửa vào màn chấm của phiếu FULL.

Kiểm thử chặng 1 (DB dev, `ZZTEST`, xoá sạch sau test): **63/63 đạt** — giao theo hộp / hồ sơ / số lượng, các lỗi `400` / `103` / `328` /
`331`, `SAMPLE` → `400`, giao trùng đồng thời (một lần ra `330`, một lần `328`), quyền xem, phạm vi Leader (`337`, `327`, `316`,
`IsActive = 0`, đã gỡ), lọc paging / count / next / previous / qa-paging / tree theo phiếu.

> **Tiếp:** [19-nhom-nhan-vien.md](19-nhom-nhan-vien.md) — nhóm nhân viên của module Operation: nhóm thuần Data hoặc QA, Leader chỉ thấy nhóm mình.
