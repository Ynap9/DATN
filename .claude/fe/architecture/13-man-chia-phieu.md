# Màn Chia phiếu (Operation)

> **Phần 13/13** · trước: [12-man-cau-hinh.md](12-man-cau-hinh.md) · mục lục: [README.md](README.md)

Task 51 (giao phiếu) và Task 52 (phiếu của một người, chuyển, thu hồi), 2026-11-23 → 25. BE ở
[23-chia-phieu.md](../../be/nghiep-vu/23-chia-phieu.md) và [26-thu-hoi-chuyen-phieu.md](../../be/nghiep-vu/26-thu-hoi-chuyen-phieu.md).
Picker cặp dùng chung mô tả ở [10-man-operation.md](10-man-operation.md).

| Thứ | Chỗ |
|---|---|
| Route | `/admin/operation/assignment?projectId=&documentTypeId=&role=&isActive=&keyword=`, `permissionGuard` [`MENU_OPERATION_ASSIGNMENT`, `FUNCTION_ASSIGNMENT_MANAGE`]; menu đổi chữ "Chia phiếu" |
| Màn | `features/operation/assignment/assignment-member-list.component.*` (bảng người = `member/paging` của Thành viên dự án) |
| Ô bảng | `assignment-cells.ts`: tên, trạng thái, nút Phiếu + Chia phiếu (tooltip lý do khoá); kiểu data / kết quả của 3 dialog |
| Dialog | `assign-dialog/` (860px, cây 2 cấp), `ticket-dialog/` (860px, phiếu của một người), `move-dialog/` (520px, chuyển / thu hồi) |
| Service / model | `services/assignment.service.ts` (cây, xem trước, giao, phiếu, 4 API chuyển / thu hồi), `models/assignment.model.ts` |
| Hằng | `shared/constants/assignment.constants.ts` (vai trò, trần 1000 nút, trạng thái phiếu, mã lỗi) |

## Bảng người và giao phiếu (Task 51)

- Picker giữ cặp **Admin hoặc Leader**. Leader: BE chỉ trả người mình quản lý, chân bảng thêm "· Bạn chỉ thấy người mình quản lý".
  Thiếu `FUNCTION_PROJECT_MEMBER_MANAGE` → không gọi bảng người, chỉ hiện câu xin quyền.
- Lọc nằm trên URL; mặc định Nhập liệu + Đang hoạt động (không ghi lên URL). Tab QA ẩn khi cặp không QA. Màn nhớ khoá "cặp + lọc" lần
  tải trước để picker và URL cùng báo đổi chỉ tải một lần; đổi lọc → trang 1.
- Nút Chia phiếu khoá khi người đã tắt hoặc dự án `DONE` / `EXPIRED` (BE `818`).
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

## Phiếu, chuyển, thu hồi (Task 52)

- Nút **Phiếu** luôn bấm được (người tắt, dự án đóng vẫn xem). Dialog phiếu: `assignment/ticket/paging` theo tab đang đứng +
  `assigneeUserId`, lọc trạng thái mặc định Đang làm (`1`), "Mọi trạng thái" không gửi `status` và thêm nhãn trạng thái cạnh số phiếu.
  `pageSize 10`. Bảng HTML tự dựng vì `DataTable` không có dòng con; "Chi tiết" gọi `assignment/{id}` một lần / phiếu, hiện chip.
- Chuyển / Thu hồi chỉ ở phiếu `ACTIVE` và dự án chưa đóng. Dialog chuyển / thu hồi mở chồng lên dialog phiếu; phạm vi "Cả phiếu" hoặc
  tick nút của phiếu (lấy nguyên `{ box, profileRange }` từ API 24, dùng lại nút dialog phiếu đã tải). Người nhận = `member/paging`
  cùng vai trò, đang hoạt động, `pageSize 200`, bỏ chính người đó, lọc trong ô chọn.
- Gửi: Nhập liệu cả phiếu `{ ticketId }`, theo nút `revoke { assigneeUserId, items }` / `transfer { fromUserId, toUserId, items }`; QA
  luôn `ticketId`, cả phiếu không gửi `items`.
- Xong: toast, đóng dialog chuyển / thu hồi, tải lại bảng phiếu; đóng dialog phiếu thì bảng người tải lại (cột Phiếu đang làm).
  Lỗi: `815 816 817` → đóng, tải lại bảng phiếu; `818 805` → đóng cả hai, `reload()` picker; mã khác giữ dialog. Bốn API ghi đặt
  `SKIP_ERROR_TOAST` → HTTP 400 báo "Dữ liệu gửi lên không hợp lệ".
- Lệch thiết kế theo CASE: dòng phụ dialog chuyển / thu hồi chỉ "<n> văn bản" (thiết kế có thêm "x chưa kiểm").
- ⚠️ Dev server (`ng serve`) mở lâu không thấy file mới được import lúc file đó chưa tồn tại: bấm Phiếu lại mở dialog Chia phiếu (code
  bảng người cũ). Chạm lại file import (lưu lại) để build lại; `npm run build:admin` không bị.

## Đã kiểm

Chrome headless trên dự án test riêng (đã xoá), 2026-11-23: đủ 13 ca mục 51.3. 2026-11-25: đủ 11 ca mục 52.3 (16 bước) + 14 ca bổ
sung: đủ 6 dạng body của 52.7 (bắt request), lỗi thật `815` (phiếu xoá mềm bằng SQL) / `816` / `817` / `818` / `811` / `821` / `331`,
phân trang 11 phiếu; `812`, `825`, HTTP 400 **giả lập** bằng chặn request. **Chưa kiểm**: trần 1000 nút, `811 / 821` lúc Giao, `warnings`.
Đo trên DB dev qua tunnel: `qa/transfer` mất ~11 giây, `qa/revoke` ~6 giây (nút xoay suốt lúc chờ, không phải lỗi FE).

> **Tiếp:** hết bộ tài liệu — quay lại [README.md](README.md).
