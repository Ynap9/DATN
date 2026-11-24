# Màn module Operation

> **Phần 10/12** · trước: [09-man-qa.md](09-man-qa.md) · mục lục: [README.md](README.md)

Ba màn: Thành viên dự án (Task 47), Nhóm nhân viên (Task 49), Chia phiếu (Task 51), 2026-11-23. BE ở
[../../be/nghiep-vu/21-thanh-vien-du-an.md](../../be/nghiep-vu/21-thanh-vien-du-an.md),
[24-don-gia-tien-cong.md](../../be/nghiep-vu/24-don-gia-tien-cong.md), [19-nhom-nhan-vien.md](../../be/nghiep-vu/19-nhom-nhan-vien.md)
và [23-chia-phieu.md](../../be/nghiep-vu/23-chia-phieu.md).

**Picker dùng chung** `shared/components/project-pair-picker` (`app-project-pair-picker`): tải mọi cặp một lần
(`construction/project/paging?pageSize=-1`), giữ cặp có `viewerRole` trong input `viewerRoles`, nhớ cặp trên URL
(`?projectId=&documentTypeId=`, merge với tham số lọc khác). Phát `pairsLoaded` (danh sách) và `pairChange` (cặp mới; `null` khi
mất cặp). `reload()` tải lại danh sách và **luôn** phát lại cặp đang chọn với dữ liệu mới (vd sau `818` dự án vừa đóng).

## Thành viên dự án

| Thứ | Chỗ |
|---|---|
| Route | `/admin/operation/project-member?projectId=&documentTypeId=`, `permissionGuard` [`MENU_OPERATION_PROJECT_MEMBER`, `FUNCTION_PROJECT_MEMBER_MANAGE`] |
| Màn | `features/operation/project-member/project-member-list.component.*` |
| Ô bảng | `project-member-cells.ts`: vòng chữ cái, chip vai trò, Leader quản lý, giá, trạng thái, nút Sửa / Gỡ (phát qua `TBL_CUSTOM_COMP_EMIT`) |
| Dialog | `project-member-add/` (Thêm), `project-member-edit/` (Sửa); Gỡ dùng `ConfirmationService` ngay trong màn |
| Service / model | `services/project-member.service.ts`, `services/staff-group.service.ts` (dùng paging); `models/project-member.model.ts`, `models/staff-group.model.ts` |
| Hằng | `shared/constants/project-member.constants.ts`; 4 key `Function.Operation.*` trong `permission.constants.ts` |

- **Chỉ Admin** (quyết định 2026-11-23, khác bảng CASE của task cho cả Leader): chỉ giữ cặp `viewerRole = 'Admin'` từ
  `construction/project/paging?pageSize=-1`. Không còn cặp nào → "Chỉ Admin quản lý thành viên dự án", ẩn bảng và nút Thêm.
- Cặp qua picker dùng chung (`viewerRoles = ['Admin']`); thiếu / sai cặp trên URL → cặp đầu tiên (dự án sắp theo tên).
- Dự án `DONE` / `EXPIRED` → banner xám chỉ xem, ẩn Thêm và Sửa / Gỡ (BE chặn `818` cả SuperAdmin).
- Số trên 4 tab = `totalItems` của 4 lần paging `pageSize 1` cùng từ khoá; tải lại khi đổi cặp / từ khoá / sau Thêm, Gỡ.
- Sửa chỉ gửi phần đã đổi: giá qua `member/price`, rồi Leader quản lý / Đang làm qua `member/{id}`. Giá lưu được mà bước sau lỗi →
  giữ dialog, lần bấm sau chỉ gửi phần còn lại; đóng bằng Huỷ vẫn tải lại bảng.
