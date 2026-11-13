# Chia phiếu nhập liệu và QA (Operation)

> **Phần 23/26** · trước: [22-nghiem-thu-du-an.md](22-nghiem-thu-du-an.md) · mục lục: [README.md](README.md)

Admin hoặc Leader của cặp (dự án, loại văn bản) xem cây **Hộp → Hồ sơ → Văn bản**, chọn người nhận, tích nút, xem trước rồi lưu.
Data nhận **phiếu nhập liệu**; QA nhận **phụ trách nút** và văn bản đã kiểm vào **đợt QA**. Task 13, 2026-11-11. Không đổi schema,
dùng lại `AssignmentTicket`, `AssignmentTicketItem` (Task 9), `QaTicket`, `SubmissionUser` (Task 18C). Ba bảng này có cờ xoá mềm từ Task 29 nhưng
chỉ dùng khi Admin xoá dự án ([25](25-xoa-du-an-danh-sach-cap.md)); về pool / gỡ thành viên vẫn xoá cứng.

## Cây

- Nút = cặp `(Submission.Box, Submission.ProfileRange)`; cùng tên hồ sơ ở 2 hộp là 2 nút. Văn bản **thiếu hộp hoặc hồ sơ** (`""`) vẫn
  là nút, vẫn giao được (FE hiện "(Không có hộp)" / "(Không có hồ sơ)").
- So khớp hộp / hồ sơ **không phân biệt hoa thường** ở mọi chỗ: SQL dựa collation CI của DB, C# dùng `AssignmentDomainService.ToNodeKey`
  (trim + `ToUpperInvariant`). Tên hiển thị của nút gộp = giá trị nhỏ nhất theo collation nhị phân (`H1` thắng `h1`).
- Sắp tự nhiên `AssignmentDomainService.CompareNatural`: đoạn số so theo giá trị, đoạn chữ không phân biệt hoa thường
  (`"" < 2 < 10 < A2 < A10 < B2`). Sắp và phân trang trong bộ nhớ trên danh sách nút đã gộp ở SQL, rồi mới lấy người giữ cho trang đó.
- Cấp Hộp `pageSize` 1–200 bắt buộc; cấp Hồ sơ mặc định 200. Lọc: `keyword` (chứa trong hộp hoặc hồ sơ), `onlyUnassigned`, `holderUserId`.
- Admin thấy mọi cặp; Leader thấy **toàn bộ** cây của cặp mình là Leader (cả phần người khác giữ).

| Số liệu | Chế độ Data (mọi văn bản) | Chế độ QA (chỉ nút có ≥ 1 văn bản `CHECKED`) |
|---|---|---|
| `Total` | Số văn bản | Số văn bản `CHECKED` |
| `Assigned` | Có người giữ Data | `CHECKED` có người giữ QA |
| `Unassigned` | `Total − Assigned` (gồm văn bản về pool) | `CHECKED`, `QA_NOT_REVIEWED`, chưa ai giữ QA |
| `NotChecked` | `StatusCheck = NOT_CHECKED` | Văn bản chưa `CHECKED` của nút (còn đang nhập) |
| `OwnerUserId` | — | QA phụ trách nút; cấp Hộp chỉ có khi **mọi** hồ sơ của hộp cùng một người |
| `Holders` | Tối đa 5 người giữ nhiều nhất + `HolderCount` | Như bên trái, theo người giữ QA |

## Xem trước và lưu

Nút gửi lên `{ box, profileRange? }`, `profileRange` null = cả hộp. Kiểm theo thứ tự: role `Data` / `QA` (`400`) → người giao là Admin
hoặc Leader của cặp, dự án không `DONE` / `EXPIRED` (`805` / `818`, `DEPLOYING` được) → người nhận là `ProjectMember` đúng role, đang
hoạt động (`811`) → Leader chỉ giao người mình quản lý (`821`) → chuẩn hoá: trim, bỏ trùng, có "cả hộp" thì bỏ hồ sơ cùng hộp,
≤ 1000 nút (`400`).

Sau đó **bung** "cả hộp" thành mọi hồ sơ hiện có của hộp; phiếu lưu từng cặp (hộp, hồ sơ) đã bung (`""` cho rỗng, không lưu null),
việc giao thật lưu **từng văn bản** (`SubmissionUser`). Hồ sơ import thêm vào hộp **sau** khi giao chưa thuộc ai, Leader giao tay.

