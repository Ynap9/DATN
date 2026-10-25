# Hạn dự án — job kiểm tra hạn, chuyển hết hạn, gửi mail (Construction)

> **Phần 15/18** · trước: [14-ho-so-xoa-xuat.md](14-ho-so-xoa-xuat.md) · mục lục: [README.md](README.md)

Mỗi ngày job nền `ProjectDeadlineJob` (Hangfire, [../architecture/05-infrastructure-external.md](../architecture/05-infrastructure-external.md#backgroundjob--hangfire))
xét các dự án có `EndedAt`: dự án đã quá ngày kết thúc chuyển sang **hết hạn** (`EXPIRED = 5`), dự án sắp tới hạn được báo mail
cho SuperAdmin và Admin. Bản gốc `nhaplieu.be` có job tương tự nhưng bị comment, gửi mail lặp mỗi lần chạy; sh.be làm lại.
Cập nhật 2026-10-25.

## Các chức năng

Gốc `api/v1/construction/project`, mọi API cần `Function.Construction.Project.Deadline`.

| Endpoint | Việc |
|---|---|
| `GET deadline` | Dự án sắp / đã hết hạn, phân trang; lọc `notificationType` (`1` sắp, `2` đã hết hạn, khác → `400`), `keyword` theo tên |
| `POST deadline-check/run` | Chạy job ngay (không chờ lịch), trả `{ jobId }`; chưa đăng ký job → `400` |
| `GET deadline-check/status` | `{ job, warningDays, mailEnabled, recipients[] }`; `job` = `null` khi chưa đăng ký |
| `GET deadline-notification/paging` | Lịch sử gửi mail; lọc `projectId`, `notificationType`, `isSuccess`, `fromDate` / `toDate`, `keyword` (tên dự án, email) |

`GET deadline` trả item `projectId`, `name`, `status`, `startedAt`, `endedAt`, `daysLeft`, `deadlineType`, `lastNotifiedDate`
(mail thành công gần nhất); sắp theo `EndedAt` tăng dần (quá hạn lâu nhất lên đầu). `job` của status gồm `id`, `cron`,
`timeZoneId`, `nextExecution`, `lastExecution` (giờ VN), `lastJobId`, `lastJobState`, `error`.

## Luật phân loại

`daysLeft = EndedAt.Date − hôm nay` (giờ VN), tính trong `ProjectDeadlinePolicy`. Job xét dự án `DEPLOYING` / `DOING` /
`ACCEPTING` / `EXPIRED` có `EndedAt` (`ProjectConst.DEADLINE_STATUSES`); `DONE` không bao giờ bị đụng.

| Điều kiện | Việc | Loại mail |
|---|---|---|
| `daysLeft < 0` hoặc đã `EXPIRED` | Dự án đang hoạt động chuyển `EXPIRED` (`Project.Expire`), audit người thao tác `system` | `2` đã hết hạn |
| `0 ≤ daysLeft ≤ warningDays` | Không đổi trạng thái | `1` sắp hết hạn |
| `daysLeft > warningDays` | Không làm gì | – |

- Ngày kết thúc vẫn được làm việc: hết hạn từ ngày **sau** `EndedAt`.
- Job **không** tự mở lại dự án. Người quản lý mở lại bằng `PUT project/{id}` (đổi `EndedAt` và chọn `status`). `PUT` dự án
  không bị chặn khi dự án hết hạn.
- Dự án `EXPIRED`: import (6 API) → `323`; check, QA, xoá hồ sơ → `323`, trừ SuperAdmin (`Project.EnsureEditable`); xem
  hồ sơ vẫn được. Dự án `DONE` vẫn giữ mã `313` / `316`.

## Gửi mail và chống trùng

- Người nhận: user có email thuộc vai trò SuperAdmin hoặc Admin (`IUserQuery.GetEmailsByRoleNamesAsync`, bỏ trùng).
- Mỗi bộ `(dự án, loại mail, EndedAt, email)` chỉ gửi **một lần thành công**: bảng `construction.ProjectDeadlineNotification`
  có unique index lọc `[IsSuccess] = 1`. Gửi lỗi thì lưu `IsSuccess = 0` + `ErrorMessage`, lần chạy sau gửi lại.
- Gia hạn (đổi `EndedAt`) sinh khoá mới nên được báo lại. Mail sắp hết hạn gửi một lần cho mỗi `EndedAt`, không gửi mỗi ngày.
- Template `Mail/Templates/ProjectDeadline.html`: HTML thuần, không màu. Tiêu đề `Dự án {tên} sắp hết hạn` / `đã hết hạn`.
- Bảng là nhật ký: không xoá mềm, không có API xoá. Lưu kèm `ProjectName` lúc gửi, `DaysLeft`, `Subject`.

## Cấu hình

Hai key trong bảng `ProjectConfig` (toàn cục, sửa bằng API `project-config` có sẵn, xem [08-cau-hinh-du-an.md](08-cau-hinh-du-an.md)):

| Key | Mặc định khi thiếu / sai | Nghĩa |
|---|---|---|
| `DeadlineWarningDays` | `3` | Số ngày báo trước, số nguyên ≥ 0 |
| `DeadlineMailEnabled` | `true` | `false` thì vẫn chuyển `EXPIRED`, chỉ không gửi mail và không ghi lịch sử |

Thiếu key **không** ném `304`: job dùng mặc định và log cảnh báo. Lịch chạy ở appsettings `BackgroundJob:Jobs:ProjectDeadlineCheck`
(mặc định `0 7 * * *`, giờ VN); đổi lịch phải restart app.

## Luồng job — `ProjectDeadlineJob.ExecuteAsync`

1. Lấy khoá `sp_getapplock` mức **phiên** (`project-deadline-check`, chờ 0 ms). Không lấy được → log "đang có lần chạy khác"
   rồi thoát, không ném lỗi (Hangfire không retry). Khoá giữ suốt cả job, nhả trong `finally`.
2. Đọc cấu hình, nạp dự án; chuyển hết hạn + audit trong **một transaction**. Lỗi → rollback, ném lại để Hangfire retry (2 lần).
3. Gửi mail **ngoài transaction**: 1 mail lỗi không chặn mail khác; lưu lịch sử bằng một `SaveChanges` cuối.
4. Log kết thúc: số dự án hết hạn, số mail thành công / lỗi.

`POST run` gọi `TriggerRecurring` nên chạy đúng job định kỳ; job chạy nền, API trả ngay, xem kết quả qua status và lịch sử.

## Đã kiểm thực tế

Ngày 2026-10-24/25, gọi API thật bằng `admin`, SMTP trỏ vào SMTP sink cục bộ (script `test_deadline.py` trong scratchpad),
5 dự án test (hết hôm qua, hôm nay, +2, +10, `DONE` hôm qua), thiếu cả hai key cấu hình: 43/44 đạt. Dòng không đạt là lỗi của
script (tìm chữ trong thân mail mã hoá base64); thân mail giải mã ra đúng.

- `status`: job `0 7 * * *`, `SE Asia Standard Time`, `nextExecution` 07:00; thiếu cấu hình → `3` / `true`; 2 người nhận.
- Run 1: dự án hôm qua → `5` + audit `system`; `DONE` giữ `4`; mail đúng 6 dự án × 2 người; `+10` không có mail.
  Run 2 cùng ngày: không gửi lại. Đổi `EndedAt` → báo lại. SMTP từ chối → `IsSuccess = 0` có lỗi; sửa SMTP → gửi lại được.
- `DeadlineMailEnabled = false` → vẫn chuyển `EXPIRED`, không thêm lịch sử. Một phiên SQL giữ khoá rồi gọi `run` → job log
  "đang có lần chạy khác" và thoát; hai `run` liền nhau cũng có một lần thoát vì khoá.
- Dự án `EXPIRED`: import verify → `323`; check bằng `tester` → `323`, `admin` (SuperAdmin) không bị chặn; xem chi tiết vẫn được.
  `PUT project/{id}` về `DOING` + `EndedAt` mới → import hết `323`. `GET project/{id}` vẫn chạy sau khi thêm route `deadline`.
- `notificationType = 3` → `400`; `fromDate > toDate` → `400`; `viewer` → `403`; không token → `401`.
- Theo yêu cầu người dùng, gửi thật qua Gmail một mail "sắp hết hạn" và một mail "đã hết hạn" tới 2 tài khoản admin.

## Giới hạn

⚠️ Mail gửi xong mà `SaveChanges` lịch sử lỗi thì lần sau có thể gửi trùng: chấp nhận, vì không giữ transaction khi chờ SMTP.

⚠️ `Project.Update` cho phép đặt tay `status = 5`; job coi dự án `EXPIRED` như đã hết hạn và gửi mail loại `2` kể cả khi
`EndedAt` còn ở tương lai.

⚠️ Múi giờ `SE Asia Standard Time` là tên trên Windows. Chạy Docker Linux thì cả `GetVietnamTime()` lẫn lịch job lỗi; khi
triển khai phải đổi sang `Asia/Ho_Chi_Minh` ở một chỗ (`BaseService.VietnamTimeZone`).

⚠️ DB dev có dự án `EndedAt` đã qua và một admin có email thật: bật app với `BackgroundJob:Enabled = true` là 07:00 job sẽ
chuyển dự án sang `EXPIRED` và gửi mail thật. Máy dev không muốn vậy thì đặt `Enabled = false` hoặc `DeadlineMailEnabled = false`.

> **Tiếp:** [16-dong-goi-cau-hinh.md](16-dong-goi-cau-hinh.md) — cấu hình metadata đóng gói theo TT05: loại gói, schema, trường, bảng mã.
