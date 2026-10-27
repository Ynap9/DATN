# Nghiệp vụ backend sh.be

> Chức năng và luật nghiệp vụ của module `Auth` (người dùng, vai trò, quyền), `Core` (nhật ký hệ thống) và
> `Construction` (loại văn bản, dự án, hồ sơ), `DigitalSignature` (mẫu chữ ký, lô ký), `Packaging` (cấu hình đóng gói TT05), `Operation` (vận hành — nền móng
> Task 9, nhóm nhân viên Task 10, thành viên dự án Task 11, plan ở [../plans/van-hanh/README.md](../plans/van-hanh/README.md)) và `Ai` (cửa gọi AI — nhà cung cấp Task 19).
> Đối chiếu với code `sh.be` ngày 2026-10-19. Kiến trúc và quy ước code nằm ở [../architecture/README.md](../architecture/README.md).

## Mục lục

| File | Nội dung |
|---|---|
| [01-nguoi-dung.md](01-nguoi-dung.md) | Tạo, sửa hồ sơ, xoá, đổi mật khẩu, gán vai trò, đăng nhập lấy token |
| [02-phan-quyen.md](02-phan-quyen.md) | Cây quyền, luật cấp và kiểm quyền, bảng endpoint → key quyền |
| [03-vai-tro-mac-dinh.md](03-vai-tro-mac-dinh.md) | Vai trò, quyền mặc định, tài khoản khởi tạo, luật tạo / xoá vai trò |
| [04-nhat-ky-he-thong.md](04-nhat-ky-he-thong.md) | Các thao tác được ghi nhật ký và cách đọc nhật ký |
| [05-loai-van-ban.md](05-loai-van-ban.md) | Danh mục loại văn bản (`DocumentType`): CRUD, tên duy nhất, xoá mềm |
| [06-truong-van-ban.md](06-truong-van-ban.md) | Trường của loại văn bản (`DocumentField`): CRUD, `Field` duy nhất, thứ tự tự tăng |
| [07-du-an.md](07-du-an.md) | Dự án (`Project`): CRUD, trạng thái `ProjectConst`, tự tạo Google Sheet khi tạo |
| [08-cau-hinh-du-an.md](08-cau-hinh-du-an.md) | Cấu hình dự án (`ProjectConfig`): CRUD key-value, xác thực tài khoản Google lấy refresh token |
| [09-mau-chu-ky.md](09-mau-chu-ky.md) | Mẫu chữ ký (`Template`): tạo / đổi tên / xoá, ghi cấu hình ký kèm ảnh lên MinIO và toạ độ khối |
| [10-lo-ky.md](10-lo-ky.md) | Lô ký (`SigningBatch`): tạo lô, tải / xoá file PDF, phiên ký, runner 3 chặng, đưa thư plugin, tạm dừng / huỷ, tiến độ, bản ký |
| [11-ho-so-import.md](11-ho-so-import.md) | Hồ sơ (`Submission`, `SubmissionData`): import append / blank / overwrite từ Google Sheet, proxy xem file PDF |
| [12-ho-so-kiem-tra.md](12-ho-so-kiem-tra.md) | Hồ sơ: danh sách, lọc, chi tiết, hồ sơ liền kề, đếm, kiểm tra / tái kiểm tra (`SubmissionLog`), lịch sử |
| [13-ho-so-qa.md](13-ho-so-qa.md) | Hồ sơ: QA Đạt / Không đạt, trả lại, `StatusQa` độc lập với `StatusCheck`, lịch sử QA (`SubmissionQaLog`) |
| [14-ho-so-xoa-xuat.md](14-ho-so-xoa-xuat.md) | Hồ sơ: xoá mềm nhiều hồ sơ (chặn hồ sơ đã kiểm tra), xuất Excel theo bộ lọc (trả file ngay hoặc chạy nền Hangfire + link tải) |
| [15-han-du-an.md](15-han-du-an.md) | Hạn dự án: job Hangfire hằng ngày chuyển `EXPIRED`, mail báo sắp / đã hết hạn chống trùng, lịch sử gửi |
| [16-dong-goi-cau-hinh.md](16-dong-goi-cau-hinh.md) | Đóng gói: 20 schema (5 loại gói × 4 loại metadata), trường đúng thứ tự, luật bắt buộc, 13 nhóm mã, seeder |
| [17-thong-ke-qa.md](17-thong-ke-qa.md) | Thống kê QA: 1 văn bản = 1 lượt, người nhập theo lịch sử kiểm tra, tỉ lệ đúng theo ô tính tiền, theo trường / người nhập, dòng thời gian |
| [18-qa-cham-mau.md](18-qa-cham-mau.md) | Chấm mẫu QA: chế độ `FULL` / `SAMPLE` theo QA và dự án, cấu hình mẫu (mặc định 20 văn bản, ngưỡng 50%), công thức cỡ mẫu, điểm móc thuật toán, bảng `SubmissionUser` |
| [19-nhom-nhan-vien.md](19-nhom-nhan-vien.md) | Nhóm nhân viên (Operation): nhóm thuần `Data` hoặc `QA` suy từ role thành viên, Leader chỉ thấy nhóm mình (`801`), 8 API |
| [20-ai-nha-cung-cap.md](20-ai-nha-cung-cap.md) | Module Ai: 5 bảng nền móng, nhà cung cấp AI (`AiProvider`) với 12 luật R1–R12, key mã hoá + SHA-256, tải key file JSON, 8 API, lỗi `901`–`907` |
| [21-thanh-vien-du-an.md](21-thanh-vien-du-an.md) | Thành viên dự án (Operation): ai quản lý ai, không có Leader thì Admin quản lý, thêm hàng loạt có bỏ qua, gỡ = tự thu hồi văn bản chưa làm, 6 API |

