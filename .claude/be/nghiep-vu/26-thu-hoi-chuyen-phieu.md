# Thu hồi, chuyển phiếu và danh sách phiếu (Operation)

> **Phần 26/26** · trước: [25-xoa-du-an-danh-sach-cap.md](25-xoa-du-an-danh-sach-cap.md) · mục lục: [README.md](README.md)

Task 14, 2026-11-13. Leader / Admin lấy lại hoặc chuyển việc **đang dở** của một người mà không gỡ họ khỏi dự án: phiếu nhập liệu
(Data) và phụ trách nút QA. Nối tiếp giao phiếu ở [23-chia-phieu.md](23-chia-phieu.md). Không đổi schema, không mã lỗi mới, không tổng
hợp tiền (Task 12, [24](24-don-gia-tien-cong.md)).

## Kiểm chung (4 API ghi)

Theo thứ tự: `EnsureCanManageAsync(..., requireOpenProject: true)` (`303` / `301` / `312` / `805` / `818` khi `DONE` / `EXPIRED`) → mở
transaction, `LockPairAsync` (cùng khoá với giao phiếu / import, quá 5 giây → `812`) → lập kế hoạch trong `AssignmentDomainService`
→ ghi → audit → commit. API QA kiểm `Project.EnsureQaEnabled()` (`345`) ở đầu kế hoạch. Người bị lấy việc đang **tắt** vẫn thu hồi /
chuyển được. Leader chỉ đụng người mình quản lý đúng vai trò (`821`), tính cả người đang tắt.

Chọn phạm vi **chỉ theo phiếu hoặc theo hộp / hồ sơ**, không chọn từng văn bản (chốt 2026-11-13: hộp + hồ sơ ổn định, chọn văn bản lâu).

## Nhập liệu

| | Thu hồi `assignment/revoke` | Chuyển A → B `assignment/transfer` |
|---|---|---|
| Đầu vào | `ticketId`, **hoặc** `assigneeUserId` + `items`; gửi cả hai / không gửi gì → `400` | `fromUserId`, `toUserId`, `ticketId` hoặc `items`; A = B → `400` |
| Phiếu | Không có / sai cặp / Role QA → `815`; không `ACTIVE` → `816`. Theo nút: mọi phiếu Data `ACTIVE` của người đó, không có → `816` | Như thu hồi; phiếu không phải của A → `815` |
| Văn bản lấy | **Chỉ** `NOT_CHECKED` của người đó trong phạm vi; không có → `817` | Như thu hồi; > 1000 → `825` |
| Người nhận | — | Thành viên Data đang hoạt động (`811`), Leader: do mình quản lý (`821`) |
| Lưu | Xoá dòng `SubmissionUser` Data có điều kiện → mỗi phiếu `DecreaseDocuments(số xoá thật)` (về 0 → `CANCELLED`) → còn `ACTIVE` thì `TryMarkDoneAsync` | Phiếu mới của B (`SourceTicketId` = phiếu gốc khi chuyển theo phiếu, theo nút thì `null`) + nút → xoá dòng A → số xoá ≠ số chọn → `812` rollback → dòng B (`ReturnCount` 0) → phiếu cũ như thu hồi |
| Audit | `Thu hồi phiếu`: "Thu hồi {n} văn bản nhập liệu chưa kiểm của {tên}, phiếu #{id}" | `Chuyển phiếu`: "Chuyển {n} văn bản chưa kiểm từ {A} sang {B}, phiếu #{cũ} → #{mới}" |

Văn bản đã kiểm và văn bản đang bị QA trả (`RETURNED`, chưa quá 3 lần) **ở lại người cũ**, lần trả sau vẫn về người cũ sửa. Văn bản bị
thu hồi hiện "chưa giao" trên cây nhập liệu, giao lại bằng API 23.

## Phụ trách QA

