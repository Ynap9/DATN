# Hồ sơ — năng suất theo người (progress-stats/users)

> **Phần 31/31** · trước: [30-import-ghep-truong.md](30-import-ghep-truong.md) · mục lục: [README.md](README.md)

Task 70 (2026-11-27). Khối "Năng suất theo người nhập" của Báo cáo tiến độ trước đây đọc `qa-stats/entry-users` nên chỉ đếm văn bản
**đã được QA chấm**: dự án không QA không có khối này, người kiểm 80 văn bản mà QA mới chấm 10 thì chỉ hiện 10. API mới đếm **sản lượng
theo người trong khoảng ngày, không cần QA**. Chỉ đọc: không transaction, không audit, không đổi DB (không migration), không key quyền
mới, không mã lỗi mới.

## API

| Endpoint (`api/v1/construction/submission/...`) | Quyền | Việc |
|---|---|---|
| `GET progress-stats/users` | `Function.Construction.Submission.ProgressStats` (cùng key với [daily](17-thong-ke-qa.md#tiến-độ-theo-ngày--progress-statsdaily-task-46a)) | Số văn bản, số ô, số lượt chấm của từng thành viên Data / QA trong một cặp dự án – loại văn bản |

Query dùng lại `FindProgressStatsDto`: `projectId`, `documentTypeId` (bắt buộc), `fromDate`, `toDate`. Khoảng ngày chuẩn hoá bằng
`NormalizeQaStatsDateRange` (mặc định 30 ngày, tối đa 366, `from > to` → `400`); cặp không hợp lệ `303` / `301` / `312` như daily. Không phân
trang (thành viên một cặp ít).

## Ai được tính cho một lượt

Người được tính **không phải lúc nào cũng là người bấm** (Admin / Leader bấm hộ vẫn tính cho nhân viên đang giữ văn bản, chốt 2026-11-27):

| Người bấm (`log.UserId`) | Được tính cho |
|---|---|
| Có dòng `ProjectMember` đúng vai trò (Data cho lượt kiểm, QA cho lượt chấm) của cặp, kể cả dòng đã gỡ / tắt | Chính người bấm |
| Không có (Admin, SuperAdmin, Leader, người ngoài) | Người **đang giữ** văn bản vai trò đó (`SubmissionUser.Role`) |
| Không có ai giữ | Dòng "Không gắn người" (`userId = null`) |

- Lấy người giữ **hiện tại** (không đổi DB nên không có người giữ lúc bấm): `SubmissionUser` bị xoá cứng khi về pool / thu hồi / chuyển.
- **Lượt kiểm (Data):** `SubmissionLog` của cặp có `NewStatus = CHECKED` và `OldStatus <> CHECKED` (cùng luật `QueryCheckedDays`, bỏ tái kiểm),
  `CreatedDate` trong khoảng, văn bản chưa xoá.
- **Lượt chấm (QA):** `SubmissionQaLog` của cặp trong khoảng, văn bản chưa xoá, **bỏ** lượt Đạt → Đạt (`OldStatusQa = NewStatusQa = QA_PASSED`,
  cùng luật tiền QA, [24](24-don-gia-tien-cong.md)). Dự án không QA (`IsQaEnabled = false`) → `qaUsers = []`.

## Chỉ số mỗi dòng

| Mảng | Trường | Cách đếm |
|---|---|---|
| `dataUsers` | `documentCount` | Số văn bản **khác nhau** của các lượt kiểm được tính cho người đó |
| | `fieldCount` | Tổng ô có dữ liệu (`Data.Trim() <> ""`) của trường `IsChargeable` trên các văn bản đó, đọc **giá trị hiện tại** (như K1 tiền công), mỗi văn bản cộng một lần |
| `qaUsers` | `reviewCount`, `reviewDocumentCount` | Số lượt chấm; số văn bản khác nhau |
| | `passedCount`, `failedCount` | Theo `NewStatusQa` |
| cả hai | `workingDays` | Số ngày khác nhau có lượt được tính |

Một văn bản do hai người kiểm trong khoảng (bị trả rồi người khác nhận) có ở **cả hai dòng**, mỗi dòng cộng đủ số ô, **không chia**.

## Dòng nào có mặt

- Mọi thành viên Data (`dataUsers`) / QA (`qaUsers`) **chưa gỡ** của cặp đều có dòng, kể cả `0` (Leader thấy người chưa làm). Thành viên bị tắt
  (`IsActive = false`) nhưng chưa gỡ vẫn có dòng `0`; response không có trường `isActive`.
- Thành viên **đã gỡ** chỉ có dòng khi số > 0, kèm `isRemoved = true`. Dòng "Không gắn người" chỉ có khi > 0 và đứng **cuối**.
- User đã xoá cứng khỏi hệ thống (còn là người giữ): dòng riêng theo `userId`, `userName` / `fullName` là `null` (như `entry-users`).
- Người giữ văn bản nhưng không có dòng thành viên nào: dòng riêng, `isRemoved = false`.
- Sắp: `dataUsers` theo `documentCount` giảm dần → `fieldCount` giảm dần → `userName`; `qaUsers` theo `reviewCount` giảm dần → `userName`.

## Phạm vi người xem

`ResolveRestrictedUserIdAsync` như daily. Khác `null` (Data / QA thiếu `ViewAllSubmission`) → chỉ trả dòng có `userId` = người xem, ở cả hai mảng nếu có,
**không** trả "Không gắn người"; lượt Leader kiểm hộ văn bản mình đang giữ vẫn tính cho mình. Admin / Leader của cặp → đủ.

```jsonc
{ "fromDate": "...", "toDate": "...", "isQaEnabled": true,
  "dataUsers": [{ "userId": "...", "userName": "...", "fullName": "...", "isRemoved": false, "documentCount": 2, "fieldCount": 6, "workingDays": 1 }],
  "qaUsers":   [{ "userId": "...", "userName": "...", "fullName": "...", "isRemoved": false, "reviewCount": 3, "reviewDocumentCount": 3,
                  "passedCount": 2, "failedCount": 1, "workingDays": 1 }] }
```

## Khác tiền công (cố ý)

[Tiền công](24-don-gia-tien-cong.md) tính theo người **đang giữ** + trạng thái **hiện tại** của văn bản (K1, K2); năng suất tính theo **lượt trong khoảng ngày**.
Số hai nơi có thể lệch nhau (ví dụ văn bản về pool sau khi kiểm, hoặc lượt kiểm nằm ngoài khoảng ngày): không phải lỗi.

## Code

- `SubmissionQuery.FindProgressUserStats` (cùng khuôn `FindProgressDailyStats`); DTO ở `Dtos/Submission`: `ViewProgressUserStatsDto`,
  `GetListProgressDataUserDto`, `GetListProgressQaUserDto`, lớp nền `ProgressUserDto` (`UserId`, `UserName`, `FullName`, `IsRemoved`, `WorkingDays`).
- Đếm ở SQL (`GroupBy`, `Distinct().Count()`, ô có dữ liệu là subquery trên từng cặp người – văn bản), không nạp từng log về bộ nhớ, không `Contains` danh sách id lớn
  (giới hạn 2100 tham số, xem [17](17-thong-ke-qa.md)). "Người được tính" viết một lần cho mỗi nguồn (kiểm / QA) bằng biểu thức
  `ProjectMembers.IgnoreQueryFilters().Any(...) ? log.UserId : người giữ`.
- `FindProgressMembersAsync` (thành viên kể cả đã gỡ, `IsRemoved = không còn dòng chưa gỡ`) và `MergeProgressUserRows<TRow>` (ghép dòng có số với thành viên) dùng chung cho Data và QA.

Lưu ý: dòng "Không gắn người" có `userId = null`, nên tra số ô theo người bằng `ToLookup` (cho phép khoá `null`), **không** dùng `Dictionary`.

## Đã kiểm thực tế

Ngày 2026-11-27, API thật trên DB dev chung, dự án tạm `ZZTEST70` (có QA) + `ZZTEST70B` (không QA), 1 metadata 4 trường (3 trường tính tiền), thành viên Data d1 / d2 / d3,
d4 đã gỡ, QA q1, một Leader, một user thiếu key; đã dọn sạch — **30/30 đạt**:

- d1 kiểm V1, V2 (đủ 4 ô): 2 văn bản / 6 ô / 1 ngày; d2 kiểm V3 (2 ô tính tiền): 1 / 2 / 1; d1 lưu lại V1 → không đổi. Leader kiểm hộ V4 (đang giao d2) → d2 thành 2 / 5, Leader không có dòng;
  Admin kiểm V5 chưa giao ai → "Không gắn người" 1 văn bản, đứng cuối; người giữ đã xoá cứng → dòng riêng `userName = null`.
- d3 chưa làm gì có dòng `0`; d4 (đã gỡ, 0) không có dòng; gỡ d1 → d1 vẫn có dòng `isRemoved = true`. Khoảng ngày không chứa lượt → mọi dòng thành viên `0`, không có "Không gắn người".
- q1 chấm V1 Đạt, V3 Không đạt, Admin chấm V2 (q1 giữ) Đạt → q1: 3 lượt / 3 văn bản / 2 Đạt / 1 Không đạt; Admin chấm Đạt lại V1 → không đổi; Admin không có dòng QA.
- `ZZTEST70B`: `qaUsers = []`, `isQaEnabled = false`. d2 đăng nhập → chỉ dòng d2; q1 → chỉ dòng QA q1; Leader → đủ; role User → `403`.
- `fromDate > toDate` → `400`; 367 ngày → `400`, 366 ngày → `200`; metadata ngoài dự án → `312`; không gửi ngày → mặc định 30 ngày.

> **Tiếp:** hết — quay lại [README.md](README.md).
