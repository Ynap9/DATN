# Màn module Operation

> **Phần 10/10** · trước: [09-man-qa.md](09-man-qa.md) · mục lục: [README.md](README.md)

Hai màn: Thành viên dự án (Task 47) và Nhóm nhân viên (Task 49), 2026-11-23. BE ở
[../../be/nghiep-vu/21-thanh-vien-du-an.md](../../be/nghiep-vu/21-thanh-vien-du-an.md),
[24-don-gia-tien-cong.md](../../be/nghiep-vu/24-don-gia-tien-cong.md) và [19-nhom-nhan-vien.md](../../be/nghiep-vu/19-nhom-nhan-vien.md).

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
- Cặp đang chọn ghi lên URL; thiếu / không còn trong danh sách → ghi cặp đầu tiên (dự án sắp theo tên). Đổi dự án → loại đầu tiên.
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

> **Tiếp:** hết bộ tài liệu — quay lại [README.md](README.md).
