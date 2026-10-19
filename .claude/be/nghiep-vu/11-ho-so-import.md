# Hồ sơ — import Google Sheet và xem file (Construction)

> **Phần 11/12** · trước: [10-lo-ky.md](10-lo-ky.md) · mục lục: [README.md](README.md)

Hồ sơ (`construction.Submission`) = một file tài liệu số hoá = một dòng trên Google Sheet, thuộc một dự án và một loại văn bản.
Giá trị từng trường của hồ sơ nằm ở `construction.SubmissionData` (một dòng cho mỗi cặp hồ sơ – trường). Nghiệp vụ lấy từ
`SubmissionService` của hệ thống cũ (ABP), viết lại theo kiến trúc ĐATN và sửa các lỗi của bản cũ.

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Chạy thử / import thêm mới | `POST api/construction/submission/import/append/verify`, `.../import/append` | `Function.Construction.Submission.Import` |
| Chạy thử / import bổ sung ô trống | `POST api/construction/submission/import/blank/verify`, `.../import/blank` | `Function.Construction.Submission.Import` |
| Chạy thử / import ghi đè một trường | `POST api/construction/submission/import/overwrite/verify`, `.../import/overwrite` | `Function.Construction.Submission.Import` |
| Xem file PDF của hồ sơ | `GET api/construction/submission/proxy-file?url=...` | `Function.Construction.Submission.View` |

FE luôn gọi `verify` → hiện số liệu → người dùng xác nhận → gọi import. Verify và import dùng **chung** hàm dựng kế hoạch
(`SubmissionImportPolicy.Build...Plan`), nên số verify bằng đúng số import ghi (nếu sheet và DB không đổi giữa hai lần gọi).

Body chung (`ImportSubmissionDto`): `projectId`, `documentTypeId`, `fileColumnImport` (tên cột tên file, bắt buộc), `linkColumnImport`
(tên cột link file, bắt buộc với append), `profileRange` (tên cột Hồ sơ), `box` (tên cột Hộp). Overwrite thêm `url`, `sheetName`,
`columnImport` (cột lấy dữ liệu), `documentFieldId` (trường bị ghi đè), `overwriteChecked` (mặc định `false`).

## Khoá hồ sơ và cách đọc sheet

- Khoá = `FileName + ProfileRange (Hồ sơ) + Box (Hộp)` trong cùng dự án + loại văn bản; so bằng `SubmissionKeyPolicy.Build`
  (trim, **không phân biệt hoa thường**) ở mọi nơi. DB lưu giá trị đã trim, giữ nguyên hoa thường; unique index có filter chặn trùng ở DB.
- Dòng `1` là header; mọi ô đã trim. Dòng có tên file rỗng bị bỏ qua ở mọi API. `IndexGoogleSheet` (ví dụ `G5`) dùng **số dòng thật**.
- Tên cột so bằng `FieldImportMatchPolicy` (giống API `map-field-import`): không phân biệt hoa thường, gộp khoảng trắng.
- Cột trường lấy theo cấu hình `ProjectDocumentTypeDocumentField` của dự án + loại văn bản: bỏ `FieldImport` rỗng, xếp theo
  `OrderFieldImport` rồi `DocumentFieldId`, mỗi cấu hình lấy cột **đầu tiên** khớp mà chưa bị cấu hình khác dùng; không có cột thì bỏ qua.
  `IndexRow` = thứ tự trong danh sách đã khớp. Import **chỉ đọc** `OrderFieldImport`, không ghi.

## Kiểm tra trước khi ghi (cả verify)

| Lỗi | Mã |
|---|---|
| Dự án không tồn tại / đã xoá | `303` |
| Dự án `Status = DONE` (`Project.EnsureImportable`) | `313` |
| Loại văn bản không tồn tại / chưa gắn vào dự án | `301` / `312` |
| Link sheet sai / không đọc được (chưa share cho service account) / không có sheet | `202` / `203` / `204` |
| Sheet không có dòng dữ liệu | `306` |
| Chưa nhập tên cột tên file, hoặc cột nhập vào không có trên header (liệt kê tên cột) | `307` |
| Hai dòng trùng khoá — liệt kê `Tên file, Hộp, Hồ sơ ... xuất hiện n lần (dòng r1, r2)` | `308` |
| Header X có N cột nhưng chỉ M < N cấu hình trỏ tới X (header không ai cấu hình thì bỏ qua) | `309` |
| Append: chưa nhập cột link / dòng có tên file mà link rỗng (liệt kê số dòng) | `307` / `314` |
| Overwrite: trường không thuộc cấu hình của dự án + loại văn bản | `311` |

