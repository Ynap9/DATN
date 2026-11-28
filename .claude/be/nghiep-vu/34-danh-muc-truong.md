# Trường kiểu danh mục (Task 75)

> Cập nhật 2026-11-28. Danh mục thuộc từng metadata (loại văn bản); trường chọn Kiểu = Danh mục thì gợi ý giá trị ở màn Nhập liệu / QA,
> đưa vào prompt OCR và được đối chiếu lúc chạy thử import. Nhập liệu "mềm": giá trị ngoài danh mục vẫn lưu được, BE không chặn.
> Nguồn: soHoaSaoMai `metadataEnum.service.js` (danh mục theo metadata, không xoá khi còn trường dùng) + `buildFieldGuideJson` (đưa giá trị vào prompt).
> Màn FE: [../../fe/architecture/07-man-construction.md](../../fe/architecture/07-man-construction.md). Trường văn bản: [06](06-truong-van-ban.md).

## Schema — migration `Add_DocumentEnum`

| Thay đổi | Chi tiết |
|---|---|
| Bảng `construction.DocumentEnum` | `Id`, `DocumentTypeId` (FK `Restrict`), `Name` nvarchar(200), `ValuesJson` nvarchar(max) (mảng chuỗi JSON), cột tạo / sửa / xoá mềm như `DocumentField` |
| Unique | `(DocumentTypeId, Name)` lọc `[Deleted] = 0` nên xoá mềm rồi tạo lại cùng tên được |
| `DocumentField.EnumId` | `int null`, FK `DocumentEnum` (`Restrict`); dữ liệu cũ giữ `FieldType = "string"`, `EnumId = null` |

`DocumentFieldConst.FIELD_TYPES` = `string` / `enum`. Trước khi chặn giá trị khác, đã quét `SELECT DISTINCT FieldType` trên DB chung (2026-11-28):
21 dòng đều là `string`.

## Luật danh mục

- Tên trim 1–200 ký tự, trùng trong metadata → `409`.
- Giá trị: trim từng dòng, bỏ dòng trống, bỏ trùng không phân biệt hoa thường (giữ cách viết lần đầu), giữ thứ tự nhập; sau chuẩn hoá phải
  1–500 giá trị, mỗi giá trị ≤ 500 ký tự, sai → `400` câu cụ thể. Lưu JSON với `UnsafeRelaxedJsonEscaping` để tiếng Việt đọc được khi xem DB.
- Sửa danh mục **không** đụng dữ liệu văn bản đã nhập: giá trị cũ thành "ngoài danh mục".
- Xoá mềm; còn trường chưa xoá trỏ tới → `350` "Danh mục đang được dùng bởi trường: {tên các trường}". Đổi trường về Chữ rồi xoá được.
- Tạo / sửa / xoá đều khoá dòng `DocumentType` (`LockOrderAsync`, cùng khoá với tạo / sửa trường) nên xoá danh mục và chọn danh mục cho trường
  không đua nhau. Audit: `ACTION_DOCUMENT_ENUM_CREATE / UPDATE / DELETE` (giá trị ghi dạng mảng, không phải JSON thô).

## API — `api/v1/construction/document-type/{id}/enum`

| API | Quyền | Việc |
|---|---|---|
| `GET` | `DocumentType.View` | Danh sách theo tên: `id`, `name`, `values`, `usedByFields [{ id, name }]`. Metadata không có / ngoài phạm vi xem → `301` (như `GET {id}/document-fields`) |
| `POST` | `DocumentType.Update` | `{ name, values[] }` → trả `id` danh mục mới (FE form trường dùng để chọn luôn) |
| `PUT {enumId}` | `DocumentType.Update` | Sửa tên + giá trị |
| `DELETE {enumId}` | `DocumentType.Update` | Xoá mềm; `350` khi còn trường dùng |

`enumId` không thuộc metadata hoặc đã xoá → `351` "Không tìm thấy danh mục". Không thêm key quyền mới. Ghi chỉ cần metadata tồn tại (`301`),
không kiểm phạm vi xem, giống `PUT document-type/{id}`.

## Trường kiểu danh mục

