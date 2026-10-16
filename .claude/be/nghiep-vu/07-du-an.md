# Dự án (Construction)

> **Phần 7/8** · trước: [06-truong-van-ban.md](06-truong-van-ban.md) · mục lục: [README.md](README.md)

Danh mục dự án, bảng `construction.Project`. Mỗi dự án có một Google Sheet riêng, hệ thống **tự tạo** lúc tạo dự án.
Trường: `Name`, `StartedAt`, `EndedAt`, `Status`, `UrlGoogleSheetSync`, `GoogleSheetName`, cột tạo / sửa / xoá, cờ `IsDeleted`.

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Danh sách (không phân trang) | `GET api/construction/project` | `Function.Construction.Project.View` |
| Dropdown | `GET api/construction/project/dropdown` | `Function.Construction.Project.View` |
| Danh sách phân trang | `GET api/construction/project/paging` | `Function.Construction.Project.View` |
| Xem chi tiết | `GET api/construction/project/{id}` | `Function.Construction.Project.View` |
| Tạo mới (kèm tạo Google Sheet) | `POST api/construction/project` | `Function.Construction.Project.Create` |
| Cập nhật | `PUT api/construction/project/{id}` | `Function.Construction.Project.Update` |
| Xoá | `DELETE api/construction/project/{id}` | `Function.Construction.Project.Delete` |

Body tạo: `{ "name", "startedAt", "endedAt" }`. Hệ thống tự gán `status = DEPLOYING` (đang triển khai) và
`googleSheetName = name`; FE không gửi hai trường này. Tạo xong trả `data` cùng dạng `GET {id}` (`FindProjectByIdDto`:
`id`, `name`, `startedAt`, `endedAt`, `status`, `urlGoogleSheetSync`, `googleSheetName`).
Body sửa: `{ "name", "startedAt", "endedAt", "status" }` — **không** sửa được `urlGoogleSheetSync`, `googleSheetName`;
đổi `name` thì tên file Google Sheet đổi theo.

Dropdown trả `[{ id, name }]` sắp theo `name`. Paging nhận `pageNumber`, `pageSize`, `keyword` (tìm trong `name`, tự trim),
`status` (tuỳ chọn, lọc đúng trạng thái); trả `{ items, totalItems }`, mỗi item `id`, `name`, `startedAt`, `endedAt`, `status`,
`urlGoogleSheetSync`; sắp mới tạo trước (`CreatedDate` giảm dần, rồi `Id` giảm dần). Danh sách không phân trang trả mọi dự án
cùng dạng item và cùng thứ tự như paging (`GetListProjectDto`). Cả ba API bỏ dự án đã xoá mềm.

## Loại văn bản của dự án — `ProjectDocumentType`

Bảng nối `construction.ProjectDocumentType` (`ProjectId`, `DocumentTypeId`, cột tạo / sửa / xoá, cờ `Deleted`), khoá ghép
`(ProjectId, DocumentTypeId)`, index `IX_ProjectDocumentType_DocumentTypeId`; migration `Add_ProjectDocumentType`.
Chưa có khoá ngoại sang `Project` / `DocumentType` (giống `DocumentField`). 🔬 Chưa có API.

⚠️ Khoá ghép + xoá mềm: gán lại cặp đã xoá mềm bằng `Add` sẽ trùng khoá chính — khi làm API phải khôi phục dòng cũ
(`IgnoreQueryFilters()`) hoặc xoá cứng, đừng thêm dòng mới.

## Trạng thái — `ProjectConst`

| Hằng | Giá trị | Nghĩa |
|---|---|---|
| `DEPLOYING` | `1` | Đang triển khai (gán khi tạo) |
| `DOING` | `2` | Đang làm |
| `ACCEPTING` | `3` | Đang nghiệm thu |
| `DONE` | `4` | Đã xong |

## Luật nghiệp vụ

- `Name` **duy nhất** trong các dự án chưa xoá (`ProjectDomainService.EnsureUniqueNameAsync`) → trùng trả `409`
  "Tên dự án đã tồn tại"; khi sửa không tính trùng với chính nó.