Preview không ghi DB, không ném lỗi kế hoạch: trả số liệu + `CanSave`, `BlockReasons`, `Warnings`. Lưu thì ném lỗi đầu tiên theo
thứ tự: nút không có văn bản `813` → xung đột `812` → QA trùng người nhập `331` → QA chế độ `SAMPLE` `400` → không có gì để giao `824`
→ quá trần `825` (Data 1000) / `TicketTooLarge` Construction (QA 5000).

### Data

| Văn bản | Xếp vào | Ảnh hưởng |
|---|---|---|
| Chưa ai giữ Data | New | Được giao (gồm văn bản về pool, mọi trạng thái kiểm) |
| Chính người nhận đang giữ | AlreadyHeld | Bỏ qua |
| Người khác đang giữ | Conflict | **Chặn cả phiếu** `812` |
| New, `RETURNED`, người nhận có `SubmissionLog` trên nó | ReturnedToSame | Chỉ **cảnh báo** |

Lưu: `AssignmentTicket.CreateManual(Data)` → các nút → `SubmissionUser.CreateEntry` từng văn bản New (bulk) → audit `Giao phiếu`.

### QA — phụ trách nút + đợt

- **Phụ trách nút** = `AssignmentTicket` Role `QA`, `ACTIVE` + item là các nút. Mỗi QA **một** phiếu phụ trách / cặp: giao thêm thì
  dồn nút mới vào phiếu cũ, cộng `DocumentCount`. Nút đã có QA khác phụ trách → `812`; cùng QA phụ trách lại không là xung đột.
- **Đợt QA** = `QaTicket` `FULL`. Văn bản New vào **đợt đang mở** của QA đó cùng cặp: `ACTIVE`, chưa văn bản nào có
  `SubmissionQaLog` sau lúc vào đợt, còn chỗ ≤ 5000; không có thì tạo đợt mới. QA chấm văn bản đầu tiên → đợt đóng nhận (vẫn chấm,
  vẫn tự chốt Đạt như 18C). Vì sao: chấm mẫu (18C pha 4–6) cần tập cố định, còn QA vẫn nhận văn bản liên tục.

| Văn bản thuộc nút đã chọn | Xếp vào | Ảnh hưởng |
|---|---|---|
| QA khác đang giữ, chưa QA Đạt (kể cả giữ từ API QA cũ, kể cả `RETURNED`) | Conflict | **Chặn** `812` |
| Chính người nhận đang giữ | AlreadyHeld | Bỏ qua |
| `CHECKED`, `QA_NOT_REVIEWED`, chưa ai giữ QA | New | Vào đợt ngay khi lưu |
| `CHECKED`, đã QA (luồng cũ, không ai giữ) | — | Coi như xong: không giao, không chặn |
| Chưa `CHECKED` (`NOT_CHECKED`, `RETURNED`) | Later | Kiểm xong tự về QA phụ trách (Task 15), trừ 6 trường hợp dừng ở mục C bên dưới |
| Người nhận từng có `SubmissionLog` trên văn bản, hoặc đang giữ Data của nó | SameAsEntry | **Chặn** `331` |

- Lưu được khi New = 0 nhưng có Later ở nút mới phụ trách; New = 0 và (Later = 0 hoặc không có nút mới) → `824`.
- Audit `Giao phiếu`: "Giao QA {tên} phụ trách {k} hộp / hồ sơ, {n} văn bản đã kiểm, phiếu #{id}".
- Đổi QA phụ trách một nút: API `qa/revoke`, `qa/transfer` (Task 14, [26](26-thu-hoi-chuyen-phieu.md)). Gỡ QA khỏi dự án vẫn huỷ đợt
  mở và phiếu phụ trách (`CancelledQaOwnershipTicketIds` trong audit gỡ).

API giao QA cũ `POST construction/qa-assignment/ticket` (không biết phụ trách nút) đã xoá 2026-11-12 (Task 26); chỉ còn API 23
bên dưới. Văn bản do API cũ giao từ trước vẫn bị tính là Conflict ở đây.