- `POST document-field` nhận thêm `fieldType` (mặc định `string`) và `enumId`; `PUT document-field/{id}` nhận thêm `enumId`.
- `fieldType` ngoài `string` / `enum` → `400`. `enum` bắt buộc `enumId` của **cùng** metadata, chưa xoá (thiếu / sai → `351`). `string` thì `enumId`
  bị đặt `null`. Đổi kiểu không đụng dữ liệu đã nhập. `quick-create` giữ `string`.
- `GET document-type/{id}/document-fields` trả thêm `enumId`, `enumName`; `GET document-field/{id}` trả thêm `enumId`; `submission/{id}` trả
  `fields[].enumValues` (`null` khi `string`, đọc danh mục **hiện tại** nên sửa danh mục là màn Nhập liệu đổi theo).

## Prompt OCR

`PromptTemplateDomainService.Compose` nhận thêm `Dictionary<int, EnumFieldValues>` (id trường → giá trị danh mục hiện tại). Trường danh mục thêm chi tiết
`Chỉ chọn một trong: a; b; c` (nối `"; "`), tối đa 100 giá trị đầu, quá thì thêm "…" sau giá trị cuối; đặt sau "Ví dụ", trước "Định dạng". Xem trước
(`GET ai/prompt-template/{id}/preview`) cũng thấy. Quan hệ với bản đã xuất bản: Compose đọc danh mục lúc chạy nên **sửa danh mục là prompt của bản đã
xuất bản đổi theo, không tăng phiên bản** — cùng một phiên bản có thể ra prompt khác nhau theo thời điểm ([27](27-ai-prompt.md)).

## Chạy thử import báo giá trị lạ

6 API chạy thử (3 kiểu Google + 3 kiểu Excel, [30](30-import-ghep-truong.md)) trả thêm `outOfEnum: [{ documentFieldId, name, count, samples }]`.
So khớp sau trim, không phân biệt hoa thường; ô trống không tính; `samples` ≤ 5 giá trị khác nhau. Tính trên **ô sắp được ghi** (Thêm mới: mọi ô có giá trị
của văn bản mới; Điền trống / Ghi đè: ô thực sự đổi), nên ô giữ nguyên hoặc văn bản đã kiểm bị bỏ qua không được tính. Không chặn import, không đổi số ô ghi.

## Đã kiểm thực tế — 2026-11-28

DB riêng LocalDB `ZZTEST75` (migration áp từ đầu, đã xoá sau khi kiểm; DB chung chưa có bảng mới vì PM chưa duyệt script) + BE thật:
- API 30/30: tạo (trim, bỏ trùng / dòng trống, giữ thứ tự), `409` trùng tên, `400` 501 giá trị / giá trị 501 ký tự / tên trắng / không giá trị, 500 giá trị
  tạo được, sửa thêm giá trị, `351` enumId metadata khác, `301`, trường `enum` + `enumId`, `fieldType` lạ `400`, `enum` thiếu `enumId` `351`,
  `quick-create` giữ `string`, `usedByFields`, xoá khi còn dùng `350` kèm tên trường, đổi về Chữ xoá được, xoá lại `351`, tạo lại cùng tên sau xoá mềm. Audit đủ 3 loại.
- Prompt: xem trước có `Chỉ chọn một trong: Vĩnh viễn; 70 năm; 50 năm; 20 năm`, sửa danh mục thì xem trước đổi theo.
- `submission/{id}`: `enumValues` đúng, `null` ở trường chữ; `PUT submission/check` với giá trị ngoài danh mục vẫn lưu được.
- Hàm thuần (console tạm): `outOfEnum` gộp " 30 NĂM " và "30 năm" thành 1 mẫu, ô trống không tính, "vĩnh viễn" không tính; quá 100 giá trị thì thêm "…".

Lưu ý: Chưa gọi thật 6 API chạy thử có `outOfEnum`: Google cần tài khoản kết nối, Excel cần MinIO còn chỗ (MinIO dev đang đầy). Số liệu chỉ kiểm bằng hàm thuần.

> **Tiếp:** [README.md](README.md) — mục lục nghiệp vụ.
