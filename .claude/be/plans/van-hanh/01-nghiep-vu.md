# Operation — nghiệp vụ và bảng dữ liệu

> **Phần 1/2** · mục lục: [README.md](README.md)

## Phân người vào dự án

| Ai | Được làm | Không được |
|---|---|---|
| Admin | Thêm Leader và nhân viên Data / QA vào mọi dự án; đặt cả giá công ty và giá thực nhận | – |
| Leader | Thêm nhân viên Data / QA (từng người hoặc cả nhóm) vào dự án mình là Leader; đặt giá thực nhận cho người mình quản lý | Thêm Leader khác; đặt giá công ty; đụng dự án khác |

- Một người chỉ một vai trò trong một (dự án, loại văn bản). Thêm trùng → bỏ qua, trả về trong danh sách `skipped` kèm lý do.
- Thêm theo nhóm thì bung thành từng người tại lúc thêm, lưu `SourceGroupId` để tra lại.
- Gỡ người khỏi dự án bị chặn khi còn phiếu `ACTIVE` — phải thu hồi hoặc chuyển phiếu trước.

## Nhóm nhân viên

- Admin quản lý mọi nhóm; Leader tạo và chỉ thấy nhóm của mình (`OwnerUserId`).
- Một người thuộc nhiều nhóm. Thành viên chỉ là user role `Data` / `QA`.

## Đơn giá

| Vai trò | Tính trên |
|---|---|
| Nhập liệu (`DATA`) | Mỗi trường có dữ liệu trên văn bản QA Đạt |
| QA | Mỗi văn bản đã QA |
| Leader | Mỗi **phiếu đạt** (phiếu `DONE`) của người Leader quản lý |

- **Giá công ty**: mức công ty chi cho đầu việc — chỉ Admin đặt.
- **Giá thực nhận**: mức người làm nhận. Admin đặt cho mọi người, Leader đặt cho người mình quản lý.
- Luật: `0 ≤ ActualPrice ≤ CompanyPrice`.
- Thêm người không gửi giá → lấy giá mặc định của dự án theo (loại văn bản, vai trò) có hiệu lực tại ngày thêm.
- Đổi giá = thêm dòng mới có `EffectiveFrom`; giá dùng cho một đầu việc là dòng có `EffectiveFrom` gần nhất ≤ ngày hoàn thành.

**Quỹ KPI** = Σ (giá công ty − giá thực nhận) × khối lượng trong kỳ. Dùng để thưởng người năng suất cao; cách chia
thưởng làm cùng phần thanh toán, plan này chỉ có API xem trước.

## Chia phiếu theo cây

Cây: Loại văn bản → Hộp (`Box`) → Hồ sơ (`ProfileRange`) → Văn bản. Mỗi nút: tổng, đã giao, đã nhập, QA Đạt, người đang giữ.

1. Chọn người + vai trò (`DATA` hoặc `QA`) → tích nút (tích Hộp = cả hộp) → **xem trước** → **lưu** thành 1 phiếu.
2. Mỗi văn bản chỉ một người nhập và một người QA đang giữ; người QA ≠ người nhập của văn bản đó.
3. Nút đã có người khác giữ → báo trùng ở bước xem trước, không cho lưu.
4. **Thu hồi** chỉ lấy lại văn bản chưa nhập; **chuyển phiếu** đưa phần chưa xong sang người khác (phiếu cũ
   `CANCELLED` phần đó, phiếu mới `ACTIVE`).
5. Phiếu `DONE` khi mọi văn bản trong phiếu QA Đạt — căn cứ tính giá Leader.

## Bảng dữ liệu (schema `operation`)

| Bảng | Trường chính |
|---|---|
| `StaffGroup` | Id, Name, Description, OwnerUserId, audit, Deleted |
| `StaffGroupMember` | StaffGroupId, UserId — duy nhất theo cặp |
| `ProjectMember` | Id, ProjectId, DocumentTypeId, UserId, Role (`Leader`/`Data`/`QA` của `RoleConstants`), ManagedByUserId, SourceStaffGroupId, IsActive, audit — duy nhất (ProjectId, DocumentTypeId, UserId) |
| `ProjectPriceDefault` | ProjectId, DocumentTypeId, Role, CompanyPrice, ActualPrice, EffectiveFrom |
| `ProjectMemberPrice` | ProjectMemberId, CompanyPrice, ActualPrice, EffectiveFrom, CreatedBy |
| `AssignmentTicket` | Id, ProjectId, DocumentTypeId, AssigneeUserId, Role (chỉ `Data` — phiếu QA là `QaTicket` của Construction), Mode (`1` thủ công; chừa `2`/`3`), Status (`1` ACTIVE / `2` CANCELLED / `3` DONE), DocumentCount, SourceTicketId, DoneAt, CancelledAt, audit |
| `AssignmentTicketItem` | AssignmentTicketId, Box, ProfileRange (null = cả hộp) |
| `construction.SubmissionUser` (Task 18B) | Người giữ: Role `Data` + `TicketId` = phiếu + `ReturnCount` (quá 2 → về pool); Task 9 chỉ thêm method, không đổi bảng |
| `construction.Submission` (thêm) | Chỉ index `(ProjectId, DocumentTypeId, Box, ProfileRange)`, **không** thêm cột |

Mô tả giao việc chi tiết cho người làm: `C:\Users\Admin\Downloads\Task_Operation\Task_09` … `Task_15` (mã lỗi 801–823 bỏ 814, 12 key quyền gồm gốc `Menu.Operation`).

> **Tiếp:** [02-api.md](02-api.md) — 28 API và quyền mới.
