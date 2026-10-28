# Thành viên dự án (Operation)

> **Phần 21/21** · trước: [20-ai-nha-cung-cap.md](20-ai-nha-cung-cap.md) · mục lục: [README.md](README.md)

Xác định **ai** làm việc trong một cặp (dự án, loại văn bản) và **ai quản lý** họ (`ProjectMember`). Đầu vào của giao phiếu nhập liệu
(Task 13), giao phiếu QA ([18C](18-qa-cham-mau.md#18c--phiếu-qa)) và phạm vi Leader ở mọi màn Operation. Task 11, 2026-10-27.
Phần tính tiền (đơn giá lúc thêm người, `ProjectMemberPrice`) **tạm lùi** chờ chốt công thức lương: task này không ghi, không trả giá.

## Ai quản lý ai

- Admin / SuperAdmin (`OperationAccessPolicy.IsAdmin`) quản lý **mọi** thành viên của mọi cặp.
- Leader = `ProjectMember` Role `Leader`, `IsActive`, chưa gỡ, của đúng cặp. Leader quản lý người có `ManagedByUserId` = mình.
- `ManagedByUserId = null` → **Admin quản lý trực tiếp**; Leader không thấy, không thao tác. Không bắt buộc chọn Leader khi thêm.
- Một cặp có nhiều Leader; mỗi nhân viên tối đa 1 Leader. Một người 1 vai trò / cặp (unique lọc `[Deleted] = 0`), khác cặp thì khác vai trò được.
- Vai trò dự án = đúng tên role hệ thống (`RoleConstants`: `Leader` / `Data` / `QA`), một user không vừa `Data` vừa `QA` (`109`).

| Thao tác | Admin / SuperAdmin | Leader của cặp |
|---|---|---|
| Xem danh sách | Mọi thành viên | Chính mình + người mình quản lý |
| Thêm Leader | Có | `805` |
| Thêm Data / QA từng người | Có, `managedByUserId` tuỳ chọn (phải là Leader đang hoạt động của cặp, sai `823`) | Có, `managedByUserId` **ép** = mình |
| Thêm theo nhóm | Mọi nhóm | Chỉ nhóm mình là chủ (khác → `801`) |
| Đổi / bỏ Leader quản lý | Có | `821` |
| Bật / tắt, gỡ | Mọi người | Chỉ người mình quản lý; Leader khác, người Admin quản lý → `821` |

Người gọi không phải Admin, cũng không là Leader đang hoạt động của cặp (kể cả Leader đã tắt / đã gỡ) → `805`.

## Phạm vi

`OperationAccessDomainService.EnsureCanManageAsync` (Task 13–15 dùng lại), theo thứ tự: dự án không có `303` → loại văn bản không có
`301` → chưa gắn vào dự án `312` (mã Construction) → dự án `DONE` / `EXPIRED` khi thêm / sửa / gỡ → `818`, **cả SuperAdmin** (xem vẫn
được; `DEPLOYING` / `DOING` / `ACCEPTING` được) → Admin hay Leader (`805`). Trả `OperationAccess(Project, DocumentType, IsAdmin,
LeaderUserId)` để service khỏi đọc lại tên dự án / loại văn bản cho audit. `memberId` không có, đã gỡ, khác `projectId` trên route → `806`.

## Thêm hàng loạt (API 11, 12)

- Bung: `users[]` (role người gọi chọn, chỉ `Data` / `QA`) + mọi thành viên **hiện tại** của `staffGroupIds`. Người từ nhóm nhận role hệ
  thống hiện tại của chính họ (`Data` hoặc `QA`) — nhóm lệch vẫn áp được, mỗi người theo role mình.
- Trùng: dòng `users[]` thắng dòng nhóm (`SourceStaffGroupId = null`); trùng trong `users[]` lấy dòng đầu; trùng giữa 2 nhóm lấy nhóm trước.
- **Bỏ qua** (trả `skipped[{userId, fullName, reason}]`, không lỗi), xét theo thứ tự: không tồn tại `SKIP_REASON_USER_NOT_FOUND` → đã là
  thành viên cặp (vai trò nào cũng vậy) `ALREADY_MEMBER` → không có role hệ thống đúng vai trò (gồm người từ nhóm nay không còn Data / QA)
  `INVALID_ROLE`.
- **Hỏng cả request**: quyền `805` / `801`, phạm vi, `823`, `users` + `staffGroupIds` cùng rỗng / > 500 người **sau khi bung** / role
  khác `Data` / `QA` → `400`. Hai request thêm trùng người cùng lúc: unique chặn, `ProjectMemberRepository.AddRangeAsync` bắt SQL
  `2601` / `2627` → `400` "Có người vừa được thêm bởi thao tác khác, hãy tải lại", rollback.
- Tất cả bị bỏ qua → `addedCount = 0`, **không** audit. Nhóm đổi / xoá sau đó không kéo theo dự án.

## Sửa (API 13)

- Đổi Leader quản lý chỉ Admin: `managedByUserId` phải là Leader đang hoạt động của cặp (`823`); `clearManager = true` → `null`.
  Đổi manager của thành viên Leader → `400` "Leader không có Leader quản lý".
- Bật / tắt: Admin hoặc Leader quản lý người đó. Không đổi gì (bật người đang bật, body rỗng) → trả bản hiện tại, không audit.
- **Không** đổi vai trò (gỡ rồi thêm lại).
- Tắt nhân viên: không nhận phiếu **mới**, phiếu đang giữ vẫn làm. Tắt Leader: mất quyền quản lý ngay; nhân viên giữ `ManagedByUserId`,
  trong lúc đó Admin quản lý họ; bật lại là như cũ. Tắt **không** thu hồi văn bản.

## Gỡ / thu hồi

Gỡ (API 14) = xoá mềm + **tự thu hồi** văn bản chưa làm của người đó **trong cặp**, một transaction. Không còn chặn `807`.

| Vai trò | Phiếu bị xét | Thu hồi (về "chưa giao") | Giữ tên người cũ |
|---|---|---|---|
| `Data` | `AssignmentTicket` `ACTIVE` | `StatusCheck` `NOT_CHECKED` / `RETURNED` | `CHECKED` (đã nhập xong) |
| `QA` | `QaTicket` `ACTIVE` / `REJECTED` | `StatusQa ≠ QA_PASSED` | Đã QA Đạt |
| `Leader` | — | — | Người mình quản lý trong cặp → `ManagedByUserId = null` |

- Data: xoá dòng `SubmissionUser` Role `Data`; dữ liệu đã nhập và `StatusCheck = RETURNED` giữ nguyên. Mỗi phiếu
  `DecreaseDocuments(số dòng xoá thật)` → về 0 thì `CANCELLED`, còn văn bản `CHECKED` thì phiếu vẫn `ACTIVE`.
- QA: làm **đúng** "Huỷ phiếu" 18C — `Cancel` → `UPDATE` có điều kiện (0 dòng → `400` "Phiếu QA vừa thay đổi, hãy tải lại") → xoá dòng QA
  của văn bản chưa Đạt; `SubmissionQaLog`, `StatusQa` giữ nguyên. QA đã gỡ chấm phiếu cũ → `334`.
- Chống đua: lệnh xoá có điều kiện trạng thái văn bản **ngay trong câu SQL** (`ExecuteDelete` + `Any` trên `Submissions`), không đọc trước
  rồi xoá sau — văn bản người đó vừa bấm Kiểm tra / vừa chấm Đạt ở lại người đó.
- Trả `revokedDocumentCount`, `keptDocumentCount` (dòng còn lại của **các phiếu vừa xét**), `cancelledAssignmentTicketIds`,
  `cancelledQaTicketIds`, `unmanagedMemberCount`. FE hỏi xác nhận trước dựa vào `heldTicketCount` của API 9.
- Gỡ rồi thêm lại cùng người được; văn bản đã thu hồi **không** tự quay lại.

⚠️ Phần QA lặp lại luật huỷ phiếu của Construction (Operation không được gọi `QaAssignmentDomainService`). Đổi luật huỷ ở
[18C](18-qa-cham-mau.md#18c--phiếu-qa) thì sửa cả `ProjectMemberDomainService.RevokeHeldDocumentsAsync`.

## API — `api/v1/operation/project/{projectId:int}`, quyền `Function.Operation.ProjectMember.Manage`

| # | Method | Route | Việc | Trả về |
|---|---|---|---|---|
| 9 | GET | `member/paging` | `documentTypeId` bắt buộc, `role`, `keyword` (userName / fullName), `managedByUserId` (chỉ Admin), `isActive`, `pageSize` 1–200 | `ViewProjectMemberDto` + `managedByFullName` (`null` = Admin quản lý), `sourceStaffGroupName` (cả nhóm đã xoá), `heldTicketCount`; sắp Leader → Data → QA, rồi `fullName` |
| 10 | GET | `member/candidates` | `documentTypeId`, `role` bắt buộc, `keyword`, `staffGroupId` (`801` nếu không thấy nhóm); Leader hỏi `role=Leader` → `805` | User có role đó, chưa là thành viên cặp |
| 11 | POST | `leader` | `{ documentTypeId, userIds[1..500] }`, chỉ Admin | `AddProjectMemberResultDto` (`addedCount`, `added[]`, `skipped[]`) |
| 12 | POST | `member` | `{ documentTypeId, managedByUserId?, users[{userId, role}], staffGroupIds[] }` | Như API 11 |
| 13 | PUT | `member/{memberId:int}` | `{ managedByUserId?, clearManager, isActive? }` | `ViewProjectMemberDto` |
| 14 | DELETE | `member/{memberId:int}` | Gỡ + thu hồi | `RemoveProjectMemberResultDto` |

`heldTicketCount`: Data = phiếu nhập liệu `ACTIVE`, QA = phiếu QA `ACTIVE` / `REJECTED`, Leader = 0. Audit `ACTION_PROJECT_MEMBER_ADD`
(một dòng cho cả lượt, danh sách `{UserId, Role, ManagedByUserId, SourceStaffGroupId}`), `_UPDATE` (cũ / mới `{Id, UserId, ManagedByUserId,
IsActive}`), `_REMOVE` (một dòng cho cả lần gỡ, mới = số thu hồi + Id phiếu + Id người bị bỏ Leader).

## Code

| Tầng | File |
|---|---|
| Domain | `ProjectMembers/Repositories/IProjectMemberRepository`, `Models/OperationAccess`, `Models/RevokeOnRemoveResult`, `Services/OperationAccessDomainService` (`EnsureCanManageAsync`, `EnsureCanTouchMember`), `Services/ProjectMemberDomainService` (`EnsureManagerAsync`, `ResolveSkipReason`, `ResolveGroupMemberRole`, `RevokeHeldDocumentsAsync`) |
| Applications | `Dtos/ProjectMember/*`, `IProjectMemberQuery`, `IProjectMemberService` / `ProjectMemberService` |
| Data | `Module/Operation/Repositories/ProjectMemberRepository` (đọc thẳng bảng Construction cùng `ShDbContext`), `Queries/ProjectMemberQuery` |
| Api | `Controllers/Module/Operation/ProjectMemberController` |

Lệch nhỏ so với file task: `OperationAccess` mang thêm `Project`, `DocumentType` (cần tên cho audit); `ExistsDocumentTypeAsync` thành
`FindDocumentTypeAsync`; `RevokeOnRemoveResult` có thêm `KeptDocumentCount` và `CountKeptDocumentsAsync(role, ticketIds)` đếm theo phiếu
vừa xét (chữ ký cũ theo người không phân biệt được phiếu `DONE`).

## Đã kiểm thực tế (2026-10-27)

Gọi thật trên DB dev: dự án X (`DOING`) có loại A, B, dự án `DONE`, dự án `DEPLOYING`, Admin, SuperAdmin, 2 Leader, 4 Data, 2 QA (chép
password hash của `admin`), phiếu nhập liệu + dòng `SubmissionUser` chèn bằng SQL (Task 13 chưa có), phiếu QA giao bằng API 18C. Tạm cấp
`AssignQa` cho Leader, `Qa` cho QA. Xoá sạch sau khi test. **48 ca: 45 đạt ngay, 3 ca báo sai do script** (2 lần so chuỗi sai thứ tự,
1 sai đường dẫn `swagger/v1-operation/swagger.json`), số liệu in ra và kiểm lại tay đều đúng — đủ mục 11.11
và 11.9: bỏ qua 3 lý do; `805` / `801` / `821` / `823` / `818` (cả SuperAdmin) / `312` / `303` / `301` / `806`; nhóm lệch; 504 người sau
khi bung → `400`; gỡ D1 thu hồi 11 giữ 2, phiếu `DONE` và cặp (X, B) không đụng; gỡ Q1 huỷ phiếu `ACTIVE` + `REJECTED`, giữ 2 Đạt và
`SubmissionQaLog`; rollback khi ném lỗi sau bước thu hồi (chèn `throw` tạm rồi gỡ); hai Admin cùng gỡ một QA → 1 thành công, 1 `400`;
gỡ Leader chỉ bỏ quản lý trong cặp; `338` / `339`; phiếu giao trước luật vẫn chấm; `403` / `401`; Swagger 6 route.

Chưa kiểm thực tế: đua "gỡ D1 ↔ D1 bấm Kiểm tra" (cần luồng nhập liệu Task 13, an toàn nhờ điều kiện trong câu xoá); dữ liệu nhập của văn
bản `RETURNED` (seed không có `SubmissionData`; câu thu hồi chỉ xoá `SubmissionUser`).

> **Tiếp:** hết phần nghiệp vụ. Kiến trúc và quy ước code ở [../architecture/README.md](../architecture/README.md).