## Chống đua

Lưu mở transaction rồi khoá dòng `construction.ProjectDocumentType` của cặp (`UPDLOCK, HOLDLOCK`, chờ tối đa 5 giây, hết giờ → `812`),
**sau đó** mới lập kế hoạch. Request đến sau chờ, đọc lại thấy xung đột → `812`. Cần khoá vì phụ trách nút chưa có văn bản `CHECKED`
không sinh dòng `SubmissionUser` nào để unique `(SubmissionId, Role)` chặn. Unique + bulk insert (`812`) vẫn là chốt chặn cuối.
⚠️ Cùng khoá với import hồ sơ: đang import lớn thì giao phiếu chờ, quá 5 giây nhận `812` — thử lại sau, đừng nới timeout.

## Phiếu của tôi (API 28)

Phiếu Data `ACTIVE` của mình + đợt QA `ACTIVE` / `REJECTED` của mình; `includeFinished=true` thêm Data `DONE`, QA `PASSED`. Phiếu
huỷ luôn ẩn, phiếu của dự án đã xoá không hiện, phiếu phụ trách QA không hiện (QA làm theo đợt). Sắp mới nhất trước.
`Kind` = `Data` | `QA`; `Status`: Data là mã `AssignmentConst` dạng chuỗi (`"1"` đang làm, `"3"` xong), QA là trạng thái đợt.

## API — `api/v1/operation/project/{projectId}/document-type/{documentTypeId}/...`

| # | Method | Route | Quyền | Việc |
|---|---|---|---|---|
| 20 | GET | `assignment-tree?role=` | `Assignment.Manage` | Cây cấp Hộp |
| 21 | GET | `assignment-tree/profiles?role=&box=` | `Assignment.Manage` | Cấp Hồ sơ của 1 hộp (`box` trống = hộp rỗng) |
| 22 | POST | `assignment/preview` | `Assignment.Manage` | Xem trước, không ghi |
| 23 | POST | `assignment` | `Assignment.Manage` | Lưu Data hoặc QA |
| 24 | GET | `assignment/{ticketId}` | `Assignment.Manage` | Chi tiết phiếu Data / phụ trách QA |
| 29–33 | POST / GET | `assignment/revoke`, `/transfer`, `/qa/revoke`, `/qa/transfer`, `assignment/ticket/paging` | `Assignment.Manage` | Thu hồi / chuyển, danh sách phiếu ([26](26-thu-hoi-chuyen-phieu.md)) |
| 28 | GET | `api/v1/operation/my-assignment` | `Assignment.ViewOwn` | Phiếu của tôi |

Phiếu QA trong API 24 đếm văn bản QA đó đang giữ thuộc các nút phụ trách. Mã mới: `824` không có văn bản mới để giao, `825` phiếu
nhập liệu quá 1000 văn bản.

## Case đã chốt

| # | Case | Chốt |
|---|---|---|
| B2 | Văn bản về pool từ D1, giao lại D1 | Cho, chỉ cảnh báo |
| B3 | Nút / văn bản đã có QA khác | Chặn cả lần giao |
| B4 | QA trùng người nhập | Chặn mọi người từng kiểm (`SubmissionLog`) + người đang giữ Data |
| B5–B8 | Trần phiếu Data / sắp xếp / phân trang / `DEPLOYING` | 1000 / tự nhiên / 200 / cho giao |
| B9 | Phiếu của tôi | Mặc định đang làm, ẩn huỷ |
| 2026-11-11 | Hộp / hồ sơ rỗng; "cả hộp" | Hiện và giao được; bung theo hồ sơ rồi văn bản, không lưu null |
| 2026-11-11 | QA phụ trách lại / nút mới | Một phiếu phụ trách / QA / cặp, dồn vào phiếu cũ |
| 2026-11-11 | QA Đạt luồng cũ, không ai giữ | Coi như xong |
| 2026-11-11 | Hồ sơ phụ trách cả hộp | Hộp là cha: phụ trách mọi hồ sơ trong hộp |

## Nối vào luồng kiểm tra / QA

Task 15: thân `SubmissionAssignmentService`, luật ở `AssignmentDomainService`. **Người xem tất cả** của cặp: Admin / SuperAdmin, hoặc có
`Assignment.ViewAllSubmission` **và** là Leader đang hoạt động của đúng cặp.

