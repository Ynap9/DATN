# Nghiệm thu dự án (Construction)

> **Phần 22/22** · trước: [21-thanh-vien-du-an.md](21-thanh-vien-du-an.md) · mục lục: [README.md](README.md)

Task 23 (2026-11-11): dự án đi `DEPLOYING` / `DOING` → `ACCEPTING` (đang nghiệm thu) → `DONE`. Tự vào nghiệm thu khi văn bản
cuối cùng QA đạt; Admin cho vào khi chưa đủ điều kiện (bắt buộc lý do). Đang nghiệm thu thì khoá import. **Không đổi schema**:
bằng chứng nghiệm thu nằm trong `AuditLog`.

## Case đã chốt

| # | Case | Chốt |
|---|---|---|
| 23.1 | Ai chuyển sang `ACCEPTING` khi đủ | **Tự động** khi văn bản cuối cùng QA đạt + người có quyền bấm tay |
| 23.2 | "Đủ điều kiện" | Mọi văn bản chưa xoá của dự án đã `CHECKED` **và** `QA_PASSED`, dự án có ≥ 1 văn bản |
| — | Chưa đủ | Chỉ Admin / SuperAdmin cho qua (`isForced`). Bắt đầu: lý do **tuỳ chọn** (có thì ≤ 1000 ký tự); hoàn tất: lý do bắt buộc 10–1000 ký tự |
| — | Ai bấm tay | Bắt đầu / hoàn tất: Admin / SuperAdmin mọi dự án, Leader chỉ dự án mình đang là Leader (≥ 1 loại văn bản, `ProjectMember` `IsActive`). Trả về: **chỉ Admin** |
| — | Dự án 0 văn bản | **Chặn hẳn** (`340`), kể cả Admin `isForced` |
| 23.3 | Khoá gì khi `ACCEPTING` | Khoá import (`341`); kiểm tra / QA vẫn làm (làm nốt, sửa lỗi nghiệm thu) |
| — | Văn bản của loại văn bản đã gỡ khỏi dự án | **Tính hết** — không lọc theo `ProjectDocumentType` |
| — | Chế độ QA `SAMPLE` | Vẫn đòi mọi văn bản `QA_PASSED`; để Task 18D quyết lại (⚠️ dưới) |

## Chuyển trạng thái

| Thao tác | Từ | Sang | Điều kiện |
|---|---|---|---|
| Tự chuyển (trong `PUT submission/qa`) | `DEPLOYING`, `DOING` | `ACCEPTING` | Kết quả QA là Đạt và dự án đủ điều kiện |
| `POST {id:int}/acceptance/start` | `DEPLOYING`, `DOING` | `ACCEPTING` | Đủ, hoặc Admin `isForced` (lý do tuỳ chọn) |
| `POST {id:int}/acceptance/finish` | `ACCEPTING` | `DONE` | Đủ, hoặc Admin `isForced` + lý do |
| `POST {id:int}/acceptance/reopen` | `ACCEPTING` | `DOING` | Chỉ Admin, bắt buộc lý do |

Không bao giờ tự sang `DONE`, không tự hạ `ACCEPTING` về `DOING`. **Chỉ** tự chuyển trong `ReviewQa`: xoá văn bản cuối cùng chưa
đạt không tự chuyển — bấm "Bắt đầu" (lúc đó đã đủ, không cần lý do). Một chỗ tự chuyển thì dễ kiểm.

`PUT project/{id}` **không** được vào / ra `ACCEPTING` hay đặt `DONE` (`342` "Dùng chức năng nghiệm thu", kiểm trong `Project.Update`).
Giữ nguyên `status` (kể cả `ACCEPTING` / `DONE`) để sửa tên, ngày… vẫn được; `EXPIRED` → `DOING` vẫn như cũ.

## API

Gốc `api/v1/construction/project`.