- Giá đã có không xoá được (BE: `null` = giữ nguyên) → lỗi tại ô, khoá Lưu. Thực nhận cần giá công ty và không vượt giá công ty.
- Lỗi trong dialog giữ dialog; `onMemberError` báo màn tải lại cặp (`805`, `818`) hoặc bảng (`806`).
- ⚠️ Ô "Admin quản lý" / "Không chọn" dùng giá trị giả `'admin'` / `0` vì `p-select` coi option có value `null` là chưa chọn. Id thật
  là GUID / số dương nên không trùng; lúc gửi BE phải đổi lại thành không gửi (`clearManager` khi sửa).
- ⚠️ Ô chọn người tìm qua server (`onFilter` của `sh-multi-select`): mỗi lần tìm thay `options`, nên phải ghép lại người đã chọn kèm nhãn
  đã nhớ, nếu không chip hiện GUID.

## Nhóm nhân viên

| Thứ | Chỗ |
|---|---|
| Route | `/admin/operation/staff-group?role=&ownerUserId=&keyword=`, `permissionGuard` [`MENU_OPERATION_STAFF_GROUP`, `FUNCTION_STAFF_GROUP_MANAGE`] |
| Màn | `features/operation/staff-group/staff-group-list.component.*` |
| Ô bảng | `staff-group-cells.ts`: tên + mô tả, chip vai trò (`getStaffGroupRoleChip`), nút Sửa / Xoá |
| Dialog | `staff-group-form/` dùng chung Tạo / Sửa (có `group` = Sửa); Xoá dùng `ConfirmationService` ngay trong màn |
| Service / model | `services/staff-group.service.ts` (đủ 9 API), `models/staff-group.model.ts` |
| Hằng | `shared/constants/staff-group.constants.ts`; nhãn vai trò dùng `MEMBER_ROLE_LABELS` |

- Admin và Leader đều vào. Admin = `isSuperAdmin()` hoặc role `Admin`: thấy cột và ô lọc **Trưởng nhóm** (API `owners`). Leader
  không có hai thứ đó, chân bảng thêm "· Bạn chỉ thấy nhóm mình tạo". Không có `FUNCTION_STAFF_GROUP_MANAGE` → không gọi API, bảng
  trống "Bạn không có quyền quản lý nhóm", ẩn nút.
- Giao diện ghi **"Trưởng nhóm"** thay "Chủ nhóm" của bảng CASE / BE (người dùng đổi 2026-11-23); field BE vẫn là `ownerUserId`.
- Lọc ghi lên URL, `queryParamMap` đổi thì đọc lại và về trang 1. Chip `role`: `null` "Chưa có người", có dấu phẩy "Lệch vai trò".
- Dialog Sửa: nhóm đã có người thì khoá vai trò, bỏ hết người vẫn khoá (quyết định 2026-11-23). Nhóm lệch: vai trò = bên đông
  người hơn, hoà thì không chọn; người lệch có chip vàng, ô Thêm người khoá tới khi còn một vai trò (BE `804`).
- Lưu Sửa chỉ gửi phần đổi: `PUT {id}` → `DELETE {id}/members` → `POST {id}/members` (bỏ trước thêm sau). Bước giữa lỗi giữ dialog,
  lần sau chỉ gửi phần còn lại. `802` → lỗi tại ô Tên; `801` → toast, đóng, tải lại bảng.
- ⚠️ HTTP 400 của `[ApiController]` bị `httpErrorInterceptor` báo câu chung. Bốn API ghi (`create`, `update`, `addMembers`,
  `removeMembers`) đặt `SKIP_ERROR_TOAST` để dialog tự báo "Dữ liệu gửi lên không hợp lệ"; gọi chúng phải tự xử lý `error`.
- Gợi ý người gửi `excludeStaffGroupId`, nên người đã lưu vừa bỏ trong dialog không hiện lại trong gợi ý tới khi Lưu.

## Chia phiếu

