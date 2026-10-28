# Nhóm nhân viên (Operation)

> **Phần 19/21** · trước: [18-qa-cham-mau.md](18-qa-cham-mau.md) · mục lục: [README.md](README.md)

Leader hay thêm cả tổ 5–20 người vào dự án mới. Nhóm (`StaffGroup` + `StaffGroupMember`) là công cụ chọn nhanh cho Task 11 ([21](21-thanh-vien-du-an.md));
nhóm **không** dùng để tính tiền hay giao phiếu. Task 10, 2026-10-26. Plan: [../plans/van-hanh/README.md](../plans/van-hanh/README.md).

## Ai làm được gì

| Người gọi | Xem | Tạo | Sửa / xoá / thêm, bỏ thành viên |
|---|---|---|---|
| `SuperAdmin`, `Admin` | Mọi nhóm | Có | Mọi nhóm |
| `Leader` | Chỉ nhóm mình tạo | Có | Chỉ nhóm mình tạo |

- "Admin" = `OperationAccessPolicy.IsAdmin(roleNames)` (role `SuperAdmin` hoặc `Admin`). Chủ nhóm = người tạo, kể cả khi Admin tạo.
- Leader đụng nhóm người khác (xem, sửa, xoá, thêm / bỏ người) → `801` "không tìm thấy", **không** báo `803`, để không dò được
  nhóm nào tồn tại. `803` để dành cho API đổi chủ nhóm (chưa làm).
- Leader gửi `ownerUserId` khi phân trang thì bị ép về chính mình; Admin lọc theo `ownerUserId` được.

## Vai trò của nhóm

- Mỗi nhân viên có đúng một trong hai role hệ thống `Data` hoặc `QA` (luật `109` ở [01-nguoi-dung.md](01-nguoi-dung.md)).
- Mỗi nhóm là nhóm `Data` **hoặc** nhóm `QA`. **Không lưu cột vai trò**: vai trò nhóm suy từ role hệ thống của thành viên;
  nhóm rỗng thì `role = null`.
- Tạo nhóm / thêm thành viên: (người mới + người đang có) phải có đúng một role trong `{Data, QA}`, sai thì `804` và **cả
  request không ghi gì**. Message nêu người đầu tiên sai: "Người dùng X không cùng vai trò Data với nhóm".
- User không tồn tại hoặc không có role `Data` / `QA` (Leader, Admin…) → cũng `804`, không tách mã để không dò được user.
- **Nhóm lệch**: Admin đổi role một thành viên sau khi vào nhóm (vd D3 `Data` → `QA`) thì nhóm **không** tự xoá người đó.
  Chi tiết nhóm trả `role = "Data, QA"` và `roleNames` của từng người để FE cảnh báo. Thêm người vào nhóm lệch → `804` cho tới khi
  chủ nhóm bỏ người lệch. **Bỏ thành viên không kiểm vai trò** — để luôn gỡ được người lệch.

## Luật khác

- Tên trim, 1–200 ký tự, không trùng trong các nhóm **cùng chủ** chưa xoá → `802`; khác chủ được trùng. Mô tả ≤ 1000, rỗng → `null`.
- Thêm người đã có, bỏ người không có → bỏ qua im lặng, không ghi audit. Mỗi request ≤ 500 `userIds` (`StaffGroupConst.MEMBERS_PER_REQUEST_MAX`).
- Xoá nhóm = xoá mềm `StaffGroup` + xoá cứng các dòng `StaffGroupMember`, cùng một `SaveChanges`. Thành viên dự án đã thêm từ nhóm
  (`ProjectMember.SourceStaffGroupId`) không bị ảnh hưởng. Xoá xong tạo lại cùng tên được (unique index lọc `[Deleted] = 0`).
- Một user thuộc nhiều nhóm được. Mọi thao tác ghi có audit `ACTION_STAFF_GROUP_*`, content ghi tên nhóm + số người.

## API — `api/v1/operation/staff-group`, quyền `Function.Operation.StaffGroup.Manage`

