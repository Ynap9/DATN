# Hạn dự án — job kiểm tra hạn, chuyển hết hạn, gửi mail (Construction)

> **Phần 15/22** · trước: [14-ho-so-xoa-xuat.md](14-ho-so-xoa-xuat.md) · mục lục: [README.md](README.md)

Mỗi ngày job nền `ProjectDeadlineJob` (Hangfire, [../architecture/05-infrastructure-external.md](../architecture/05-infrastructure-external.md#backgroundjob--hangfire))
xét các dự án có `EndedAt`: dự án đã quá ngày kết thúc chuyển sang **hết hạn** (`EXPIRED = 5`), dự án sắp tới hạn được báo mail
cho SuperAdmin và Admin. Bản gốc `nhaplieu.be` có job tương tự nhưng bị comment, gửi mail lặp mỗi lần chạy; sh.be làm lại.
Cập nhật 2026-11-28 (gia hạn dự án Task 68; dự án đang nghiệm thu quá hạn, mail loại 3, Task 74).

## Các chức năng

Gốc `api/v1/construction/project`, mọi API cần `Function.Construction.Project.Deadline`.

| Endpoint | Việc |
|---|---|
| `GET deadline` | Dự án sắp / đã hết hạn, phân trang; lọc `notificationType` (`1` sắp, `2` đã hết hạn, `3` đang nghiệm thu quá hạn, khác → `400`), `keyword` theo tên |
| `POST deadline-check/run` | Chạy job ngay (không chờ lịch), trả `{ jobId }`; chưa đăng ký job → `400` |
| `GET deadline-check/status` | `{ job, warningDays, mailEnabled, recipients[] }`; `job` = `null` khi chưa đăng ký |
| `GET deadline-notification/paging` | Lịch sử gửi mail; lọc `projectId`, `notificationType`, `isSuccess`, `fromDate` / `toDate`, `keyword` (tên dự án, email) |

`GET deadline` trả item `projectId`, `name`, `status`, `startedAt`, `endedAt`, `daysLeft`, `deadlineType`, `lastNotifiedDate`
(mail thành công gần nhất); sắp theo `EndedAt` tăng dần (quá hạn lâu nhất lên đầu). `job` của status gồm `id`, `cron`,
`timeZoneId`, `nextExecution`, `lastExecution` (giờ VN), `lastJobId`, `lastJobState`, `error`.

## Luật phân loại

`daysLeft = EndedAt.Date − hôm nay` (giờ VN), tính trong `ProjectDomainService.CalculateDeadlineDaysLeft`. Job xét dự án `DOING` /
`ACCEPTING` / `EXPIRED` có `EndedAt` (`ProjectConst.DEADLINE_STATUSES`); `DONE` không bao giờ bị đụng.

| Điều kiện | Việc | Loại mail |
|---|---|---|
| `ACCEPTING` và `daysLeft < 0` (xét trước) | Không đổi trạng thái, chỉ báo mail (Task 74) | `3` quá hạn khi đang nghiệm thu |
| `daysLeft < 0` hoặc đã `EXPIRED` | Dự án `DOING` chuyển `EXPIRED` (`Project.Expire` chỉ nhận `DOING`), audit người thao tác `system` | `2` đã hết hạn |
| `0 ≤ daysLeft ≤ warningDays` | Không đổi trạng thái | `1` sắp hết hạn |
| `daysLeft > warningDays` | Không làm gì | – |

- Ngày kết thúc vẫn được làm việc: hết hạn từ ngày **sau** `EndedAt`.
- Job **không** chuyển dự án `ACCEPTING` sang `EXPIRED` (Task 74, trước đó có): người dùng đã chốt giữ trạng thái nghiệm thu, Admin tự chuyển bằng
  `POST project/{id}/acceptance/expire` khi dự án quá ngày kết thúc ([22](22-nghiem-thu-du-an.md)); sau đó gia hạn rồi bắt đầu nghiệm thu lại.
- Job **không** tự mở lại dự án. Người quản lý mở lại bằng `PUT project/{id}` dời `EndedAt` từ hôm nay trở đi ([Gia hạn dự án](#gia-hạn-dự-án--task-68)).
  `PUT` dự án không bị chặn khi dự án hết hạn.
- Dự án `EXPIRED`: import (6 API) → `323`; check, QA, xoá hồ sơ → `323`, trừ SuperAdmin (`Project.EnsureEditable`); xem
  hồ sơ vẫn được. Dự án `DONE` vẫn giữ mã `313` / `316`.

## Gia hạn dự án — Task 68

Chốt 2026-11-27, như 5sdb: `EXPIRED` do hệ thống đặt theo ngày kết thúc, người dùng không chọn tay (5sdb không cho sửa trạng thái
trong form dự án); gia hạn = dời `EndedAt`. `Project.Update` kiểm theo thứ tự: trạng thái hợp lệ → kỳ hạn hợp lệ → luật nghiệm thu →
bảng dưới. "Hôm nay" là `modifiedDate.Date` giờ VN, cùng mốc với `Project.Expire` (chỉ hết hạn khi `EndedAt.Date` < hôm nay), nên
gia hạn tới đúng hôm nay thì sáng mai mới hết.

| Dự án đang | Body gửi | Kết quả |
|---|---|---|
| `DOING` | `status = 5` | `342` "Trạng thái Hết hạn do hệ thống đặt khi qua ngày kết thúc, không chọn tay"; không lưu, không audit |
| `DOING` | `EndedAt` về trước hôm nay | Lưu, giữ `DOING`; job 07:00 hôm sau chuyển `EXPIRED` như cũ |
| `EXPIRED` | `EndedAt` trống hoặc từ hôm nay trở đi, `status` 2 hoặc 5 | Lưu, tự về `DOING` (audit thấy `Status` 5 → 2) |
| `EXPIRED` | `EndedAt` vẫn trước hôm nay, `status = 2` | `342` "Ngày kết thúc đã qua. Dời ngày kết thúc từ hôm nay trở đi để gia hạn dự án"; không lưu |
| `EXPIRED` | `EndedAt` vẫn trước hôm nay, `status = 5` | Lưu bình thường (tên, ngày bắt đầu, metadata), vẫn `EXPIRED` |
| `ACCEPTING` / `DONE` | status khác trạng thái hiện tại | `342` "Dùng chức năng nghiệm thu" ([22](22-nghiem-thu-du-an.md)); Task 70 chặn cả đi ra khỏi `DONE` (trước đó `DONE` gửi 2 / 5 vẫn lưu được) |

Mã `342` dùng lại với câu mới; route, DTO, `ProjectDeadlineJob`, `Project.Expire` không đổi. Body sửa vẫn gửi `status` (FE gửi lại
trạng thái hiện tại của dự án).

Đã kiểm 2026-11-27 bằng API thật trên DB dev chung (`ZZTEST68`, đã dọn; tắt `DeadlineMailEnabled` trước khi gọi `deadline-check/run`):
22/22 ca, gồm đặt `EXPIRED` bằng job rồi gia hạn đủ các nhánh trong bảng, job chạy ngay sau khi gia hạn tới hôm nay vẫn giữ `DOING`.

## Gửi mail và chống trùng

- Người nhận: user có email thuộc vai trò SuperAdmin hoặc Admin (`IUserQuery.GetEmailsByRoleNamesAsync`, bỏ trùng).
- Mỗi bộ `(dự án, loại mail, EndedAt, email)` chỉ gửi **một lần thành công**: bảng `construction.ProjectDeadlineNotification`
  có unique index lọc `[IsSuccess] = 1 AND [Deleted] = 0` (cờ xoá mềm chỉ bật khi Admin xoá dự án kèm dữ liệu, [25](25-xoa-du-an-danh-sach-cap.md)). Gửi lỗi thì lưu `IsSuccess = 0` + `ErrorMessage`, lần chạy sau gửi lại.
- Gia hạn (đổi `EndedAt`) sinh khoá mới nên được báo lại. Mail sắp hết hạn gửi một lần cho mỗi `EndedAt`, không gửi mỗi ngày.
- Template `Mail/Templates/ProjectDeadline.html`: HTML thuần, không màu. Tiêu đề `Dự án {tên} sắp hết hạn` / `đã hết hạn`. Loại `3` có template riêng
  `ProjectAcceptingOverdue.html` (số ngày quá hạn, vẫn Đang nghiệm thu, ba cách xử lý) và tiêu đề `Dự án {tên} đã quá hạn khi đang nghiệm thu`;
  `IMailExternal.SendProjectDeadlineAsync` nhận loại mail để chọn template. `FindPaging` tính `deadlineType` bằng SQL riêng nhưng cùng luật `ClassifyDeadline`.
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

Lưu ý: Mail gửi xong mà `SaveChanges` lịch sử lỗi thì lần sau có thể gửi trùng: chấp nhận, vì không giữ transaction khi chờ SMTP.

Lưu ý: Múi giờ `SE Asia Standard Time` là tên trên Windows. Chạy Docker Linux thì cả `GetVietnamTime()` lẫn lịch job lỗi; khi
triển khai phải đổi sang `Asia/Ho_Chi_Minh` ở một chỗ (`BaseService.VietnamTimeZone`).

Lưu ý: DB dev có dự án `EndedAt` đã qua và một admin có email thật: bật app với `BackgroundJob:Enabled = true` là 07:00 job sẽ
chuyển dự án sang `EXPIRED` và gửi mail thật. Máy dev không muốn vậy thì đặt `Enabled = false` hoặc `DeadlineMailEnabled = false`.

> **Tiếp:** [16-dong-goi-cau-hinh.md](16-dong-goi-cau-hinh.md) — cấu hình metadata đóng gói theo TT05: loại gói, schema, trường, bảng mã.