| Mục | Luật |
|---|---|
| A. Xem | Người xem tất cả thấy mọi văn bản. Còn lại: không là thành viên cặp (đã gỡ / chưa từng) → `820`; là thành viên → chỉ văn bản có dòng `SubmissionUser` (Data hoặc QA) của mình, mở văn bản khác → `820` |
| B. Kiểm | Người xem tất cả: được, kể cả văn bản đã QA đạt (sửa hộ). Không giữ Data → `820`. Giữ Data, văn bản QA đạt → `826`. Thành viên bị tắt vẫn kiểm văn bản đang giữ |
| C. Kiểm xong | Dừng (không lỗi) khi: đã QA; đã có QA giữ; nút (hộp, hồ sơ) không có phụ trách; QA phụ trách không còn hoạt động; QA từng kiểm / đang giữ Data văn bản; QA ở `SAMPLE`. Qua hết → khoá cặp, đọc lại, thêm dòng QA vào đợt mở (chưa có thì tạo), phiếu phụ trách +1. Áp cả văn bản chưa từng giao Data |
| D. QA Không đạt | Khoá cặp rồi xét: phiếu Data không còn `ACTIVE` → về pool ngay, phiếu giữ nguyên; người nhập đã bị gỡ → về pool ngay, phiếu −1; còn lại tăng `ReturnCount`, lần 3 → về pool, phiếu −1. Về pool = xoá dòng Data, giữ dòng QA, audit `Trả văn bản về pool` |
| D. Sau chấm | Phiếu Data còn `ACTIVE` và mọi văn bản đang thuộc phiếu đã QA đạt → `DONE` (UPDATE có điều kiện `Status = ACTIVE`) |
| E. Sửa hộ | Admin / Leader kiểm văn bản đã đạt → QA cũ chấm lại; Không đạt khi phiếu đã `DONE` → về pool ngay |

| # | Case | Chốt |
|---|---|---|
| 15.1 | Đếm 3 lần trả | Theo từng người giữ; đổi người = đếm từ 0 |
| 15.2 / 15.8 | Về pool | Giữ QA cũ; chỉ người xem tất cả xem / sửa được tới khi giao lại |
| 15.3 | Admin / Leader kiểm hộ | Được; văn bản vẫn của nhân viên, `SubmissionLog` ghi người kiểm thật |
| 15.6 / 15.9 | QA phụ trách tắt / gỡ; kiểm xong | Không tự giao; còn lại luôn vào đợt QA mở hoặc đợt mới |
| 15.10 | Xuất Excel | Không lọc theo người giữ (Task 25, `337`) |
| 2026-11-12 | Hai QA cùng chấm văn bản của một phiếu Data | Khoá cặp ở D trước khi nạp phiếu, không mất lượt trừ `DocumentCount` |

⚠️ Đạt chạy song song với một văn bản khác của cùng phiếu về pool có thể đều thấy còn văn bản chưa đạt và không chốt `DONE` (cùng
giới hạn như chốt đợt QA FULL 18C). Đừng thêm khoá ở nhánh Đạt: dòng `Submission` đã cập nhật trước khi khoá, dễ deadlock.

## Số đo (DB dev local Docker, 20.000 văn bản / 400 hộp, 2026-11-11)

| Việc | Đo được | Ngưỡng task |
|---|---|---|
| GET cây Data / QA trang 50 | 1,2 s / 1,6 s (câu gộp SQL 320–560 ms) | 500 ms |
| Lưu Data 1 hộp 50 / 1000 văn bản | 3,0 s / 4,2 s | 300 ms / 1 s |

⚠️ DB local này trễ ~130 ms **mỗi** lượt gọi DB (API có sẵn `auth/role/dropdown`, 2 câu SQL, mất 260–330 ms), nên số trên chủ yếu là
độ trễ môi trường; cần đo lại trên DB thật trước khi kết luận chậm.

> **Tiếp:** [24-don-gia-tien-cong.md](24-don-gia-tien-cong.md) — đơn giá và thống kê tiền công (Task 12). Kiến trúc và quy ước code ở [../architecture/README.md](../architecture/README.md).
