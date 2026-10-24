# Hồ sơ — xoá và xuất Excel (Construction)

> **Phần 14/15** · trước: [13-ho-so-qa.md](13-ho-so-qa.md) · mục lục: [README.md](README.md)

Hai thao tác trên màn danh sách hồ sơ của một dự án + loại văn bản: chọn hồ sơ để xoá, hoặc tải danh sách theo bộ lọc
hiện tại về Excel. Cùng `SubmissionController` và cùng bộ lọc với [12-ho-so-kiem-tra.md](12-ho-so-kiem-tra.md#bộ-lọc-chung).

## Các chức năng

| Endpoint (`api/v1/construction/submission/...`) | Quyền | Việc |
|---|---|---|
| `DELETE` (gốc, `ids` trong body) | `Function.Construction.Submission.Delete` | Xoá mềm nhiều hồ sơ, tối đa 500 mỗi lần |
| `GET export-excel` + bộ lọc chung | `Function.Construction.Submission.Export` | Tải `.xlsx` theo bộ lọc |

## Xoá hồ sơ

Body `DeleteSubmissionDto`: `{ projectId, documentTypeId, ids: [] }`. Mọi hồ sơ trong `ids` phải thuộc đúng dự án và loại văn bản đó.

| Lỗi | Mã |
|---|---|
| Dự án không tồn tại / đã xoá | `303` |
| Dự án `DONE` (trừ SuperAdmin) | `316` |
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

⚠️ Toàn bộ dòng và các ô trường được nạp vào bộ nhớ, dựng workbook ClosedXML trong bộ nhớ rồi mới trả về; không stream. Dự án lớn
nhiều trường sẽ tốn RAM và thời gian tương ứng. Chưa đo.

⚠️ Mọi ô được ghi dạng chuỗi (`SetValue(string)`), kể cả `STT`. Excel sẽ coi số là văn bản và không tính được tổng. Muốn ô số
thì đổi `ExcelExternal` và kiểu dữ liệu của hàng, đừng xử lý ở FE.

## Chưa kiểm thực tế

Hai API này mới viết (commit `2070cd1`), chưa gọi thật trên DB có dữ liệu, chưa kiểm luồng xoá bị khoá bởi import, chưa đo
thời gian xuất. Kiểm trên `.claude/be/db/test-data-ho-so.sql` trước khi dựa vào các con số hoặc hành vi ở trên.

> **Tiếp:** [15-dong-goi-cau-hinh.md](15-dong-goi-cau-hinh.md) — cấu hình metadata đóng gói theo TT05: loại gói, schema, trường, bảng mã.