| | Thu hồi `assignment/qa/revoke` | Chuyển Q1 → Q2 `assignment/qa/transfer` |
|---|---|---|
| Đầu vào | `ticketId` phụ trách, `items?` (bỏ trống = cả phụ trách) | `ticketId`, `toUserId`, `items?` |
| Kiểm | `345` → không có / sai cặp / không phải Role QA `815` → không `ACTIVE` `816` → `821` → nút không thuộc phụ trách `400` (gửi cả hộp = mọi hồ sơ phụ trách của hộp) | Như thu hồi, rồi Q2: trùng Q1 `400` → thành viên QA đang hoạt động `811` → `821` → `SAMPLE` `400` → từng kiểm / đang giữ Data văn bản bị chuyển `331` → nút đang chuyển thuộc phụ trách khác `812` → > 5000 `329` |
| Văn bản lấy | `StatusQa ≠ QA_PASSED` của Q1 trong nút bị lấy (chưa chấm + đang bị trả). Đã đạt giữ nguyên Q1 | Như thu hồi; số xoá ≠ số chọn → `812` rollback |
| Lưu | `ReleaseQaOwnershipAsync` (dưới) | `ReleaseQaOwnershipAsync` → Q2 đã có phụ trách ở cặp thì **gộp** nút + `AddDocuments`, chưa có thì tạo mới `SourceTicketId` = phụ trách gốc → dòng QA của Q2 vào đợt mở (`FindOpenQaBatchAsync`, không có thì `CreateFull`) |
| Audit | `Thu hồi phiếu`: "Thu hồi phụ trách QA {k} nút, {n} văn bản chưa đạt của {tên}" | `Chuyển phiếu`: "Chuyển phụ trách QA {k} nút, {n} văn bản chưa đạt từ {Q1} sang {Q2}" |

`ReleaseQaOwnershipAsync` dùng chung hai API:

- Lấy hết nút → phụ trách `Cancel`. Lấy một phần → xoá `AssignmentTicketItem` các nút đó, giảm `DocumentCount` bằng **mọi** văn bản
  Q1 giữ trong các nút ấy (kể cả đã đạt) qua `UPDATE` — **không** qua `DecreaseDocuments` vì hàm đó tự `CANCELLED` khi về 0 dù phụ
  trách còn nút (K4).
- Mỗi đợt QA mất văn bản: giảm `DocumentCount` bằng `UPDATE` (entity Construction không có hàm giảm). Đợt `ACTIVE` về 0 → `Cancel`;
  còn văn bản và đều đã đạt → `Pass`; đợt `PASSED` / `REJECTED` chỉ giảm số (K3).

Sau thu hồi, văn bản chưa chấm về "chưa giao" trên cây QA; văn bản bị trả sửa xong tự về QA phụ trách **mới** của nút nếu có (Task 15),
không thì "chưa giao". Sau chuyển, văn bản kiểm xong trong nút đó tự về Q2. Kết quả `newTicket` là phiếu nhận — phiếu cũ của Q2 khi gộp.
`cancelledTicketIds` chỉ chứa Id phụ trách, không chứa Id đợt QA (khác bảng).

## Chống đua với Kiểm tra / QA

Lệnh xoá (`DeleteUnfinishedHoldersAsync`) xét lại trạng thái **trong câu SQL** và đọc `Submission` `WITH (UPDLOCK, ROWLOCK)`:
văn bản vừa kiểm / vừa đạt giữa lúc lập kế hoạch và lúc xoá thì ở lại người cũ. Thu hồi trả số xoá thật (`0` → `817`), chuyển lệch
số → `812` rollback. `AssignCheckedToQaAsync` gọi lại `EnsureCanCheckAsync` trong transaction của Kiểm tra: văn bản vừa bị thu / chuyển
→ `820`, Kiểm tra rollback.