## Thuật ngữ

| Thuật ngữ | Nghĩa trong code |
|---|---|
| User (`AppUser`) | Tài khoản đăng nhập, kế thừa `IdentityUser`, có `FullName` |
| Role (`Role`) | Vai trò, gom quyền; một user có thể có nhiều vai trò |
| Permission (`Permission`) | Một node trong cây quyền, định danh bằng `Key` (chuỗi, ví dụ `Function.Auth.Role.Create`) |
| RolePermission | Quyền đã gán cho một vai trò; khoá ghép `(RoleId, PermissionKey)` |
| UserRole | Vai trò đã gán cho một user; khoá ghép `(UserId, RoleId)` |
| AuditLog | Bản ghi nhật ký một thao tác nghiệp vụ |
| SuperAdmin | Vai trò đặc biệt: bỏ qua mọi kiểm tra quyền |
| Xoá mềm | `Deleted = true`; bản ghi bị global query filter ẩn đi |
| Hồ sơ (`Submission`) | Một file số hoá = một dòng Google Sheet; khoá `FileName + Hồ sơ + Hộp` trong dự án + loại văn bản |

## Ba quy tắc xuyên suốt

- Mọi thao tác ghi đều tạo `AuditLog` **trong cùng transaction** với thay đổi dữ liệu (xem [04-nhat-ky-he-thong.md](04-nhat-ky-he-thong.md)).
- Lỗi nghiệp vụ trả về HTTP 200 với `ApiResponse.Status = 0` và mã trong dải `101`–`109` (Auth), `201`–`211` (Core), `301`–`339` (Construction), `601`–`635` (DigitalSignature), `701`–`703` (Packaging), `801`–`823` (Operation, bỏ `814`) hoặc `901`–`907` (Ai).
- Thời gian thao tác lấy theo giờ Việt Nam, người thao tác lấy từ token đăng nhập.

Route trong các file nghiệp vụ viết rút gọn `api/<module>/...`; route thật có version: `api/v1/<module>/...` (từ 2026-10-20,
xem [../architecture/01-api.md](../architecture/01-api.md#version-api)). Riêng `/connect/token`, `/connect/authorize` không có version.

## Giới hạn đã biết

Các giới hạn và bẫy nằm ngay trong từng file, đánh dấu `⚠️`. Chưa có chức năng nào trong module này bị đánh dấu là "đã sửa";
mọi thay đổi hành vi đều cần hỏi trước vì ảnh hưởng tới FE và dữ liệu đang chạy.

> **Tiếp:** [01-nguoi-dung.md](01-nguoi-dung.md) — vòng đời tài khoản người dùng.
