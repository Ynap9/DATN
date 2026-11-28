# Màn Hạn dự án (Construction)

> **Phần 16/16** · trước: [15-man-bao-cao-tien-do.md](15-man-bao-cao-tien-do.md) · mục lục: [README.md](README.md)

Task 73 (2026-11-28): `/admin/construction/project-deadline` (`features/construction/project-deadline/`). Job hạn dự án chạy 07:00 mỗi ngày, tự
chuyển Hết hạn và gửi mail cho SuperAdmin / Admin ([BE 15](../../be/nghiep-vu/15-han-du-an.md)); màn này cho xem job chạy lần cuối lúc nào, dự án nào
sắp / đã hết hạn, mail nào gửi lỗi, và chạy kiểm hạn ngay. Không sửa BE, không thêm key quyền.

## Route, quyền và file

| Thứ | Chỗ |
|---|---|
| Route + menu | `project-deadline`, `permissionGuard` chỉ nhận `FUNCTION_PROJECT_DEADLINE` (`Function.Construction.Project.Deadline`); menu "Hạn dự án" (`pi pi-calendar-clock`) ngay dưới "Dự án". Leader mặc định không có key nên không thấy menu, dán URL ra `auth/access` |
| Màn cha | `project-deadline.component.*`: nút Làm mới / Kiểm hạn ngay, thẻ job, thanh hai tab, tab đang mở lấy từ URL |
| Thẻ job | `deadline-status-card/` (`deadline-cron.ts` đổi `m h * * *` thành "Hằng ngày HH:mm") |
| Hai tab | `deadline-attention-tab/` (Dự án cần chú ý), `deadline-mail-tab/` (Lịch sử mail), ô bảng riêng ở `cells/` |
| Query | `deadline-query.ts`: `readDeadlineQuery`, `updateDeadlineQuery`, `toSuccessParam` |
| Hằng / model | `project-deadline.constants.ts`, `project-deadline.model.ts`; `ProjectService` có 4 hàm `findDeadlinePaging`, `runDeadlineCheck`, `findDeadlineCheckStatus`, `findDeadlineNotificationPaging` |

## Thẻ job và Kiểm hạn ngay

- Thẻ: lịch chạy, lần chạy gần nhất (kèm tag Thành công / Lỗi / Đang chạy / Chưa chạy, dòng `error` nhỏ khi Lỗi), lần kế, báo trước, mail
  Đang bật / Đang tắt, người nhận (bấm mở popover email; 0 người thì chữ đỏ). Link "Sửa ở Cấu hình" chỉ khi có `MENU_PROJECT_CONFIG`.
- `job = null`: banner amber thay thẻ và khoá nút Kiểm hạn ngay. Mail đang tắt: banner info dưới thẻ, thẻ vẫn hiện.
- Kiểm hạn ngay: hộp xác nhận (nói số người nhận khi mail bật) → `POST deadline-check/run` → hỏi `deadline-check/status` mỗi 2 giây, tối đa 15
  lần, tới khi `lastJobId` = `jobId` vừa tạo và không còn Enqueued / Processing. Xong: toast "Đã kiểm hạn xong" (Failed thì toast lỗi kèm
  `error`) và tải lại tab đang mở. Hết 15 lần: toast info rồi nhả nút. Rời màn thì dừng hỏi (`takeUntilDestroyed`).

## Hai tab

- Dự án cần chú ý: thanh Tất cả / Sắp hết hạn / Đã hết hạn + ô tìm tên (400 ms), giữ thứ tự BE (quá hạn lâu nhất trước). Cột Còn lại: "Còn n
  ngày" / "Hết hạn hôm nay" màu amber, "Quá n ngày" màu đỏ. "Gia hạn" (cần `Project.Update`, ẩn khi Nghiệm thu / Xong) gọi `findById` rồi mở
  `ProjectFormComponent` như danh sách dự án, lưu xong tải lại tab và thẻ. "Xem mail" sang tab Lịch sử mail lọc đúng dự án.
- Loại báo hạn (Task 74): `1` sắp, `2` đã hết hạn, `3` "Nghiệm thu quá hạn" (dự án đang nghiệm thu đã quá ngày kết thúc, nhãn amber). Hằng `DEADLINE_TYPE`, `DEADLINE_TYPE_FILTERS` dùng chung nên cả
  thanh lọc tab Dự án cần chú ý và bộ lọc Loại tab Lịch sử mail có mục này; `type=3` hợp lệ trên URL. "Gia hạn" vẫn ẩn với dự án nghiệm thu, gia hạn ở màn Dự án (Sửa).
- Lịch sử mail: lọc Loại, Kết quả (`isSuccess`), Từ ngày / Đến ngày, ô tìm tên dự án hoặc email; chip "Dự án: {tên} ×". Từ ngày sau Đến ngày
  thì chữ đỏ và không gọi API (BE trả `400`). Tag Lỗi bấm mở popover `errorMessage`.

## URL

`tab` (tab đầu không ghi), `type`, `success` (`true` / `false`), `from`, `to`, `keyword`, `project`, `projectName`. Mỗi tab đọc URL **một lần**
khi được dựng rồi ghi lại khi đổi lọc (`replaceUrl`), nên F5 giữ tab và bộ lọc; đổi tab thì xoá bộ lọc của tab cũ. `projectName` chỉ để hiện
chip (người chỉ có quyền Deadline có thể thiếu `Project.View`, nên không gọi `GET project/{id}` lấy tên); thiếu thì chip hiện `#id`.

