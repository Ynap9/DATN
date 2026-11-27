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
- **Link**: Sheet bất kỳ, **trừ Sheet đang gắn làm Sheet đồng bộ của một dự án chưa xoá, kể cả chính dự án đang xuất** → `202` câu
  "Google Sheet đang là Sheet đồng bộ của một dự án, không dùng để xuất". Vì sao: Sheet đồng bộ là nguồn import và mọi Sheet dự án do cùng một
  Gmail tạo; không chặn thì xuất xoá mất nguồn import của dự án đó. Luật chốt thêm ngày 2026-11-24, ngoài bảng CASE của spec; ban đầu chỉ
  chặn dự án khác, ngày 2026-11-24 chặn luôn Sheet chính của dự án (task 56 FE không điền sẵn link Sheet dự án).
- **Cách ghi**: dòng `startRow` là tiêu đề, dữ liệu từ dòng kế tiếp, mỗi dòng đệm ô rỗng tới hết số cột của tab để ghi đè ô cũ bên phải; rồi
  mới xoá giá trị các dòng dư phía dưới tới cuối tab. Kết quả như "xoá từ `startRow` trở xuống rồi ghi" nhưng lỗi giữa chừng không để tab trống; các dòng phía
  trên giữ nguyên. Ghi RAW nên "001", ngày dạng chữ giữ nguyên như file Excel. Tab thiếu dòng / cột thì tự thêm ([05](../architecture/05-infrastructure-external.md)).
- **`startRow`**: số nguyên ≥ 1 (FE mặc định 1); < 1 hoặc thiếu → `400` "Dòng bắt đầu phải từ 1 trở lên".
- **Kiểm trước khi tạo job** (trả lỗi ngay, không tạo job), theo thứ tự: quyền, bộ lọc, mã cột, `startRow`, Sheet đồng bộ của dự án, tài khoản
  Google, đọc tên tab, `sheetName` có trong file, server queue `export`.
- **Job** `SubmissionExportJob.ExecuteSheetAsync` → `ExportSheetFromJob` (không kiểm user, đã kiểm lúc tạo). Lỗi nghiệp vụ trong job
  (`204` tab bị xoá sau khi bấm, `206`, `207`, `322`…) → `3` ngay với câu của mã, không retry. Lỗi tạm thời của Google (429 / 5xx) và lỗi khác → retry 2 lần rồi câu chung
  `Xuất Google Sheet thất bại, hãy thử lại`. Tham số job chỉ có `JobType = submission-export-sheet` + `UserId`; không bảng, không audit.

| Lỗi | Mã |
|---|---|
| Link sai định dạng, hoặc là Sheet đồng bộ của một dự án | `202` |
| Gmail không đọc được file (chưa chia sẻ, không tồn tại) khi lấy tên tab | `203` |
| `sheetName` không có trong file | `204` |
| Ghi thất bại trong job (thiếu quyền Chỉnh sửa…) | `206` |
| Refresh token Google hết hạn / bị thu hồi | `207` |
| Không có server Hangfire nghe queue `export` | `211` |
| Thiếu cấu hình Google / chưa kết nối | `304` |
| `jobId` không có, hết hạn, của người khác, hoặc là job xuất Excel (và ngược lại) | `326` — câu dùng chung "Không tìm thấy yêu cầu xuất dữ liệu…" |

Lưu ý: Hai job xuất ghi cùng một tab không khoá nhau (FE khoá nút khi còn dòng "Đang ghi…" cùng link và tab, nhưng gọi API thẳng vẫn được): kết quả là bản ghi sau cùng đè lên.

Lưu ý: Ghi không nguyên tử, chia lô 5.000 dòng: lỗi giữa chừng để lại tab ghi dở (đầu là dữ liệu mới, sau là dữ liệu cũ) nhưng không bao giờ trống; xuất lại là đúng. Chỉ xoá giá trị, định dạng ô cũ còn.
Chưa xử lý giới hạn 10 triệu ô của Google — lỗi Google trả về thành `206`.

Lưu ý: Chặn Sheet dự án so `UrlGoogleSheetSync` chứa `spreadsheetId` (id Google dài cố định nên không nhầm); Sheet của dự án **đã xoá** không
bị chặn. Máy đồng đội chạy code cũ cùng DB vẫn có thể nhặt job (bẫy ở [14](14-ho-so-xoa-xuat.md#xuất-excel-chạy-nền)).

## Đã kiểm thực tế

Ngày 2026-11-24, Debug chạy `dotnet` trên máy dev cổng 5356, DB dev qua tunnel, Gmail đã kết nối thật; script `test56a.py` trong
scratchpad tạo dự án tạm `ZZTEST56A` có Sheet, 5 văn bản, user Data + Admin tạm, rồi dọn hết (cả file Sheet trên Drive).

- 16 ca chạy thật, đều qua: tên tab đúng thứ tự; link sai `202`; Sheet đồng bộ dự án `202` (cả 2 API); id không tồn tại `203` (thay cho file
  chưa chia sẻ — cùng nhánh lỗi Google); xoá tạm `GoogleRefreshToken` → `304` rồi trả lại đúng (so SHA-256); Data → `403` như
  `export-excel/job`; `startRow` 0 / thiếu → `400`; tab không có → `204`, không tạo job.
- `startRow = 3` trên tab có dữ liệu dòng 1–200: dòng 1–2 giữ, dòng 3 tiêu đề, 5 dòng dữ liệu, không còn dòng cũ; "001", "05/11/2026" giữ
  chữ. Lọc `statusCheck = 1` + `columns` → bảng trên Sheet trùng từng ô với `GET export-excel` cùng bộ lọc. Job do server `export` của máy
  dev chạy, `0 → 2`.
- `326`: Admin khác hỏi job, job Excel hỏi ở API Sheet và ngược lại. `GET export-excel` / `POST export-excel/job` vẫn như cũ.
- Chặng sửa 2026-11-24 (script `test56b.py`, dự án tạm `ZZTEST56B` + một Sheet đích riêng, dọn hết, 15/15 qua): Sheet chính của dự án → `202` cả
  2 API; Sheet dự án khác vẫn `202`; Sheet không thuộc dự án nào đọc được. Xuất từ dòng 3 vào tab 10 cột × 200 dòng cũ: dòng 1–2 giữ đủ
  10 cột, bảng mới 6 cột × 6 dòng, cột G–J và các dòng dư đã trống; xuất lại từ dòng 1 với bộ lọc + cột thêm trùng từng ô file Excel;
  tab 3 dòng × 2 cột tự mở rộng. Chưa chạy thật: retry khi Google trả 429 / 5xx (không gây ra được lỗi này), và Sheet của dự án **đã xoá**
  không bị chặn (API trả `303` vì dự án không còn nên không tới được bước kiểm).
- Không làm được: file chỉ chia sẻ quyền Xem → `206` (không có file như vậy); tắt server queue `export` → `211` (máy đồng đội vẫn nghe
  queue `export` trên DB chung).

> **Tiếp:** hết — quay lại [README.md](README.md).
