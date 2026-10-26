# Hồ sơ — xoá và xuất Excel (Construction)

> **Phần 14/18** · trước: [13-ho-so-qa.md](13-ho-so-qa.md) · mục lục: [README.md](README.md)

Hai thao tác trên màn danh sách hồ sơ của một dự án + loại văn bản: chọn hồ sơ để xoá, hoặc tải danh sách theo bộ lọc
hiện tại về Excel. Cùng `SubmissionController` và cùng bộ lọc với [12-ho-so-kiem-tra.md](12-ho-so-kiem-tra.md#bộ-lọc-chung).

## Các chức năng

| Endpoint (`api/v1/construction/submission/...`) | Quyền | Việc |
|---|---|---|
| `DELETE` (gốc, `ids` trong body) | `Function.Construction.Submission.Delete` | Xoá mềm nhiều hồ sơ, tối đa 500 mỗi lần |
| `GET export-excel` + bộ lọc chung | `Function.Construction.Submission.Export` | Tải `.xlsx` theo bộ lọc |
| `POST export-excel/job` + bộ lọc chung | `Function.Construction.Submission.Export` | Xếp job xuất Excel chạy nền, trả `jobId` |
| `GET export-excel/job/{jobId}` | `Function.Construction.Submission.Export` | Trạng thái job + link tải khi xong |

## Xoá hồ sơ

Body `DeleteSubmissionDto`: `{ projectId, documentTypeId, ids: [] }`. Mọi hồ sơ trong `ids` phải thuộc đúng dự án và loại văn bản đó.

| Lỗi | Mã |
|---|---|
| Dự án không tồn tại / đã xoá | `303` |
| Dự án `DONE` (trừ SuperAdmin) | `316` |
| Dự án hết hạn `EXPIRED` (trừ SuperAdmin) | `323` |
| Không chọn hồ sơ, hoặc trên 500 Id (Id trùng được bỏ trước khi đếm) | `400` |
| Id không tồn tại hoặc đã xoá; liệt kê các Id thiếu | `305` |
| Hồ sơ thuộc dự án / loại văn bản khác | `319` |
| Hồ sơ đã kiểm tra (`StatusCheck = 1`), trừ SuperAdmin; liệt kê Id | `325` |
| Import cùng loại văn bản đang giữ khoá quá 5 giây | `310` |

Luật:

- Hồ sơ **chưa kiểm tra** (`0`) hoặc **trả lại** (`2`) xoá được. Hồ sơ **đã kiểm tra** (`1`), kể cả đã QA Đạt, chỉ SuperAdmin xoá được.
  QA Không đạt luôn đưa hồ sơ về `RETURNED`, nên hồ sơ bị QA trả lại vẫn xoá được.
- Một lần xoá là một transaction. Trước khi đọc hồ sơ, lệnh khoá dòng `ProjectDocumentType` như import
  ([11-ho-so-import.md](11-ho-so-import.md#ghi-dữ-liệu--một-transaction)), nên xoá và import cùng loại văn bản không chạy song song.
- Xoá mềm hồ sơ (`Deleted`, `DeletedBy`, `DeletedDate`) và toàn bộ `SubmissionData` của chúng. `SubmissionLog` và
  `SubmissionQaLog` giữ nguyên.
- Ghi `AuditLog` action `Xóa hồ sơ`, type `Delete`, mô tả `Xóa N hồ sơ của dự án …, loại văn bản Id = …`. Giá trị cũ là danh sách
  `Id, FileName, ProfileRange, Box, StatusCheck, StatusQa` của từng hồ sơ.
- Trả `{ deletedSubmissionCount, deletedDataCount }`.

Sau khi xoá, import lại cùng khoá sẽ tạo hồ sơ mới: snapshot lúc import và unique index (`[Deleted] = 0`) đều bỏ qua bản đã xoá.

⚠️ Xoá **không** xoá file trên MinIO (`FileUrlS3Object`): object vẫn nằm trong kho. Muốn dọn kho thì thêm bước xoá object riêng, và
hỏi trước vì không khôi phục được. Đừng gắn việc này vào `Delete` khi chưa được duyệt.

⚠️ Lịch sử kiểm tra và QA của hồ sơ đã xoá vẫn nằm trong DB. Lịch sử toàn dự án (`logs`) vẫn hiện tên file nhờ `IgnoreQueryFilters`,
còn các API theo `{id}` của hồ sơ đã xoá trả `305`.

## Xuất Excel

`GET export-excel` nhận bộ lọc chung (`projectId`, `documentTypeId`, `statusCheck`, `statusQa`, `keyword`, hai cặp trường) và
**không** phân trang: xuất mọi hồ sơ khớp bộ lọc, sắp theo `Id` (thứ tự import). Dùng chung `SubmissionQuery.ApplyFilter` với
danh sách, nên số dòng bằng `count.total` cùng bộ lọc.

- Tên file `{Tên dự án}_{Tên loại văn bản}_{yyyyMMddHHmm}.xlsx`. Sheet mang tên loại văn bản.
- Hàng 1 là tiêu đề, in đậm và cố định. Độ rộng cột tự chỉnh theo 200 dòng đầu, kẹp trong khoảng 8–60.
- Cột cố định: `STT`, `Tên file`, `Hồ sơ`, `Hộp`, `Đường dẫn` (link S3), `Trạng thái kiểm tra`, `Người kiểm tra`,
  `Thời gian kiểm tra`, `Trạng thái QA`, `Lý do QA`, `Người sửa cuối`, `Ngày sửa cuối`.
- Sau đó là các trường có `IsShowTableView = true` của cấu hình dự án + loại văn bản, theo `OrderTableView`. Ô chưa có dữ liệu để trống.
- Trạng thái ghi bằng chữ: `Chưa kiểm tra` / `Đã kiểm tra` / `Trả lại`; `Chưa QA` / `Đạt` / `Không đạt`. Ngày theo `dd/MM/yyyy HH:mm:ss`.
  Người kiểm tra và người sửa ghi tên đăng nhập, lấy từ lần kiểm tra gần nhất và `ModifiedBy`.
- Xuất được cả khi dự án `DONE`. Không đổi trạng thái, không ghi audit.

| Lỗi | Mã |
|---|---|
| Dự án / loại văn bản không tồn tại, hoặc loại văn bản chưa gắn dự án | `303` / `301` / `312` |
| Số hồ sơ vượt `SubmissionConst.EXPORT_MAX_ROWS` (1.048.575): hãy lọc bớt | `322` |

Lỗi nghiệp vụ không trả file: controller trả `ApiResponse` JSON với HTTP 200. FE phải kiểm `Content-Type` của response như
`proxy-file` ([11-ho-so-import.md](11-ho-so-import.md#xem-file--proxy)).

⚠️ Toàn bộ dòng và các ô trường được nạp vào bộ nhớ, dựng workbook ClosedXML trong bộ nhớ rồi mới trả về; không stream. Số đo ở
[mục chạy nền](#đã-kiểm-thực-tế-xuất-nền).

⚠️ Bẫy plan cache (đo 2026-10-25, chưa sửa): câu đọc `SubmissionData` trong `FindExportTable` có `SubmissionId IN (subquery Submission)`.
Plan dựng lần đầu cho dự án nhỏ (15 hồ sơ) là nested loops; dùng lại cho 20.000 hồ sơ × 30 trường thì chạy **hơn 10 phút** (quét
600.000 × 20.000), chỉ dừng ở `CommandTimeout = 600`. Xoá plan thì còn ~10 s. Thử ở SQL với plan dự án nhỏ: câu gốc > 60 s, bỏ điều kiện
`SubmissionId IN` (lọc theo danh sách `submissions` đã nạp, trong RAM) còn 2,5 s. Đề xuất sửa theo hướng đó, chờ người dùng duyệt vì đổi
`FindExportTable`; đừng "sửa" bằng `DBCC FREEPROCCACHE`.

⚠️ Mọi ô được ghi dạng chuỗi (`SetValue(string)`), kể cả `STT`. Excel sẽ coi số là văn bản và không tính được tổng. Muốn ô số
thì đổi `ExcelExternal` và kiểu dữ liệu của hàng, đừng xử lý ở FE.

## Xuất Excel chạy nền

Cùng bộ lọc, cùng nội dung file với `GET export-excel`, nhưng chạy trong job Hangfire `SubmissionExportJob` (queue `export`,
[05](../architecture/05-infrastructure-external.md#backgroundjob--hangfire)): request không giữ HTTP lâu, tối đa `ExportWorkerCount` (2) workbook
dựng cùng lúc. Không làm xuất nhanh hơn. `GET export-excel` cũ giữ song song để FE chuyển dần.

1. FE gọi `POST export-excel/job` + bộ lọc. BE kiểm `303` / `301` / `312` (không tạo job), chốt tên file `{Tên dự án}_{Tên loại văn bản}_{yyyyMMddHHmm}.xlsx`
   theo giờ VN lúc bấm, xếp job, trả `{ jobId, status: 0, fileName }`. Không kiểm `322` lúc này: job tự báo.
2. FE gọi `GET export-excel/job/{jobId}` mỗi 2–3 giây tới khi `status` là `2` hoặc `3`.
3. `status = 2` → `downloadUrl` (link ký sẵn MinIO 15 phút, `attachment` kèm tên file). Link hết hạn thì gọi lại API 2 để lấy link mới.

| `status` | Nghĩa | Trạng thái Hangfire |
|---|---|---|
| `0` | Đang chờ | Enqueued, Scheduled (đợi retry), Awaiting |
| `1` | Đang chạy | Processing |
| `2` | Xong, có `downloadUrl` | Succeeded |
| `3` | Lỗi, có `errorMessage` | Failed, Deleted |

| Lỗi | Mã |
|---|---|
| jobId không có, đã hết hạn, không phải số, của người khác, hoặc không phải job xuất | `326` |
| Không có server Hangfire nào nghe queue `export` còn heartbeat trong 90 giây | `211` |

- Lỗi nghiệp vụ trong job (`322`, dự án bị xoá sau khi bấm…) → Failed ngay, **không** retry, `errorMessage` là câu của mã lỗi. Lỗi khác
  (DB, MinIO) → retry 2 lần rồi `3` với câu chung `Xuất Excel thất bại, hãy thử lại` (không lộ lỗi hệ thống).
- Người tạo, loại job, tên file, object key lưu ở tham số job Hangfire; không thêm bảng, không ghi audit.
- File: `construction/submission-export/{yyyyMMdd}/{guid}.xlsx`, private. `SubmissionExportCleanupJob` chạy `BackgroundJob:Jobs:SubmissionExportCleanup`
  (`0 2 * * *` giờ VN), xoá thư mục ngày cách hôm nay 2–8 ngày (giữ hôm nay + hôm qua, quét lại 7 ngày nếu job lỡ chạy).
- Job Succeeded / Deleted Hangfire tự xoá sau 24 giờ → API 2 trả `326`. ⚠️ Job **Failed thì Hangfire không tự xoá**, nằm mãi ở dashboard.

⚠️ Mọi app trỏ cùng DB là một server Hangfire. Khi đo, máy đồng đội chạy code cũ (không có `SubmissionExportJob`) đã lấy job đang chờ retry,
báo `Could not load type` và đánh retry theo mặc định 10 lần của nó. Cả nhóm phải chạy cùng bản code, hoặc máy dev đặt `BackgroundJob:Enabled = false`.

## Đã kiểm thực tế (xuất nền)

Ngày 2026-10-25, Release chạy `dotnet` trên máy dev (không Docker), DB dev đi qua tunnel `cloudflared` (~130 ms mỗi câu SQL), MinIO
`minio-api.yna.io.vn`. Script trong scratchpad: `test_export.py`, `test_fail.py`, `test_cleanup.py`, dữ liệu `test-data-ho-so.sql` + 20.000 hồ sơ × 30 trường.

- 30/30 chức năng: `serverCount` gồm 2 server của app (`default`, `export`); job đi `0 → 1 → 2`; file tải về giống hệt `GET export-excel`
  (so từng phần XML trong `.xlsx`); link `X-Amz-Expires=900`; `303` / `301` / `312` không tạo job; `abc`, `999999`, user khác, loại job khác → `326`;
  `viewer` → `403`, không token → `401`; `GET export-excel` cũ vẫn chạy; swagger có 2 route; lịch dọn file đăng ký một lần `0 2 * * *`.
- Hạ tạm `EXPORT_MAX_ROWS = 10` → `3` + câu `322`, state chỉ một lần Failed. S3 sai URL → 3 lần chạy (đều trên server `export`) rồi `3` + câu chung.
  Tắt server nền → `211`. Restart app khi 2 job đang chạy + 3 job chờ → cả 5 Succeeded (2 job bị cắt chạy lại sau ~5 phút).
- Bấm 5 lần liền với 20.000 hồ sơ: tối đa 2 Processing, 3 Enqueued, cả 5 Succeeded. Dọn file: còn thư mục 0, 1 và 9 ngày trước, mất 2, 3, 8.
- Chưa đợi đủ 15 phút để thử link hết hạn: chỉ kiểm tham số link.
- `DELETE` (xoá hồ sơ) chưa gọi thật trên DB có dữ liệu, chưa kiểm luồng bị khoá bởi import (`310`).

| Đo (20.000 hồ sơ × 30 trường, file 5,06 MB) | Kết quả |
|---|---|
| `GET export-excel` cũ, một lượt | 9,4–13,3 s |
| Job một mình, Enqueued → Succeeded (gồm upload MinIO) | 18,6 s |
| `POST export-excel/job` (dự án 15 hồ sơ, 10 lượt) | trung vị 1.440 ms: ~11 câu SQL, riêng Hangfire tạo job ~0,9 s |
| `GET export-excel/job/{jobId}` (10 lượt) | trung vị 277 ms; mốc `GET count` cùng máy 351 ms |
| RAM đỉnh khi 5 lượt cùng lúc | nền 782 MB; `GET` cũ 1.177 MB, lượt chậm nhất 105,6 s |

Mục tiêu 300 ms / 100 ms của spec không đạt **vì độ trễ tunnel tới DB**, không phải vì code: số câu SQL mới là thước đo. `FindJob` đọc
tham số từ `JobData.ParametersSnapshot` (1 câu, thêm 1 câu khi Failed) thay vì 6 câu; trước khi đổi API 2 mất 1.238 ms.

> **Tiếp:** [15-han-du-an.md](15-han-du-an.md) — hạn dự án, job kiểm tra hạn và mail báo.