| # | Method | Route | Việc | Trả về |
|---|---|---|---|---|
| 1 | GET | `paging` | `keyword` (tên / mô tả), `ownerUserId`, `role` (`Data` / `QA`), `pageSize` 1–200 | `ViewStaffGroupDto`: + `ownerFullName`, `role`, `memberCount` |
| 2 | GET | `{id:int}` | Chi tiết | `FindStaffGroupByIdDto` + `members[]` (`userName`, `fullName`, `email`, `roleNames`, `addedDate`), sắp `fullName` |
| 3 | POST | (gốc) | `name`, `description`, `userIds` (tuỳ chọn) | Chi tiết nhóm |
| 4 | PUT | `{id:int}` | Sửa tên, mô tả | – |
| 5 | DELETE | `{id:int}` | Xoá mềm | – |
| 6 | POST | `{id:int}/members` | Thêm `userIds` | Chi tiết nhóm |
| 7 | DELETE | `{id:int}/members` | Bỏ `userIds` (body) | Chi tiết nhóm |
| 8 | GET | `candidates` | `role` **bắt buộc** `Data` / `QA`, `keyword`, `excludeStaffGroupId` | User có đúng role đó, sắp `fullName` |

Luồng FE: dropdown "Vai trò" chỉ để lọc (BE không nhận) → gọi API 8 theo role → lưu bằng API 3. Màn sửa: khoá dropdown theo `role`
của API 2 (nhóm rỗng thì mở), gọi `candidates?role=…&excludeStaffGroupId=<id>`.

Lỗi định dạng DTO (thiếu `name`, tên > 200, > 500 `userIds`, `userIds` rỗng ở API 6 / 7, thiếu `role` ở API 8) trả **HTTP 400** của
`[ApiController]`, không phải `ApiResponse`. `pageSize` sai, `role` khác `Data` / `QA` trả `ApiResponse` mã `400`.

## Code

| Tầng | File |
|---|---|
| Domain | `StaffGroups/Repositories/IStaffGroupRepository`, `StaffGroups/Services/StaffGroupDomainService` (`EnsureAccessibleAsync`, `EnsureUniqueNameAsync`, `EnsureSameStaffRoleAsync`), `ProjectMembers/Services/OperationAccessPolicy` (Task 11 dùng lại) |
| Applications | `Dtos/StaffGroup/*`, `IStaffGroupQuery`, `IStaffGroupService` / `StaffGroupService` |
| Data | `Module/Operation/Repositories/StaffGroupRepository` (`GetStaffRoleNamesAsync` đọc thẳng `UserRoles` / `Roles`), `Queries/StaffGroupQuery` |
| Api | `Controllers/Module/Operation/StaffGroupController` |

`role` của nhóm tính trong bộ nhớ sau khi tải trang (một câu lấy cặp (nhóm, role) cho cả trang), không dùng `string.Join` trong SQL.

## Đã kiểm thực tế (2026-10-26)

Gọi thật trên DB dev với Admin, 2 Leader, 4 Data, 2 QA tạo bằng SQL (chép password hash của `admin` để khỏi gửi mail thật qua
SMTP), xoá sạch sau khi test: **50/50 đạt** — đủ các ca ở mục 10.8 của task (109 khi tạo / gán Data + QA, Data + Leader được;
802; 804 cho nhóm trộn, user Leader, user không có, nhóm lệch; idempotent; `candidates` + `excludeStaffGroupId`; Leader A ↔ B → 801;
400 cho 501 người / tên rỗng / 201 ký tự; xoá rồi tạo lại cùng tên; đủ 5 audit action; Data → 403; không token → 401).
DB dev trước khi merge không có user nào giữ cả `Data` và `QA`.

⚠️ Bẫy khi viết script kiểm thử PowerShell: biến không phân biệt hoa thường — `$LA` (userId) đè `$la` (token) làm mọi lời gọi thành 401.

> **Tiếp:** [20-ai-nha-cung-cap.md](20-ai-nha-cung-cap.md) — module Ai: nhà cung cấp AI và bảo mật API key.