## Ba kiểu import

| Kiểu | Đọc sheet | Làm gì | Audit |
|---|---|---|---|
| Append | `Project.UrlGoogleSheetSync` + `GoogleSheetName` | Tạo hồ sơ có khoá **chưa** có + `SubmissionData` cho **mọi** trường đã map (ô rỗng vẫn tạo, để blank điền sau). Hồ sơ đã có: bỏ qua | `Import thêm mới hồ sơ từ Google Sheet`, `Create` |
| Blank | Như append | Hồ sơ **đã có**, ô sheet khác rỗng: data đang rỗng thì điền (`FillIfBlank`); chưa có data cho trường đó thì thêm. Không tạo hồ sơ, không ghi đè ô có dữ liệu; điền cả hồ sơ đã kiểm tra | `Import bổ sung dữ liệu trống từ Google Sheet`, `Update` |
| Overwrite | `url` + `sheetName` trong body | Cột `columnImport` ghi đè trường `documentFieldId` của hồ sơ đã có. Ô rỗng: bỏ qua (không xoá). Giá trị giống cũ: không tính. Hồ sơ `CHECKED` bị bỏ qua khi `overwriteChecked = false` | `Import ghi đè trường dữ liệu từ Google Sheet`, `Update`, `OldData` / `NewData` = danh sách `SubmissionId, FileName, DocumentFieldId, Data` |

Append tạo hồ sơ với `Name = FileName`, `StatusCheck = 0`, **một** link gán cho cả `FileUrlS3Object`, `FileUrlGoogleDrive`,
`FileUrlGstore` (hiện link là S3/MinIO; Google Drive / Gstore làm sau).

Kết quả verify: append `{ totalDataFields (ô có giá trị sẽ thêm), totalAppendRowCount (hồ sơ sẽ tạo), totalFieldCount (trường đã map),
totalSubmission (hồ sơ đang có), totalSubmissionGoogleSheet (số khoá trên sheet) }`; blank `{ totalBlankCount, totalFieldCount }`;
overwrite `{ totalOverWriteCount, totalSkippedChecked }`. Kết quả import: `{ message, totalDataFields }` (server không đo thời gian; muốn đo thì đo từ phía client).

## Ghi dữ liệu — một transaction

1. Ngoài transaction: kiểm như bảng trên; đọc sheet **song song** với query cấu hình (append/blank đọc sheet sau khi có dự án).
2. Mở transaction → khoá dòng `ProjectDocumentType (ProjectId, DocumentTypeId)` bằng `UPDLOCK, HOLDLOCK`, chờ tối đa 5 giây
   (`SubmissionConst.IMPORT_LOCK_TIMEOUT_MILLISECONDS`); hết giờ → `310`. Import khác dự án / loại văn bản không chặn nhau.
3. Đọc lại hồ sơ / dữ liệu **sau khi khoá** → dựng kế hoạch (sheet ≥ 5.000 dòng thì chạy song song, kết quả giống tuần tự).
4. Có thay đổi: bulk insert / update (`EFCore.BulkExtensions`, lô 100.000) + audit → `SaveChanges` → commit. Không có gì: commit
   rỗng, **không** ghi audit. Lỗi bất kỳ → rollback toàn bộ. Vi phạm unique index (import song song lọt khoá) → `310`.

## Hiệu năng — sheet 20.000 dòng × 30 trường (600.000 dữ liệu)

Đo ngày 2026-10-17 trên máy dev (SQL Server 2022 trong Docker, build Debug), `appsettings.json` đã **tắt MARS**, dự án mới tạo:

| API | Thời gian | API | Thời gian |
|---|---|---|---|
| verify append | 4,7 s | import append | **50,8 s** (mục tiêu < 60 s: đạt) |
| verify blank (20.000 ô) | 15,9 s | import blank | 27,2 s |
| verify overwrite (20.000 ô) | 3,9 s | import overwrite | 11,7 s |

Cùng code nhưng bật MARS: import append 383,7 s.

⚠️ **MARS + bulk**: bulk async trên kết nối MARS chết giữa chừng (`BeginExecuteNonQuery requires an open and available Connection`),
transaction phía SQL Server vẫn mở và giữ khoá → mọi import sau của loại văn bản đó nhận `310` tới khi `KILL` session. Bulk sync thì chạy
đúng nhưng chậm 8 lần. Vì vậy `SO_HOA_DB` để `MultipleActiveResultSets=False` (đổi 2026-10-17); môi trường deploy cũng phải tắt.