⚠️ DB bật `READ_COMMITTED_SNAPSHOT`: `ExecuteDelete` + `Any` đọc thường thấy bản đã commit **cũ** ("chưa kiểm") của dòng Kiểm tra
đang ghi dở và xoá nhầm, để lại văn bản `CHECKED` không người giữ (test đua 2026-11-13 bắt được). Đừng bỏ `UPDLOCK`, đừng chuyển
điều kiện ra C# đọc trước rồi xoá sau.

## Danh sách phiếu — `GET assignment/ticket/paging`

Lọc `role` (`Data` / `QA`, sai → `400`), `assigneeUserId`, `status`; `pageSize` 1–200. `EnsureCanManageAsync(..., false)`: dự án đã
đóng vẫn xem. Leader chỉ thấy phiếu của người có `ManagedByUserId` = mình, đúng Role phiếu. Mỗi dòng là `ViewAssignmentTicketDto`
**không** kèm `items` (xem nút bằng API 24), số liệu đếm như API 24 bằng một câu gộp cho cả trang. Sắp `CreatedDate` giảm dần. Văn
bản của một phiếu: `GET construction/submission/paging?assignmentTicketId=` có sẵn (Task 25), không thêm API (K5).

## Dự án không QA (Task 28 cờ `IsQaEnabled`)

Chặn hẳn QA phía Operation → `345`: thêm thẳng user Role QA, preview / giao role QA, `qa/revoke`, `qa/transfer`. Thêm theo nhóm thì
người QA bị **bỏ qua** với `SKIP_REASON_QA_DISABLED` ([21](21-thanh-vien-du-an.md)). Kiểm qua `ProjectMemberDomainService.EnsureRoleEnabled`.

## Case đã chốt (2026-11-12, 2026-11-13)

| # | Case | Chốt |
|---|---|---|
| 14.2 / 14.3 | Nhập liệu lấy gì | Chỉ `NOT_CHECKED`; đã kiểm / đang bị trả ở lại người cũ, không đụng tiền |
| 14.4 | Phụ trách QA lấy gì | Văn bản chưa đạt trong nút bị lấy; đã đạt giữ nguyên |
| 14.6 | Đếm lần trả khi chuyển | Dòng mới `ReturnCount` 0 (theo từng người giữ, 15.1) |
| 14.8 / 14.9 | Người bị tắt; tắt / gỡ | Vẫn thu / chuyển được; không thêm luật, không tự thu hồi |
| 14.11 | Dự án `DONE` / `EXPIRED` | Ghi → `818`, danh sách vẫn xem |
| 2026-11-13 | Q2 đã có phụ trách | Gộp vào phiếu đó (một phụ trách / QA / cặp như Task 13) |
| 2026-11-13 | Xét `DONE` phiếu Data | Gọi `TryMarkDoneAsync` có sẵn, không viết hàm thứ hai |

Chưa kiểm thực tế: trần `329` (cần > 5000 văn bản chưa đạt). Còn hở: gỡ thành viên (`ReleaseEntryNotCheckedAsync`) xoá chưa có
`UPDLOCK`, vẫn đua được với Kiểm tra như bẫy ở trên.

| Tầng | File |
|---|---|
| Domain | `AssignmentDomainService`: `PlanRevokeAsync`, `PlanTransferAsync`, `PlanQaRevokeAsync`, `PlanQaTransferAsync`, `ReleaseQaOwnershipAsync`; record `AssignmentHolderRow`, `AssignmentChangePlan`, `QaOwnershipChangePlan` |
| Applications | `AssignmentService`: `Revoke`, `Transfer`, `RevokeQaOwnership`, `TransferQaOwnership`, `FindTicketPaging` |
| Data | `AssignmentRepository` (xoá có điều kiện, giảm số bằng `UPDATE`), `AssignmentQuery.FindTicketPaging` |

Test API thật (DB dev, tag `ZZTEST14`, dọn sạch sau test): chặn QA 15/15, thu hồi 25/25, chuyển 17/17, phụ trách QA 30/30, danh sách
16/16, gồm các vòng đua Kiểm tra ↔ thu hồi / chuyển ra đủ các kết quả hợp lệ.
