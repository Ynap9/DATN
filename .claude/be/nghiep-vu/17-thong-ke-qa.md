# Hồ sơ — thống kê QA chi tiết

> **Phần 17/19** · trước: [16-dong-goi-cau-hinh.md](16-dong-goi-cau-hinh.md) · mục lục: [README.md](README.md)

Tổng hợp dữ liệu QA đã lưu (`SubmissionQaLog`, `SubmissionQaLogField`) của **một dự án + một loại văn bản**. Mục đích là để Leader
biết trường nào hay sai, người nhập nào hay sai, và tỉ lệ đúng là bao nhiêu. Đây là đầu vào cho KPI và dashboard. Cả 4 API chỉ đọc:
không transaction, không audit, không đổi schema. Luồng QA tạo ra dữ liệu này nằm ở [13-ho-so-qa.md](13-ho-so-qa.md).

## Các chức năng

| Endpoint (`api/v1/construction/submission/...`) | Quyền | Việc |
|---|---|---|
| `GET qa-stats/summary` | `Function.Construction.Submission.QaStats` | Chỉ số tổng của dự án + loại văn bản trong kỳ, lọc thêm `entryUserId` |
| `GET qa-stats/fields` | `...Submission.QaStats` | Số lần sai và tỉ lệ đúng theo từng trường, lọc thêm `entryUserId` |
| `GET qa-stats/entry-users` | `...Submission.QaStats` | Chỉ số theo từng người nhập, phân trang, `keyword`, `orderBy` |
| `GET {id:int}/timeline` | `Function.Construction.Submission.View` | Một trục thời gian gộp các lần kiểm tra và QA của một văn bản |

Ba API thống kê nhận `projectId`, `documentTypeId` (bắt buộc), `fromDate`, `toDate`. Key quyền `QaStats` là key mới.
`SeedRolePermission` tự cấp key này cho `Admin`; các vai trò khác phải gán tay. Không lọc theo người đang đăng nhập: Data / QA xem
số của chính mình để làm cùng dashboard.

## Phạm vi và đơn vị đếm

- **Khoảng ngày:** tính theo ngày giờ VN, lấy trọn ngày `toDate`. Bỏ trống `toDate` thì lấy hôm nay; bỏ trống `fromDate` thì lấy
  30 ngày tính tới `toDate`. Khoảng tối đa 366 ngày. API `summary` trả lại `fromDate` / `toDate` sau khi đã chuẩn hoá.