| Thứ | Chỗ |
|---|---|
| Route | `/admin/operation/assignment?projectId=&documentTypeId=&role=&isActive=&keyword=`, `permissionGuard` [`MENU_OPERATION_ASSIGNMENT`, `FUNCTION_ASSIGNMENT_MANAGE`]; menu đổi chữ "Chia phiếu" |
| Màn | `features/operation/assignment/assignment-member-list.component.*` (bảng người = `member/paging` của Thành viên dự án) |
| Ô bảng / dialog | `assignment-cells.ts` (tên, trạng thái, nút Chia phiếu + tooltip lý do khoá); `assign-dialog/` (860px, cây 2 cấp) |
| Service / model | `services/assignment.service.ts` (`findTree`, `findProfiles`, `preview`, `assign`), `models/assignment.model.ts` |
| Hằng | `shared/constants/assignment.constants.ts` (vai trò, trần 1000 nút, mã lỗi) |

- Picker giữ cặp **Admin hoặc Leader**. Leader: BE chỉ trả người mình quản lý, chân bảng thêm "· Bạn chỉ thấy người mình quản lý".
  Thiếu `FUNCTION_PROJECT_MEMBER_MANAGE` → không gọi bảng người, chỉ hiện câu xin quyền.
- Lọc nằm trên URL; mặc định Nhập liệu + Đang hoạt động (không ghi lên URL). Tab QA ẩn khi cặp không QA. Màn nhớ khoá "cặp + lọc" lần
  tải trước để picker và URL cùng báo đổi chỉ tải một lần; đổi lọc → trang 1.
- Nút Chia phiếu khoá khi người đã tắt hoặc dự án `DONE` / `EXPIRED` (BE `818`). **Không** có nút "Phiếu" (Task 52).
- **Tick lưu trong `Map`**, khoá = hộp thường hoá chữ thường + `*` (cả hộp, `profileRange: null`) hoặc `=` + hồ sơ: tick giữ qua đổi
  trang / lọc. Tick hộp xoá tick hồ sơ con; bỏ một hồ sơ của hộp đang tick → tick các hồ sơ **đã tải** còn lại. Hộp "–" khi có hồ sơ
  con được tick. "Chưa giao trong nút đã chọn" cộng `unassigned` lúc tick. Vượt 1000 → toast cảnh báo, không tick.
- Hồ sơ của một hộp tải một lần khi mở (`pageSize 200` = trần BE), giữ qua đổi trang. `box ""` vẫn gửi lên (service không lọc bỏ).
- Mọi đổi tick xoá kết quả xem trước; Giao chỉ bật khi đã xem trước và `canSave`. Lỗi khi Giao: `812 813 331 824 825 329` → tự xem
  trước lại; `818 805` → đóng, `reload()` picker; `811 821` → đóng, tải lại bảng. Toast lỗi là câu BE (`isResponseSucceed`).
- ⚠️ Ô tick gốc (`<input type="checkbox">`) tự đảo trạng thái khi bấm; tick bị chặn (vượt 1000) thì binding không đổi nên Angular không
  đặt lại DOM → `syncCheckbox` gán tay `checked` / `indeterminate`. Đừng bỏ bước đó.
- ⚠️ Hộp > 200 hồ sơ: chỉ tải 200 hồ sơ đầu, nên bỏ một hồ sơ của hộp đang tick sẽ chỉ tick lại phần đã tải. Chưa gặp trên dữ liệu
  thật; nếu cần thì phân trang hồ sơ.
- Sau lỗi `812` lúc Giao chỉ xem trước lại (đúng CASE 51.14), **không** tải lại cây: số "Chưa giao" của nút vừa bị giao còn cũ tới
  khi đổi trang / lọc.
- Đã kiểm 2026-11-23 bằng Chrome headless trên dự án test riêng (đã xoá): đủ 13 ca mục 51.3 (gồm 2 tab cùng giao → `812`, QA từng
  nhập → `331`, dự án không QA, người đã tắt). **Chưa kiểm**: trần 1000 nút, lỗi `811 / 821` lúc Giao, dòng cảnh báo `warnings`.

> **Tiếp:** [11-man-nhap-xuat.md](11-man-nhap-xuat.md) — màn Nhập / Xuất dữ liệu (wizard import, xuất Excel chạy nền).