| Endpoint | Quyền | Body |
|---|---|---|
| `GET {id:int}/acceptance` | `Function.Construction.Project.View` | — |
| `POST {id:int}/acceptance/start`, `.../finish` | `Function.Construction.Project.Accept` | `{ isForced, reason }` |
| `POST {id:int}/acceptance/reopen` | `Function.Construction.Project.Accept` | `{ reason }` |

Ba API chuyển trả lại đúng dữ liệu `GET acceptance` sau khi chuyển. `GET` trả `ViewProjectAcceptanceDto`: `projectId`, `projectName`,
`status`, `isReady`, `canForce` (người gọi là Admin / SuperAdmin), `documentTypes` (mỗi loại văn bản một dòng), `total` (dòng tổng),
`lastTransition`. Mỗi dòng (`GetListProjectAcceptanceSummaryDto`): `documentTypeId`, `documentTypeName`, `isRemoved`,
`totalSubmissions`, `totalNotChecked`, `totalChecked`, `totalReturned`, `totalQaNotReviewed` / `totalQaPassed` (chỉ đếm trong
`CHECKED`), `totalQaFailed`, `qaPassedPercent` (= QA đạt / tổng, 1 chữ số thập phân). Loại đã gỡ mà còn văn bản vẫn hiện
(`isRemoved = true`, xếp cuối) để dòng tổng bằng tổng các dòng. Tỉ lệ lỗi theo trường: dùng API thống kê QA
([17-thong-ke-qa.md](17-thong-ke-qa.md)).

## Luật và mã lỗi

Thứ tự kiểm trong `ProjectService`: dự án không có `303` → người gọi (`EnsureCanAcceptAsync`: không phải Admin và không là Leader
đang hoạt động của dự án → `337`; reopen `EnsureCanReopen`: không phải Admin → `400` "Chỉ Admin được trả dự án về đang làm") → sai
trạng thái nguồn `342` (entity `StartAcceptance` / `FinishAcceptance` / `ReopenFromAcceptance`) → đếm rồi `EnsureAcceptanceAllowed`:

| Tình huống | Kết quả |
|---|---|
| Dự án 0 văn bản | `340` "Dự án chưa có văn bản nào" — kể cả Admin `isForced` |
| Đủ điều kiện | Qua; bỏ qua cờ `isForced`, không bắt lý do (gửi kèm thì lưu bản đã trim) |
| Chưa đủ, không `isForced` | `340` "Còn {a} văn bản chưa kiểm, {b} văn bản chưa QA đạt" |
| Chưa đủ, `isForced`, không phải Admin | `400` "Chỉ Admin được cho phép khi chưa đủ điều kiện" |
| Chưa đủ, Admin, `finish` / `reopen` lý do sau trim ngoài 10–1000 ký tự | `400` "Lý do phải từ 10 đến 1000 ký tự" |
| `start` (hoặc đã đủ điều kiện) gửi lý do dài quá 1000 ký tự | `400` "Lý do tối đa 1000 ký tự"; rỗng / toàn khoảng trắng → lưu `null` |

`a` = văn bản chưa `CHECKED` (gồm trả lại), `b` = đã `CHECKED` nhưng chưa `QA_PASSED` — tách rời, không đếm trùng
(`ProjectAcceptanceBlockers`, một câu `GROUP BY` ở `ProjectRepository.CountAcceptanceBlockersAsync`).
`341` "Dự án đang nghiệm thu, không import được": `Project.EnsureImportable`, kiểm trước `313` / `323`.

## Tự chuyển trong `ReviewQa`

Sau khối chốt phiếu FULL, trong **cùng transaction**, chỉ khi kết quả là Đạt: `ProjectDomainService.TryStartAcceptanceAsync` —
dự án `DEPLOYING` / `DOING` → khoá dòng `Project` (`ProjectRepository.LockAsync`, `UPDLOCK, HOLDLOCK`) → đếm → đủ thì
`TryStartAcceptanceAsync` (UPDATE có điều kiện trạng thái) trả `1` mới gọi `project.StartAcceptance`, rồi ghi audit
`IsAutomatic = true`, người ghi = người vừa QA.

