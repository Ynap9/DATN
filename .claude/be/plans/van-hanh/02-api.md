# Operation — API và quyền

> **Phần 2/2** · trước: [01-nghiep-vu.md](01-nghiep-vu.md) · mục lục: [README.md](README.md)

Tiền tố `api/v1/operation`. `{p}` = `projectId`, `{dt}` = `documentTypeId`. Envelope `ApiResponse` như các module khác.

## Nhóm nhân viên (O2)

| # | Method | Route | Ai | Việc |
|---|---|---|---|---|
| 1 | GET | `/staff-group` | Admin, Leader | Phân trang, tìm kiếm; Leader chỉ thấy nhóm mình |
| 2 | GET | `/staff-group/{id}` | Admin, Leader | Chi tiết + thành viên |
| 3 | POST | `/staff-group` | Admin, Leader | Tạo nhóm, kèm `userIds` tuỳ chọn |
| 4 | PUT | `/staff-group/{id}` | Admin, chủ nhóm | Sửa tên, mô tả |
| 5 | DELETE | `/staff-group/{id}` | Admin, chủ nhóm | Xoá mềm |
| 6 | POST | `/staff-group/{id}/members` | Admin, chủ nhóm | Thêm `userIds` |
| 7 | DELETE | `/staff-group/{id}/members` | Admin, chủ nhóm | Bỏ `userIds` |
| 8 | GET | `/staff-group/candidates` | Admin, Leader | User role Data / QA để chọn |

## Thành viên dự án (O3)

| # | Method | Route | Ai | Việc |
|---|---|---|---|---|
| 9 | GET | `/project/{p}/member` | Admin, Leader | Danh sách + giá hiện hành; lọc `documentTypeId`, `role`, từ khoá |
| 10 | GET | `/project/{p}/member/candidates` | Admin, Leader | User chưa có trong (dự án, loại văn bản), lọc role |
| 11 | POST | `/project/{p}/leader` | Admin | `documentTypeId`, `users[{userId, companyPrice?, actualPrice?}]` |
| 12 | POST | `/project/{p}/member` | Admin, Leader | `documentTypeId`, `users[{userId, role, prices?}]`, `groups[{groupId, role}]`, `managedByUserId`; trả `added`, `skipped[{userId, reason}]` |
| 13 | PUT | `/project/{p}/member/{memberId}` | Admin, Leader | Đổi role, Leader quản lý, bật / tắt |
| 14 | DELETE | `/project/{p}/member/{memberId}` | Admin, Leader | Gỡ; chặn khi còn phiếu `ACTIVE` |

## Đơn giá và quỹ KPI (O4, O7)

| # | Method | Route | Ai | Việc |
|---|---|---|---|---|
| 15 | GET | `/project/{p}/price-default` | Admin, Leader | Giá mặc định theo loại văn bản × vai trò |
| 16 | PUT | `/project/{p}/price-default` | Admin | Đặt giá mặc định, có `effectiveFrom` |
| 17 | PUT | `/project/{p}/member/price` | Admin, Leader | `items[{memberId, companyPrice?, actualPrice, effectiveFrom}]`; Leader gửi `companyPrice` → lỗi |
| 18 | GET | `/project/{p}/member/{memberId}/price-history` | Admin, Leader | Lịch sử giá |
| 19 | GET | `/project/{p}/bonus-pool?from=&to=` | Admin | Theo vai trò và người: khối lượng, tiền giá công ty, tiền thực nhận, chênh lệch |

## Chia phiếu (O5, O6) — tiền tố `/project/{p}/document-type/{dt}`

| # | Method | Route | Ai | Việc |
|---|---|---|---|---|
| 20 | GET | `/assignment-tree?role=DATA` | Admin, Leader | Cấp Hộp: số liệu + người giữ; lọc "chưa giao" |
| 21 | GET | `/assignment-tree/profiles?box=` | Admin, Leader | Các hồ sơ trong một hộp (tải khi mở) |
| 22 | POST | `/assignment/preview` | Admin, Leader | `{assigneeUserId, role, items[{box, profileRange?}]}` → số văn bản, nút trùng; không ghi DB |
| 23 | POST | `/assignment` | Admin, Leader | Lưu phiếu, ghi `EntryUserId` / `QaUserId` |
| 24 | POST | `/assignment/revoke` | Admin, Leader | Theo `ticketId` hoặc nút; chỉ lấy lại văn bản chưa nhập |
| 25 | POST | `/assignment/transfer` | Admin, Leader | Phần chưa xong từ người A sang người B |
| 26 | GET | `/assignment/ticket` | Admin, Leader | Lọc người, vai trò, trạng thái |
| 27 | GET | `/assignment/summary` | Admin, Leader | Theo người: đã giao, đã nhập, QA Đạt, phiếu đạt, tiền ước tính |
| 28 | GET | `/api/v1/operation/my-assignment` | Data, QA | Phiếu và cây của chính mình — màn nhập liệu TV1 dùng |

## Quyền mới (`PermissionConstants`)

| Key | Gán cho |
|---|---|
| `Menu.Operation.StaffGroup`, `Function.Operation.StaffGroup.Manage` | Admin, Leader |
| `Function.Operation.ProjectMember.Manage` | Admin, Leader |
| `Function.Operation.Price.ManageCompany`; menu `Menu.Operation`, `.ProjectMember`, `.Assignment` (tick menu = cấp mọi con) | Admin |
| `Function.Operation.Price.ManageActual` | Admin, Leader |
| `Function.Operation.Assignment.Manage`, `.ViewAllSubmission` | Admin, Leader |
| `Function.Operation.Assignment.ViewOwn` | Data (QA xem phiếu QA ở `qa-assignment` của 18C) |
| `Function.Operation.BonusPool.View` | Admin |

Bảng gán đầy đủ ở [../../nghiep-vu/03-vai-tro-mac-dinh.md](../../nghiep-vu/03-vai-tro-mac-dinh.md).

Leader chỉ thao tác trên (dự án, loại văn bản) mà mình là `ProjectMember` vai trò `LEADER` — kiểm ở service, không chỉ dựa vào quyền.