⚠️ `SubmissionRepository` đọc `SubmissionData` và chạy bulk bằng API **sync** (`ToList`, `BulkInsert`, `BulkUpdate`): SqlClient đọc / ghi cột
`nvarchar(max)` ở chế độ async chậm hàng chục lần (verify blank 101 s → 13,9 s khi đổi sang sync). Đừng "sửa" lại thành `ToListAsync`.

⚠️ Lô bulk 4.000 → 100.000: mỗi lô tốn vài lượt đi – về (~150 ms mỗi lượt ở môi trường Docker này), 150 lô chiếm gần hết thời gian
(insert 600.000 dữ liệu 88 s → ~35 s). Cả import trong một transaction nên lô lớn không đổi khả năng rollback.

## Xem file — proxy

`GET proxy-file?url=`: `url` phải là `FileUrlS3Object` của một hồ sơ (không thì `305`). BE tải bằng `HttpClient`
(`IFileDownloader` → `HttpFileDownloader`) và trả **stream** `application/pdf`, `Content-Disposition: inline`,
`Cache-Control: private, max-age=300`. Link không phải http(s), lỗi mạng, HTTP lỗi hoặc `Content-Type` khác `application/pdf` /
`application/octet-stream` → `315` (vẫn `ApiResponse` JSON, HTTP 200). FE kiểm `Content-Type` của response: JSON là lỗi.
FE phải gọi bằng `HttpClient` kèm Bearer token rồi tạo blob URL (iframe không gửi được header token).

## Đã kiểm thực tế

Ngày 2026-10-17, gọi API thật bằng `admin` trên dự án test (sheet tạo qua API dự án, ghi dữ liệu bằng tài khoản OAuth) — 33/33 đạt:
verify append = import append; append lần 2 → 0; `ABC.pdf` trong DB, sheet `abc.pdf` → không tạo mới, blank điền đúng hồ sơ đó;
dòng trống ở giữa → `IndexGoogleSheet` đúng dòng thật; blank thêm data cho trường chưa có data; overwrite: giá trị giống cũ không
đếm, `CHECKED` bị bỏ qua / ghi được khi `overwriteChecked = true`, audit có giá trị cũ / mới, 0 thay đổi không ghi audit; lỗi `306`,
`307` (sai tên cột, sai `columnImport`), `308` (kèm số dòng), `309`, `311`, `312`, `313` (cả verify), `314`; 2 append đồng thời
200 dòng → một tạo 200, một chờ khoá rồi tạo 0, không trùng; proxy PDF → `%PDF` inline, link HTML → `315`, url ngoài hồ sơ → `305`;
không token → `401`; Swagger nhóm `construction` đủ 7 API.

## Giới hạn

⚠️ Chưa kiểm: thiếu quyền → `403`. Rollback đã kiểm bằng cách dừng API giữa lúc bulk insert (đã chèn 20.000 hồ sơ + 396.000 dữ liệu
trong transaction): DB không còn hồ sơ, dữ liệu, audit nào của lần import đó.

⚠️ Sheet dự án do tài khoản OAuth tạo trong thư mục `GoogleFolderId`; reader dùng **service account**, nên thư mục / sheet phải share cho
`client_email` của service account (người dùng tự share) — không thì `203`.

⚠️ Trường thêm vào loại văn bản **sau** khi gắn vào dự án không tự vào cấu hình `ProjectDocumentTypeDocumentField` (hành vi của
[07-du-an.md](07-du-an.md)), nên import không thấy trường đó cho tới khi cấu hình dự án được cập nhật.

⚠️ `proxy-file` tìm `FileUrlS3Object` không có index (cột `nvarchar(1000)` vượt giới hạn khoá index) → quét bảng `Submission` mỗi lần
xem file. Bảng lớn thì nên đổi sang tìm theo `Id` hồ sơ — đổi contract FE, hỏi trước. Chấp nhận `application/octet-stream` vì MinIO
thường lưu PDF với kiểu này, nên file không phải PDF mà có kiểu đó vẫn lọt.

⚠️ Thông báo lỗi `308` / `314` liệt kê **mọi** dòng lỗi; sheet lỗi hàng nghìn dòng cho message rất dài.

> **Tiếp:** [12-ho-so-kiem-tra.md](12-ho-so-kiem-tra.md) — xem, lọc và kiểm tra hồ sơ sau khi import.
