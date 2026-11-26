# Màn module Operation

> **Phần 10/13** · trước: [09-man-qa.md](09-man-qa.md) · mục lục: [README.md](README.md)

Hai màn: Thành viên dự án (Task 47, mở cho Leader ở Task 60 ngày 2026-11-27), Nhóm nhân viên (Task 49), 2026-11-23; màn Chia phiếu ở [13-man-chia-phieu.md](13-man-chia-phieu.md).
BE ở [../../be/nghiep-vu/21-thanh-vien-du-an.md](../../be/nghiep-vu/21-thanh-vien-du-an.md),
[24-don-gia-tien-cong.md](../../be/nghiep-vu/24-don-gia-tien-cong.md) và [19-nhom-nhan-vien.md](../../be/nghiep-vu/19-nhom-nhan-vien.md).

**Picker dùng chung** `shared/components/project-pair-picker` (`app-project-pair-picker`): tải mọi cặp một lần
(`construction/project/paging?pageSize=-1`), giữ cặp có `viewerRole` trong input `viewerRoles`, nhớ cặp trên URL
(`?projectId=&documentTypeId=`, merge với tham số lọc khác). Phát `pairsLoaded` (danh sách) và `pairChange` (cặp mới; `null` khi
mất cặp). `reload()` tải lại danh sách và **luôn** phát lại cặp đang chọn với dữ liệu mới (vd sau `818` dự án vừa đóng).

## Quyền hiện mục menu Vận hành (Task 57)

Mục menu xét key trong `app.menu.ts`; có **bất kỳ** key trong mảng là hiện. Leader không được cấp key `Menu.Operation.*` (tick menu cấp luôn mọi key con, vd `BonusPool.View`, [BE 03](../../be/nghiep-vu/03-vai-tro-mac-dinh.md)) nên menu phải nhận cả key `Function`.

- **57.1 Chia phiếu:** `[MENU_OPERATION_ASSIGNMENT, FUNCTION_ASSIGNMENT_MANAGE]`, giống `ASSIGNMENT_PERMISSIONS` của route. Leader có `Assignment.Manage` thấy menu, mở được, picker tự chọn cặp mình là Leader.
- **57.2 Thành viên dự án:** Task 57 giữ chỉ `MENU_OPERATION_PROJECT_MEMBER`; Task 60 (M2-F08) mở màn cho Leader nên menu đổi sang
  `[MENU_OPERATION_PROJECT_MEMBER, FUNCTION_PROJECT_MEMBER_MANAGE]`, Leader có `ProjectMember.Manage` thấy menu.
- **57.3 Nhóm nhân viên:** giữ nguyên. `Menu.Operation.StaffGroup` chỉ có một con nên Leader có `StaffGroup.Manage` là menu tự bật (`ResolveGrantedKeys`).
- **57.4:** mảng key viết tại chỗ trong `app.menu.ts`, không export từ `operation.routes.ts` (menu không phụ thuộc module lazy).

Đã test trình duyệt 2026-11-26 (user tạm, đã xoá): Leader thấy Chia phiếu + Nhóm nhân viên, chưa thấy Thành viên dự án (Task 60 đã mở); Data / QA không thấy cả ba mục Vận hành trừ Tiền công; Admin đủ mục.

## Thành viên dự án

| Thứ | Chỗ |
|---|---|
| Route | `/admin/operation/project-member?projectId=&documentTypeId=`, `permissionGuard` và mục menu cùng [`MENU_OPERATION_PROJECT_MEMBER`, `FUNCTION_PROJECT_MEMBER_MANAGE`] |
| Màn | `features/operation/project-member/project-member-list.component.*` |
| Ô bảng | `project-member-cells.ts`: vòng chữ cái, chip vai trò, Leader quản lý, giá (Admin / 2 ô riêng của Leader), trạng thái, nút Sửa / Gỡ (phát qua `TBL_CUSTOM_COMP_EMIT`) |
| Dialog | `project-member-add/` (Thêm), `project-member-edit/` (Sửa); Gỡ dùng `ConfirmationService` ngay trong màn |
| Service / model | `services/project-member.service.ts`, `services/staff-group.service.ts` (dùng paging); `models/project-member.model.ts`, `models/staff-group.model.ts` |
| Hằng | `shared/constants/project-member.constants.ts`; 4 key `Function.Operation.*` trong `permission.constants.ts` |

- Cặp qua picker dùng chung (`viewerRoles = ['Admin', 'Leader']`); thiếu / sai cặp trên URL → cặp đầu tiên (dự án sắp theo tên).
  Không có cặp nào → "Bạn chưa là Leader của dự án nào", ẩn bảng và nút Thêm.
- **Chế độ Leader** = `pair.viewerRole === 'Leader'` (theo cặp, không theo vai trò hệ thống). Admin giữ nguyên giao diện Task 47.
  Leader: chip "Người bạn quản lý" + banner xanh, tab Tất cả / Nhập liệu / QA, bỏ cột Leader quản lý; BE chỉ trả chính mình + người
  mình quản lý. Dòng của mình lên đầu trang, nền accent (`rowClass`), nhãn "Bạn", không thao tác. Giá công ty chỉ xem (khoá);
  giá thực nhận trống → "Chưa đặt giá" hoặc "Chờ Admin đặt giá công ty" khi chưa có giá công ty.
- Dialog của Leader (`isLeaderView` trong `IProjectMemberDialogData`): Thêm chỉ Nhập liệu / QA, không gửi `managedByUserId` (BE ép
  là mình), nhóm chỉ nhóm mình là trưởng (BE lọc). Sửa chỉ gửi `actualPrice` (gửi `companyPrice` là `809`), không đổi Leader quản lý;
  chưa có giá công ty thì khoá ô giá thực nhận + hộp vàng (BE `808`).
- Cột và tab gán lại khi đổi cặp, không dùng getter: getter trả mảng mới mỗi lần render làm `DataTable` dựng lại cột.
- Dự án `DONE` / `EXPIRED` → banner xám chỉ xem, ẩn Thêm và Sửa / Gỡ (BE chặn `818` cả SuperAdmin).
- Số trên 4 tab = `totalItems` của 4 lần paging `pageSize 1` cùng từ khoá; tải lại khi đổi cặp / từ khoá / sau Thêm, Gỡ.
- Sửa chỉ gửi phần đã đổi: giá qua `member/price`, rồi Leader quản lý / Đang làm qua `member/{id}`. Giá lưu được mà bước sau lỗi →
  giữ dialog, lần bấm sau chỉ gửi phần còn lại; đóng bằng Huỷ vẫn tải lại bảng.
- Giá đã có không xoá được (BE: `null` = giữ nguyên) → lỗi tại ô, khoá Lưu. Thực nhận cần giá công ty và không vượt giá công ty.
- Lỗi trong dialog giữ dialog; `onMemberError` báo màn tải lại cặp (`805`, `818`) hoặc bảng (`806`). `821` / `808` / `809` toast câu BE.
- Đã kiểm 2026-11-27 trên dự án test `ZZTEST60` (đã xoá): 24/24 lần kiểm phủ 12 ca bảng 60.2 — Admin, 2 Leader cùng cặp,
  Leader không có cặp, dự án Đã xong, dự án không QA (nút QA khoá), lỗi `821` giả lập (toast câu BE, dialog giữ).
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

> **Tiếp:** [11-man-nhap-xuat.md](11-man-nhap-xuat.md) — màn Nhập / Xuất dữ liệu (wizard import, xuất Excel chạy nền).