⚠️ Bẫy: DB bật `READ_COMMITTED_SNAPSHOT`, nên hai request QA đạt **song song** hai văn bản cuối cùng thấy văn bản kia "chưa đạt" và
**không ai** chuyển (đã tái hiện 1/3 vòng trên DB dev). Khoá dòng dự án trước khi đếm buộc request sau chờ request trước commit;
đừng bỏ khoá hay dời việc đếm lên trước khoá. UPDATE có điều kiện giữ đúng 1 request chuyển.

## Audit — bằng chứng nghiệm thu

Action `Bắt đầu nghiệm thu` / `Hoàn tất nghiệm thu` / `Trả dự án về đang làm` (`AuditLogConstants.ACTION_PROJECT_ACCEPTANCE_*`), type
`Update`. `oldData { ProjectId, Status }`; `newData { ProjectId, Status, IsForced, IsAutomatic, Reason, Summary }` — `IsForced = !IsReady`
lúc chuyển, `Summary` = dòng tổng của `GET` lúc chuyển. `GET acceptance` đọc audit mới nhất để ra `lastTransition` (`action`,
`createdDate`, `userName`, `fullName`, `oldStatus`, `newStatus`, `isForced`, `isAutomatic`, `reason`).

⚠️ `AuditLog` không có cột `ProjectId`: lần chuyển gần nhất lọc `NewData LIKE '{"ProjectId":<id>,%'`. `ProjectId` **phải** là trường
**đầu tiên** của `newData` (serializer PascalCase của `AuditLogUtils`); thêm trường lên trước là `lastTransition` thành `null`.

## Đã kiểm thực tế

Ngày 2026-11-11 trên DB dev (dữ liệu `ZZTEST23`, xoá sạch sau test): bấm tay **48/48** — `GET` đúng số từng loại và dòng tổng,
`canForce` theo Admin / Leader; `340` (kèm số), `341`, `342`, `303`, `400`, `401`; Admin force có lý do (đã trim) → audit `IsForced`;
đang `ACCEPTING` kiểm tra / QA vẫn được; luật `PUT`; loại đã gỡ `isRemoved` vẫn chặn; dự án 0 văn bản. Tự chuyển **15/15**: văn bản
áp cuối không chuyển, văn bản cuối → `ACCEPTING` + 1 audit tự động, QA khi `ACCEPTING` không thêm audit, dự án `EXPIRED` không
chuyển; 6/6 vòng QA song song hai văn bản cuối → đúng 1 audit.
Chạy lại sau khi siết quyền (Leader chỉ dự án mình quản lý, reopen chỉ Admin, chặn dự án rỗng) — **55/55** + **15/15**: Leader chưa
là Leader của dự án / Leader `IsActive = 0` → `337`; Leader reopen → `400`, dự án vẫn `ACCEPTING`; dự án rỗng Admin force → `340`.
Ngày 2026-11-12, bắt đầu cho lý do tuỳ chọn — **10/10** trên dự án tạm: Admin force không lý do → `ACCEPTING`, audit `Reason: null`;
lý do `ok` → lưu `ok`; 1001 ký tự → `400`; hoàn tất force không lý do vẫn `400`.

## Giới hạn

⚠️ Hai Admin bấm "Bắt đầu" cùng lúc: bấm tay không khoá như tự chuyển, có thể ra 2 audit (trạng thái vẫn đúng). Chấp nhận vì hiếm.

⚠️ QA chế độ `SAMPLE` không QA hết văn bản nên dự án không tự vào nghiệm thu, phải Admin force — Task 18D quyết định lại "đủ điều kiện".

⚠️ Khoá dòng dự án làm các lần QA đạt trên cùng dự án `DEPLOYING` / `DOING` xếp hàng ở bước cuối (khoá giữ tới commit, ngắn).

> **Tiếp:** [23-chia-phieu.md](23-chia-phieu.md) — cây Hộp → Hồ sơ, giao phiếu nhập liệu, QA phụ trách nút + đợt QA, phiếu của tôi.