- Sửa: `Status` ngoài `ProjectConst.STATUSES` → `400` "Trạng thái dự án không hợp lệ" (kiểm trong `Project.Update`).
- `StartedAt` sau `EndedAt` (khi có cả hai) → `400` "Ngày bắt đầu không được sau ngày kết thúc" (tạo và sửa).
- Xoá là **xoá mềm** (`IsDeleted = true`, global query filter ẩn đi); xem lại sau xoá trả `303`. **Không** xoá Google Sheet.
- Mỗi thao tác ghi để lại audit `Tạo / Cập nhật / Xóa dự án` kèm giá trị cũ và mới.

## Tạo dự án và Google Sheet

Thứ tự trong `ProjectService.Create`:

1. Kiểm tên trùng, dựng `Project.Create` (kiểm ngày, gán `DEPLOYING`, `GoogleSheetName = Name`) — lỗi ở đây thì **chưa** gọi Google.
2. Đọc tài khoản Google từ bảng cấu hình ([08-cau-hinh-du-an.md](08-cau-hinh-du-an.md)); thiếu dòng nào → `304` nêu tên.
   `ISpreadsheetWriter.CreateSpreadsheetAsync(account, Name, GoogleSheetName)`: tạo file trong thư mục `GoogleFolderId`,
   tên file = `Name`, sheet đầu đổi tên = `GoogleSheetName`. Lỗi Google → `205`, không lưu gì.
3. `LinkGoogleSheet(url)` (`https://docs.google.com/spreadsheets/d/<id>/edit`), lưu DB + audit trong một `SaveChanges`.
4. Lưu DB lỗi → `TryDeleteSpreadsheetAsync` xoá file vừa tạo rồi ném lại lỗi gốc.

URL lưu đúng dạng `GoogleSheetReader` đọc được, nên dùng thẳng cho `sheet-header` / `map-field-import`.

## Sửa dự án và đổi tên file

`ProjectService.Update`: kiểm tên trùng, `Project.Update`; **chỉ khi `Name` đổi** thì `RenameSpreadsheetAsync` đổi tên file
Google Sheet (Drive `files.update`) **trước** khi lưu — lỗi Google → `206`, chưa lưu gì. Lưu DB lỗi thì
`TryRenameSpreadsheetAsync` đổi tên file về tên cũ rồi ném lại lỗi gốc. Tên tab (`GoogleSheetName`) không đổi theo.

Cấu hình phía Google: [../architecture/05-infrastructure-external.md](../architecture/05-infrastructure-external.md#ispreadsheetwriter--tạo-google-sheet).

## Đã kiểm thực tế

Ngày 2026-10-15, gọi API thật bằng `admin`, `FolderId` rỗng: `status = 9` khi tạo → `400` (luật cũ, nay body tạo không còn
`status`); `startedAt` sau `endedAt` → `400`;
body hợp lệ → `205` (Drive báo `File not found` cho thư mục rỗng), không lưu dự án; `GET` / `PUT` / `DELETE` id không tồn tại
→ `303`; không token → `401`; seeder thêm đủ 4 quyền `Function.Construction.Project.*`.

Ngày 2026-10-16: `dropdown` trả đủ 2 dự án sắp theo tên; `paging` trang 1 cỡ 2 trả 2 item, `totalItems = 2`; `status=2` trả
rỗng; `keyword=" test "` khớp cả hai; `GET project` trả 2 dự án, id 2 trước id 1; không token → `401`.

Chưa có `GoogleRefreshToken` → tạo trả `304` "Thiếu cấu hình dự án: GoogleRefreshToken". Chưa kiểm luồng tạo thành công,
đổi tên, xoá trên dự án thật (cần người dùng xác thực Google trước).

## Giới hạn

⚠️ Chưa có unique index cho `Name` (index `IX_Project_Name` không unique); hai request tạo cùng tên đồng thời vẫn có thể lọt.

⚠️ Xoá dự án không xoá file Google Sheet. Muốn đồng bộ thêm thì thêm method vào `ISpreadsheetWriter`, đừng gọi Google API
thẳng từ service.

⚠️ Xoá file khi tạo lỗi và đổi tên ngược khi sửa lỗi là **bù trừ**, không phải transaction: nếu cả lưu DB lẫn bước bù trừ
đều lỗi thì file mồ côi hoặc lệch tên (đã ghi log `TryDeleteSpreadsheetAsync` / `RenameSpreadsheetAsync`).

> **Tiếp:** [08-cau-hinh-du-an.md](08-cau-hinh-du-an.md) — cấu hình dự án và xác thực tài khoản Google.