- Chỉ tính QA của văn bản **chưa xoá**: câu đọc join `Submission`, global query filter tự loại văn bản đã xoá.
- **Một văn bản = một lượt trong kỳ**, kể cả khi QA chấm nhiều lần (chốt với người dùng ngày 2026-10-26, khác spec gốc "mỗi lần chấm
  một lượt"). Kết quả, trường sai và người nhập của lượt lấy từ **lần chấm cuối trong kỳ** (`Id` lớn nhất).
  Bản sửa spec ngày 2026-10-26 lại ghi "mỗi lần chấm một lượt", nhưng người dùng giữ nguyên quyết định trên.
  ⚠️ Khi có chấm mẫu (Task 18C), văn bản đạt nhờ phiếu QA đạt không có `SubmissionQaLog`, nên không vào `ReviewCount` hay số ô. Đây là
  chủ ý: tỉ lệ đúng chỉ đo trên văn bản đã được QA xem. Số liệu phiếu sẽ do 18C bổ sung vào `summary`.
- **Người nhập** của một lần chấm = `UserId` của `SubmissionLog` mới nhất của cùng văn bản có `CreatedDate <=` lúc chấm (bằng nhau
  thì lấy `Id` lớn nhất). Không có dòng nào thì là `null`, hiển thị "Không xác định". Không dùng `Submission.ModifiedBy`, vì
  `ReviewQa` cũng ghi đè cột đó. Luật này chỉ viết một chỗ, `SubmissionQuery.QueryQaLogEntryUsers` (trả `IQueryable` cặp
  `QaLogId`, `EntryUserId`), dùng chung cho 3 API thống kê và `timeline`. Task 18C (giao QA không trùng người nhập) dùng lại hàm
  này, **đừng** chép lại điều kiện ở chỗ khác.
- **Lượt đầu tiên** = `SubmissionQaLog` có `Id` nhỏ nhất **cả lịch sử** của văn bản. Văn bản chỉ tính vào `FirstReviewCount` khi
  lần chấm đầu tiên đó nằm trong kỳ, và được gán cho người nhập của **lần chấm đầu tiên đó** (không phải của lượt cuối).

## Chỉ số

| Chỉ số | Định nghĩa |
|---|---|
| `ReviewCount` | Số lượt (= số văn bản có QA trong kỳ) |
| `PassedCount` / `FailedCount` | Số lượt có kết quả cuối là Đạt / Không đạt |
| `ReviewedDocumentCount` | Số văn bản khác nhau có QA trong kỳ (bằng `ReviewCount`) |
| `FirstReviewCount` / `FirstPassCount` | Số văn bản có lần chấm đầu tiên trong kỳ / trong số đó, lần đầu là Đạt |
| `FirstPassRate` | `FirstPassCount / FirstReviewCount` — "đạt ngay lần đầu" |
| `ChargeableFieldCount` | Số trường **tính tiền** (`IsChargeable`) đang cấu hình cho dự án + loại văn bản (chỉ có ở `summary`) |
| `WrongFieldCount` | Số trường sai **có tính tiền** của các lượt |
| `ReviewedCellCount` | `ReviewCount × ChargeableFieldCount` |
| `FieldAccuracyRate` | `(ReviewedCellCount − WrongFieldCount) / ReviewedCellCount` — "tỉ lệ đúng theo ô" |
| `AvgReturnPerDocument` | `FailedCount / ReviewedDocumentCount`, làm tròn 2 số, **không** nhân 100 |

- Tỉ lệ trả dạng **phần trăm, làm tròn 2 số** (`Math.Round(x * 100m / y, 2)`, giống `CheckedPercent`). Mẫu số `0` thì trả `0`, kèm
  các số đếm `0` để FE hiện "—".
- Mẫu số chỉ gồm trường tính tiền (chốt 2026-10-26, spec gốc dùng mọi trường cấu hình). Tử số vì vậy cũng chỉ đếm trường sai có tính
  tiền, nên tỉ lệ luôn nằm trong `0`–`100`. Sai ở trường không tính tiền vẫn hiện ở API `fields`, nhưng không trừ vào tỉ lệ đúng theo ô.

## Theo trường — `qa-stats/fields`

- Mỗi trường có `documentFieldId`, `name`, `field`, `isConfigured`, `isChargeable`, `reviewCount` (bằng nhau cho mọi trường),
  `wrongCount`, `wrongRate = wrongCount / reviewCount`, `accuracyRate = (reviewCount − wrongCount) / reviewCount`.
- Danh sách gồm **mọi** trường đang cấu hình (kể cả 0 lần sai, kể cả trường không tính tiền), cộng các trường **không còn** cấu hình mà
  vẫn có lần sai trong kỳ (`isConfigured = false`).
- Tên trường đọc cả trường đã xoá mềm (`IgnoreQueryFilters`). Chỉ trường **bị xoá** mới thêm hậu tố ` (đã xoá)`; trường chỉ bị bỏ khỏi
  cấu hình dự án thì giữ tên gốc.
- Sắp `wrongCount` giảm dần, rồi `OrderListView`, rồi `Id`.

## Theo người nhập — `qa-stats/entry-users`

- Mỗi người: `userId`, `userName`, `fullName` và đủ các chỉ số ở trên (trừ `fromDate`, `toDate`, `chargeableFieldCount`).
- Dòng người nhập `null` có `userName = "Không xác định"`. User đã bị xoá cứng (log còn `UserId` nhưng bảng `Users` không còn) vẫn là
  **dòng riêng** theo `UserId`, `userName` / `fullName` là `null`.
- `keyword` lọc `userName` / `fullName`, không phân biệt hoa thường. `totalItems` là số người sau khi lọc.
- `orderBy`: `review` (mặc định, `ReviewCount` giảm dần) hoặc `accuracy` (`FieldAccuracyRate` tăng dần, người sai nhiều lên đầu).
  Hoà thì sắp theo `userName`. `pageSize` từ `1` đến `200`.

## Dòng thời gian — `{id}/timeline`

- Gộp `SubmissionLog` (`eventType = "CHECK"`) và `SubmissionQaLog` (`"QA"`) của một văn bản, cũ trước mới sau. Cùng `createdDate`
  thì `CHECK` đứng trước. Mỗi sự kiện là **một lần chấm** (không gộp lượt như các API thống kê). Tối đa 500 sự kiện, không phân trang.
- `CHECK` có: `oldStatusCheck`, `newStatusCheck`, `note`, `changedDataCount`, `description`.
- `QA` có: `newStatusQa`, `reason`, `wrongFields` `[{ documentFieldId, name }]` và người nhập của lần chấm đó (`entryUserId`,
  `entryUserName`). Trường không thuộc loại sự kiện thì là `null`.
- Người làm (`userId`, `userName`, `fullName`) có ở mọi sự kiện.
- Thêm loại sự kiện mới (ví dụ phiếu QA của 18C): thêm một nguồn trả `GetListSubmissionTimelineDto` với `eventType` mới rồi `Concat`
  vào chỗ ghép trong `FindTimeline`. Việc sắp xếp và cắt 500 sự kiện chỉ nằm ở chỗ ghép đó.

## Lỗi

| Lỗi | Mã |
|---|---|
| Dự án / loại văn bản không tồn tại, hoặc loại văn bản chưa gắn dự án | `303` / `301` / `312` |
| `fromDate > toDate` — "Từ ngày không được sau đến ngày" | `400` |
| Khoảng ngày quá 366 ngày — "Khoảng thời gian tối đa 366 ngày" | `400` |
| `orderBy` khác `review` / `accuracy`; `pageSize` ngoài `1`–`200` | `400` |
| Timeline: văn bản không tồn tại hoặc đã xoá | `305` |

## Cách tính trong code

`SubmissionQuery.FindQaReviewsAsync` dùng chung cho 3 API thống kê. Câu 1 nạp mọi lần chấm trong kỳ, mỗi dòng kèm `EntryUserId`
(subquery `TOP 1` trên index `SubmissionLog (SubmissionId, CreatedDate)`) và `IsFirstReview` (`NOT EXISTS` lần chấm có `Id` nhỏ hơn).
Câu 2 nạp các trường sai bằng `JOIN` với cùng điều kiện, không dùng `Contains` danh sách id vì SQL Server giới hạn 2100 tham số. Sau
đó gộp theo văn bản và tính chỉ số trong bộ nhớ (`BuildQaStats`).
⚠️ Đừng `GroupBy` theo `EntryUserId` trong SQL: đó là cột tính từ subquery, EF dịch lỗi hoặc sinh SQL chậm.

## Đã kiểm thực tế

Ngày 2026-10-26, gọi API thật trên DB dev chung, dữ liệu dựng bằng SQL gắn `ZZTEST` (xoá sạch sau khi kiểm), script
`test_task18.py` trong scratchpad — 50/50 đạt. Dữ liệu: F1–F3 tính tiền, F4 không; d1, d2 nhập, q1 QA.
V1: d1 kiểm tra → Không đạt (F1, F2) → d1 kiểm tra lại → Đạt. V2: d2 kiểm tra → Đạt. V3: d1 kiểm tra → Không đạt (F1) → d2 kiểm tra lại → Không đạt (F3).

- `summary` cả kỳ: 3 lượt, Đạt 2, Không đạt 1, đạt lần đầu 1/3 = `33.33`, sai 1, 9 ô, `88.89`, `0.33`. Kỳ chỉ chứa lần chấm cuối của V1 →
  1 lượt, `firstReviewCount = 0`. V3 chấm 2 lần trong kỳ → 1 lượt, lấy lần cuối (F3).
- `fields`: F3 đứng đầu (`33.33` / `66.67`), F1 không bị tính (sai ở lần chấm không phải cuối), F4 có mặt với `isChargeable = false`.
  `entryUserId = d2` → `reviewCount = 2`, F3 `50`.
- `entry-users`: d1 có 1 lượt + 2 lần chấm đầu (0 Đạt); d2 có 2 lượt (lượt cuối của V3 tính cho d2), `83.33`; `accuracy`, `keyword`,
  trang 2 đều đúng.
- `timeline` V3: CHECK d1 → QA (F1, người nhập d1) → CHECK d2 → QA (F3, người nhập d2).
- F4 bị đánh sai → `wrongFieldCount` không đổi, `fields` có F4 = 1. QA không có lần kiểm tra nào trước đó → "Không xác định" ở cả
  `timeline` lẫn `entry-users`. Bỏ cấu hình F3 → `isConfigured = false`, không có hậu tố, `chargeableFieldCount = 2`. Xoá trường F3 →
  có ` (đã xoá)`. Xoá mềm V2 → V2 biến mất khỏi mọi số.
- Lỗi `400` (khoảng ngày, 400 ngày, `orderBy`, `pageSize` 0 / 201), `303` / `301` / `312`, `305`. User chỉ có `View` → `403` ở 3 API
  thống kê và gọi được `timeline`. Không token → `401`. Khoảng ngày không có dữ liệu → mọi số `0`.

## Hiệu năng

Đo cùng ngày: 50.005 lần chấm QA (`ZZPERF`, đã xoá), API chạy build Debug, trung vị 7 lần gọi:

| API | Đo | Mục tiêu |
|---|---|---|
| `summary` / `summary?entryUserId` | 6,4 s / 7,0 s | < 1 s |
| `fields` / `entry-users` | 5,4 s / 7,2 s | < 1 s |
| Mốc `GET count` cùng máy | 442 ms | |

Chưa đạt mục tiêu, nhưng **không phải do SQL**. Câu nạp lần chấm chạy trên server mất ~130 ms (đo bằng `SYSDATETIME` trong `sqlcmd`).
Phần còn lại là thời gian kéo ~48.000 dòng qua tunnel `cloudflared` tới DB dev: riêng việc kéo 50.000 dòng `Id, SubmissionId, UserId`
(1,2 MB) bằng `sqlcmd` đã mất ~3,5 s. API và DB đặt cùng mạng LAN thì phải đo lại.
⚠️ Nếu môi trường thật vẫn chậm, hướng sửa là gộp theo văn bản ngay trong SQL để giảm số dòng trả về, **đừng** thêm index
`SubmissionQaLog (ProjectId, DocumentTypeId, CreatedDate)` khi chưa đo: phía server không phải nút cổ chai. Thêm index là đổi schema, phải hỏi trước.

## Giới hạn

⚠️ Mẫu số dùng cấu hình và cờ `IsChargeable` **hiện tại**. Đổi cấu hình hoặc cờ tính tiền giữa kỳ thì số của cả quá khứ đổi theo
(`ChargeableFieldCount` không ghi lại theo thời điểm chấm).

⚠️ Thống kê theo lượt là lần chấm cuối **trong kỳ**: đổi khoảng ngày có thể đổi kết quả của cùng một văn bản (ví dụ kỳ chỉ chứa lần
Không đạt đầu tiên của V1).

⚠️ Toàn bộ lần chấm trong kỳ được nạp vào bộ nhớ rồi mới gộp. Kỳ 366 ngày trên dự án rất lớn sẽ tốn RAM theo số lần chấm.

⚠️ Dữ liệu test dựng tay phải dời `CreatedDate` của **cả** `SubmissionLog` lẫn `SubmissionQaLog`. Chỉ dời lần chấm QA về trước lần
kiểm tra thì người nhập thành "Không xác định".

> **Tiếp:** [18-qa-cham-mau.md](18-qa-cham-mau.md) — chấm mẫu QA: chế độ chấm `FULL` / `SAMPLE`, cấu hình mẫu theo dự án, cỡ mẫu và ngưỡng.
