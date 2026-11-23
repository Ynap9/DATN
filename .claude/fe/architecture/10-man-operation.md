# Màn module Operation

> **Phần 10/10** · trước: [09-man-qa.md](09-man-qa.md) · mục lục: [README.md](README.md)

Task 47 (2026-11-23). Hiện có một màn: Thành viên dự án. BE ở [../../be/nghiep-vu/21-thanh-vien-du-an.md](../../be/nghiep-vu/21-thanh-vien-du-an.md)
và [24-don-gia-tien-cong.md](../../be/nghiep-vu/24-don-gia-tien-cong.md).

## Thành viên dự án

| Thứ | Chỗ |
|---|---|
| Route | `/admin/operation/project-member?projectId=&documentTypeId=`, `permissionGuard` [`MENU_OPERATION_PROJECT_MEMBER`, `FUNCTION_PROJECT_MEMBER_MANAGE`] |
| Màn | `features/operation/project-member/project-member-list.component.*` |
| Ô bảng | `project-member-cells.ts`: vòng chữ cái, chip vai trò, Leader quản lý, giá, trạng thái, nút Sửa / Gỡ (phát qua `TBL_CUSTOM_COMP_EMIT`) |
| Dialog | `project-member-add/` (Thêm), `project-member-edit/` (Sửa); Gỡ dùng `ConfirmationService` ngay trong màn |
| Service / model | `services/project-member.service.ts`, `services/staff-group.service.ts` (chỉ paging); `models/project-member.model.ts`, `models/staff-group.model.ts` |
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

> **Tiếp:** hết bộ tài liệu — quay lại [README.md](README.md).
