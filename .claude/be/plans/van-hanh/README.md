# Plan: module Operation — nhóm nhân viên, thành viên dự án, đơn giá, chia phiếu theo cây

> **Trạng thái:** 🔶 làm dở — Task 9 xong 2026-10-26 (nền móng: schema, mã lỗi, quyền; đã `database update` + gán quyền Leader / Data trên DB dev);
> Task 10 xong 2026-10-26 (8 API nhóm nhân viên + luật `109` Data / QA) · người làm: TV2 · thay T2.2–T2.5 cũ.

Chia tự động (ngẫu nhiên, theo tiến độ) **không** nằm trong plan này — để làm thuật toán sau; phiếu chừa sẵn `Mode`.

## Mục lục

| Phần | Nội dung |
|---|---|
| [01-nghiep-vu.md](01-nghiep-vu.md) | Luật Admin / Leader, đơn giá 3 vai trò × 2 loại, quỹ KPI, chia phiếu, bảng dữ liệu |
| [02-api.md](02-api.md) | 28 API, quyền mới |

## Quyết định đã chốt (2026-10-25)

| # | Quyết định | Vì sao |
|---|---|---|
| 1 | Thành viên gắn theo **(dự án, loại văn bản)** | Mỗi loại văn bản có số trường khác nhau nên đơn giá khác nhau (giống `ProjectDocumentTypeUser` của 5sdb) |
| 2 | Đơn giá Leader tính theo **mỗi phiếu đạt** | Phiếu `DONE` khi mọi văn bản trong phiếu QA Đạt |
| 3 | Một người **chỉ một vai trò** trong một (dự án, loại văn bản) | Tránh tự QA hồ sơ mình nhập; khoá duy nhất không có `Role` |
| 4 | Module mới **`Operation`**, schema DB `operation` | Tách đúng phần của TV2, không đụng `Construction` |
| 5 | (2026-10-26) Vai trò lưu đúng chuỗi `RoleConstants` `Leader` / `Data` / `QA`; người giữ văn bản ở `SubmissionUser` (Task 18B), **không** thêm cột vào `Submission` | Một nguồn tên vai trò; `Submission` chỉ chứa dữ liệu văn bản |
| 6 | (2026-10-26) Cây quyền có gốc `Menu.Operation` như module khác; đơn giá `decimal(18,2)` | FE dựng menu đúng nhóm; giá lẻ được (12,5 đ / trường) |

## Tham khảo

- 5sdb `nhaplieu.be`: `ProjectDocumentTypeUser` (giá theo người), luật Leader chỉ thêm `staff` và giá không vượt
  giá Leader (`UserService.AddUserToProjectAsync`), `UserRangesTracking` + `ManualSubmissionsSplitByProfileRangeAndBox`.
- soHoaSaoMai `sao_mai_be`: `UserGroup` (thành viên nhiều nhóm), `OcrAssignment` (phiếu `active/cancelled/done`,
  đơn vị `hop/hoSo`, luồng `preview → save → revoke`), `ProjectCostSnapshot` (tính sẵn chi phí).

## Input

- `Project`, `DocumentType`, `ProjectDocumentType` đã có; `Submission` có `Box`, `ProfileRange`.
- Role `Leader`, `Data`, `QA` có trong `RoleConstants`.

## Steps

1. Tạo module theo [08-them-chuc-nang.md](../../architecture/08-them-chuc-nang.md): `domain/Module/Operation`,
   `applications/Module/Operation`, controller `api/v{version}/operation/...`.
2. ✅ Entity + cấu hình EF + migration `Add_OperationModule` schema `operation`; index `(ProjectId, DocumentTypeId, Box, ProfileRange)`
   trên `Submission` (Task 9).
   Task 10 nhóm · 11 thành viên · 12 đơn giá · 13 cây + giao phiếu · 14 thu hồi / chuyển / quỹ KPI · 15 ghép với hồ sơ.
3. Thêm quyền vào `PermissionConstants`, gán role trong seeder.
4. O2 nhóm nhân viên → O3 thành viên → O4 đơn giá → O5 cây → O6 phiếu → O7 quỹ KPI (thứ tự API ở [02](02-api.md)).
5. Viết tài liệu nghiệp vụ `.claude/be/nghiep-vu/17-…` trở đi; chạy thử từng API.
6. `dotnet build sh.be/sh.be.api/sh.be.api.sln` 0 lỗi; `has-pending-model-changes` = "No changes" sau migration.

## Expected output

- Admin thêm Leader + nhân viên, Leader thêm nhân viên / nhóm, kèm đơn giá công ty + thực nhận.
- Màn chia phiếu đọc cây Hộp → Hồ sơ, xem trước, lưu, thu hồi, chuyển phiếu.
- `GET /my-assignment` cho màn nhập liệu của TV1; `GET bonus-pool` ra quỹ KPI theo kỳ.

## Điểm cần chú ý

- Đổi giá = thêm dòng mới có `EffectiveFrom`, không sửa dòng cũ — kỳ đã tính không bị đổi tiền.
- Dòng `SubmissionUser` Role `Data` (`TicketId` = phiếu) luôn ghi cùng transaction với phiếu; xoá dòng = văn bản về pool.
- Thêm nhóm vào dự án là bung ra từng người tại thời điểm đó; nhóm đổi sau không kéo theo dự án.
- Cây group theo `Box`, `ProfileRange` trên bảng lớn — cần index ở bước 2, không thì quét toàn bảng.
