# Xuất hồ sơ ra Google Sheet

> **Phần 29/29** · trước: [28-van-ban-dang-lam.md](28-van-ban-dang-lam.md) · mục lục: [README.md](README.md)

Task 56A (2026-11-24): tab Xuất của màn Nhập / Xuất ghi bảng hồ sơ vào **một tab có sẵn** của Google Sheet, chạy nền Hangfire đúng
khuôn [xuất Excel chạy nền](14-ho-so-xoa-xuat.md#xuất-excel-chạy-nền). Cùng bộ lọc (`SubmissionFilterDto`), cùng cột và tiêu đề như file
Excel vì cùng gọi `ISubmissionQuery.FindExportTable`. Không migration, không key quyền, không mã lỗi mới; API xuất Excel giữ nguyên.

## API

| Method | Route (`api/construction/submission/...`) | Trả |
|---|---|---|
| GET | `export-sheet/sheet-names?projectId=&documentTypeId=&url=` | Tên các tab, đúng thứ tự trong file |
| POST | `export-sheet/job` — mọi tham số `[FromQuery]`: bộ lọc + `columns` như `export-excel/job`, thêm `url`, `sheetName`, `startRow` | `{ jobId, status: 0, errorMessage: null }` |
| GET | `export-sheet/job/{jobId}` | `{ jobId, status, errorMessage }`; `status` 0 chờ, 1 chạy, 2 xong, 3 lỗi (có `errorMessage`) |

Cả ba: `Function.Construction.Submission.Export` + Admin / Leader của dự án (`337`) + loại văn bản đọc được, như xuất Excel.
API lấy tên tab cũng kiểm đủ vì nó dùng tài khoản Gmail của hệ thống. FE tự giữ link, tên tab, dòng bắt đầu của từng lượt.

## Luật

- **Tài khoản**: Gmail đã kết nối ở màn Cấu hình ([08](08-cau-hinh-du-an.md)), lấy qua `IProjectConfigService.GetSpreadsheetAccount()` —
  cùng tài khoản tạo / đổi tên Sheet dự án, không dùng service account. Thiếu một trong 4 cấu hình Google (kể cả `GoogleFolderId`
  dù xuất không cần thư mục) → `304`.
- **Link**: Sheet bất kỳ (FE điền sẵn link Sheet của dự án), **trừ Sheet đang gắn với dự án khác chưa xoá** → `202` câu "Google Sheet
  thuộc dự án khác, không dùng được cho dự án này". Vì sao: mọi Sheet dự án do cùng một Gmail tạo, không chặn thì Leader dự án A
  ghi đè được Sheet nguồn import của dự án B. Luật này người dùng chốt thêm ngày 2026-11-24, ngoài bảng CASE của spec.
- **Cách ghi**: xoá giá trị từ dòng `startRow` tới dòng cuối của tab, dòng `startRow` là tiêu đề, dữ liệu từ dòng kế tiếp; các dòng phía
  trên giữ nguyên. Ghi RAW nên "001", ngày dạng chữ giữ nguyên như file Excel. Tab thiếu dòng / cột thì tự thêm ([05](../architecture/05-infrastructure-external.md)).
- **`startRow`**: số nguyên ≥ 1 (FE mặc định 1); < 1 hoặc thiếu → `400` "Dòng bắt đầu phải từ 1 trở lên".
- **Kiểm trước khi tạo job** (trả lỗi ngay, không tạo job), theo thứ tự: quyền, bộ lọc, mã cột, `startRow`, Sheet dự án khác, tài khoản
  Google, đọc tên tab, `sheetName` có trong file, server queue `export`.
- **Job** `SubmissionExportJob.ExecuteSheetAsync` → `ExportSheetFromJob` (không kiểm user, đã kiểm lúc tạo). Lỗi nghiệp vụ trong job
  (`204` tab bị xoá sau khi bấm, `206`, `207`, `322`…) → `3` ngay với câu của mã, không retry. Lỗi khác → retry 2 lần rồi câu chung
  `Xuất Google Sheet thất bại, hãy thử lại`. Tham số job chỉ có `JobType = submission-export-sheet` + `UserId`; không bảng, không audit.

| Lỗi | Mã |
|---|---|
| Link sai định dạng, hoặc Sheet của dự án khác | `202` |
| Gmail không đọc được file (chưa chia sẻ, không tồn tại) khi lấy tên tab | `203` |
| `sheetName` không có trong file | `204` |
| Ghi thất bại trong job (thiếu quyền Chỉnh sửa…) | `206` |
| Refresh token Google hết hạn / bị thu hồi | `207` |
| Không có server Hangfire nghe queue `export` | `211` |
| Thiếu cấu hình Google / chưa kết nối | `304` |
| `jobId` không có, hết hạn, của người khác, hoặc là job xuất Excel (và ngược lại) | `326` — câu dùng chung "Không tìm thấy yêu cầu xuất dữ liệu…" |

⚠️ Xuất vào Sheet đồng bộ của **chính** dự án không bị chặn (đã chốt): xuất từ dòng 1 thay dữ liệu nguồn import bằng bảng xuất, và không
khoá chung với import nên import Google chạy cùng lúc có thể đọc phải bảng đang ghi dở. Hai job ghi cùng một tab cũng không khoá nhau.

⚠️ Ghi không nguyên tử, chia lô 5.000 dòng: lỗi giữa chừng để lại tab đã xoá mà mới ghi một phần; chỉ xoá giá trị, định dạng ô cũ còn.
Chưa xử lý giới hạn 10 triệu ô của Google — lỗi Google trả về thành `206`.

⚠️ Chặn dự án khác so `UrlGoogleSheetSync` chứa `spreadsheetId` (id Google dài cố định nên không nhầm); Sheet của dự án **đã xoá** không
bị chặn. Máy đồng đội chạy code cũ cùng DB vẫn có thể nhặt job (bẫy ở [14](14-ho-so-xoa-xuat.md#xuất-excel-chạy-nền)).

## Đã kiểm thực tế

Ngày 2026-11-24, Debug chạy `dotnet` trên máy dev cổng 5356, DB dev qua tunnel, Gmail đã kết nối thật; script `test56a.py` trong
scratchpad tạo dự án tạm `ZZTEST56A` có Sheet, 5 văn bản, user Data + Admin tạm, rồi dọn hết (cả file Sheet trên Drive).

- 16 ca chạy thật, đều qua: tên tab đúng thứ tự; link sai `202`; Sheet dự án khác `202` (cả 2 API); id không tồn tại `203` (thay cho file
  chưa chia sẻ — cùng nhánh lỗi Google); xoá tạm `GoogleRefreshToken` → `304` rồi trả lại đúng (so SHA-256); Data → `403` như
  `export-excel/job`; `startRow` 0 / thiếu → `400`; tab không có → `204`, không tạo job.
- `startRow = 3` trên tab có dữ liệu dòng 1–200: dòng 1–2 giữ, dòng 3 tiêu đề, 5 dòng dữ liệu, không còn dòng cũ; "001", "05/11/2026" giữ
  chữ. Lọc `statusCheck = 1` + `columns` → bảng trên Sheet trùng từng ô với `GET export-excel` cùng bộ lọc. Job do server `export` của máy
  dev chạy, `0 → 2`.
- `326`: Admin khác hỏi job, job Excel hỏi ở API Sheet và ngược lại. `GET export-excel` / `POST export-excel/job` vẫn như cũ.
- Không làm được: file chỉ chia sẻ quyền Xem → `206` (không có file như vậy); tắt server queue `export` → `211` (máy đồng đội vẫn nghe
  queue `export` trên DB chung).

> **Tiếp:** hết — quay lại [README.md](README.md).