Lưu ý: chỉ tab đang mở được dựng (`@if`), nên đừng thêm nơi giữ bộ lọc ở màn cha: tab dựng lại là đọc URL lại. Số trang không ghi lên URL.
Lưu ý: `sh-select` / `sh-calendar` phát lại giá trị lúc khởi tạo, nên hàm đổi lọc so với giá trị đang giữ rồi mới ghi URL và tải lại.

## Đã kiểm thực tế (Task 73)

Ngày 2026-11-28, Chrome thật + API thật trên DB dev, dự án tạm ZZTEST73A (hết hôm qua), 73B (hôm nay), 73C (+2 ngày), 73D (+10 ngày), tắt
`DeadlineMailEnabled` trước khi chạy job (không gửi mail thật), bật lại sau.

- Kiểm hạn ngay: nút loading, toast "Đã kiểm hạn xong", 73A thành Hết hạn "Quá 1 ngày", 73B "Hết hạn hôm nay", 73C "Còn 2 ngày", không có 73D,
  thẻ cập nhật lần chạy gần nhất. Gia hạn 73A sang +5 ngày: form có gợi ý "Lưu xong dự án về Đang triển khai", lưu xong 73A rời tab.
- Quyền: user chỉ có `Project.Deadline` (role tạm) thấy menu, ⋯ chỉ có "Xem mail", không có link "Sửa ở Cấu hình". Leader không thấy menu,
  dán URL ra "Không có quyền truy cập".
- Lịch sử mail (1 dòng lỗi chèn SQL, DB không có dòng lỗi thật): "Xem mail" ra chip + URL; bỏ chip; lọc Lỗi; popover lỗi; khoảng ngày
  sai hiện chữ đỏ; F5 giữ tab + bộ lọc ở cả hai tab. Banner "Mail báo hạn đang tắt" hiện đúng. Link ở màn Cấu hình mở được màn này.
- Task 74 (2026-11-28, SMTP giả cục bộ): dòng mail loại 3 của `ZZTEST74B` hiện nhãn "Nghiệm thu quá hạn", "Quá 2 ngày", tiêu đề "… đã quá hạn khi đang nghiệm thu"; chọn Loại "Nghiệm thu quá hạn" ghi `type=3` lên URL và lọc đúng.
- Chỉ đọc code: người nhận 0 (DB có Admin có email nên không tạo được ca), nhánh `job = null`, job Failed, hết 15 lần hỏi.
- Chạy lại checklist 73 ngày 2026-11-28 trên môi trường riêng (LocalDB `ZZTEST73`, SMTP giả cục bộ từ chối địa chỉ có "bad" nên có dòng mail Lỗi thật, BE / FE thật; dự án tạm ZZTEST73A–D, user tạm, role tạm `ZZ73Only` chỉ có `Project.Deadline`; DB đã drop):
  khớp biên bản trên ở thẻ job (popover 3 email), Kiểm hạn ngay (73A Hết hạn, 73B / 73C đúng nhãn, không có 73D), Gia hạn 73A (gợi ý `endedAtHint`, về Đang triển khai), quyền (user chỉ có Deadline chỉ thấy "Xem mail", không có link Cấu hình; Leader không có menu, dán URL bị chặn), lịch sử mail
  (lọc Lỗi 3 dòng + popover "Mailbox unavailable", tìm email, khoảng ngày, chip + ×, F5 giữ tab và bộ lọc), banner mail tắt, người nhận 0 (chữ đỏ, tái hiện được bằng cách bỏ email Admin), loại 3 (tab 1, lọc, 3 mail).
- Lệch 1 (chưa sửa): `deadline-attention-tab` gọi `GET document-type/dropdown` ngay khi mở tab, nên user chỉ có `Project.Deadline` (không có `DocumentType.View`) nhận 403 và toast "Bạn không có quyền thực hiện thao tác này" mỗi lần tải màn. Đề xuất chỉ gọi khi có `Project.Update` hoặc khi mở form Gia hạn.
- Lệch 2 (chưa sửa): job lỗi mà Hangfire đang tự thử lại có state `Scheduled`, `error` rỗng, nên thẻ hiện "Chưa chạy" thay vì "Lỗi" và người dùng không thấy job đang hỏng; chỉ `Failed` (hết lần thử) mới ra tag Lỗi. Tái hiện bằng cách đổi tên tạm bảng `core.AuditLog` rồi chạy job với dự án quá hạn. Spec 73.4 không có state này.
- Kiểm bù cùng ngày (DB `ZZTEST73` dựng lại, đã drop): hết 15 lần hỏi (khoá bảng `ProjectDeadlineNotification` 90 giây bằng SQL, sau ~30 giây toast "Job vẫn đang chạy, bấm Làm mới sau ít phút", nút bật lại, job xong thì thẻ về Thành công); job `Failed` (chèn state Failed vào Hangfire): tag "Lỗi" đỏ, thêm `Error` của recurring job thì hiện câu lỗi đỏ dưới tag.
- Vẫn chỉ đọc code: nhánh `job = null` (BE tự đăng ký lại job khi xoá, không tái hiện được) và toast "Đã kiểm hạn xong" (hiện dưới 4 giây).

> **Tiếp:** hết bộ tài liệu — quay lại [README.md](README.md).
