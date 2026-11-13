# Dự án (Construction)

> **Phần 7/22** · trước: [06-truong-van-ban.md](06-truong-van-ban.md) · mục lục: [README.md](README.md)

Danh mục dự án, bảng `construction.Project`. Mỗi dự án có một Google Sheet riêng, hệ thống **tự tạo** lúc tạo dự án.
Trường: `Name`, `StartedAt`, `EndedAt`, `Status`, `UrlGoogleSheetSync`, `GoogleSheetName`, `IsQaEnabled` ([Có / không QA](#có--không-qa--isqaenabled)), cột tạo / sửa / xoá, cờ `Deleted` (`ISoftDeleted`, đổi tên từ `IsDeleted` ở migration `Rename_Project_Deleted_Add_Submission`).

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Danh sách (không phân trang) | `GET api/construction/project` | `Function.Construction.Project.View` |
| Dropdown | `GET api/construction/project/dropdown` | `Function.Construction.Project.View` |
| Danh sách phân trang theo cặp dự án – loại văn bản ([25](25-xoa-du-an-danh-sach-cap.md)) | `GET api/construction/project/paging` | `Function.Construction.Project.View` |
| Xem chi tiết | `GET api/construction/project/{id}` | `Function.Construction.Project.View` |
| Tạo mới (kèm tạo Google Sheet) | `POST api/construction/project` | `Function.Construction.Project.Create` |
| Cập nhật | `PUT api/construction/project/{id}` | `Function.Construction.Project.Update` |
| Xoá | `DELETE api/construction/project/{id}` | `Function.Construction.Project.Delete` |
| Xoá kèm dữ liệu phụ thuộc, chỉ Admin ([25](25-xoa-du-an-danh-sach-cap.md)) | `DELETE api/construction/project/admin/delete/{id:int}` | `Function.Construction.Project.Delete` |
| Tổng hợp nghiệm thu ([22-nghiem-thu-du-an.md](22-nghiem-thu-du-an.md)) | `GET api/construction/project/{id:int}/acceptance` | `Function.Construction.Project.View` |
| Bắt đầu / hoàn tất nghiệm thu, trả về đang làm | `POST .../{id:int}/acceptance/start`, `.../finish`, `.../reopen` | `Function.Construction.Project.Accept` |
| Hạn dự án: danh sách, chạy job, trạng thái job, lịch sử mail ([15-han-du-an.md](15-han-du-an.md)) | `GET deadline`, `POST deadline-check/run`, `GET deadline-check/status`, `GET deadline-notification/paging` | `Function.Construction.Project.Deadline` |

Body tạo: `{ "name", "startedAt", "endedAt", "isQaEnabled", "documentTypeIds": [] }` (`isQaEnabled` không gửi = `true`). Hệ thống tự gán `status = DEPLOYING` (đang triển khai)
và `googleSheetName = name`; FE không gửi hai trường này. Tạo xong trả `data` cùng dạng `GET {id}` (`FindProjectByIdDto`:
`id`, `name`, `startedAt`, `endedAt`, `status`, `urlGoogleSheetSync`, `googleSheetName`, `isQaEnabled`, `documentTypeIds`).
Body sửa: `{ "name", "startedAt", "endedAt", "status", "documentTypeIds": [] }` — **không** sửa được `urlGoogleSheetSync`,
`googleSheetName`, `isQaEnabled` (gửi kèm thì bị bỏ qua); đổi `name` thì tên file Google Sheet đổi theo.

Dropdown trả `[{ id, name }]` sắp theo `name`. Danh sách không phân trang trả mọi dự án (`GetListProjectDto`: `id`, `name`, `startedAt`,
`endedAt`, `status`, `urlGoogleSheetSync`, `isQaEnabled`), sắp mới tạo trước (`CreatedDate` giảm dần, rồi `Id` giảm dần). Paging từ Task 29
trả theo cặp dự án – loại văn bản, giới hạn theo người gọi ([25](25-xoa-du-an-danh-sach-cap.md#danh-sách-cặp--get-apiconstructionprojectpaging)).
Cả ba API bỏ dự án đã xoá mềm.

## Loại văn bản của dự án — `ProjectDocumentType`

Bảng nối `construction.ProjectDocumentType` (`ProjectId`, `DocumentTypeId`, cột tạo / sửa / xoá, cờ `Deleted`), khoá ghép
`(ProjectId, DocumentTypeId)`, index `IX_ProjectDocumentType_DocumentTypeId`; migration `Add_ProjectDocumentType`.
Chưa có khoá ngoại sang `Project` / `DocumentType` (giống `DocumentField`).
Ghi qua CRUD dự án (`documentTypeIds`), theo mẫu `User` – `UserRole`:

- `documentTypeIds` bỏ trùng; được rỗng; id không tồn tại hoặc đã xoá → `301` "Loại văn bản không tồn tại"
  (`DocumentTypeDomainService.EnsureAllExistAsync`), kiểm **trước** khi gọi Google.
- Gắn một loại văn bản (`ProjectService.AddDocumentTypesAsync`) = thêm dòng `ProjectDocumentType` **và** chép mọi trường chưa
  xoá của loại đó vào `ProjectDocumentTypeDocumentField` (`ProjectDocumentType.AssignDocumentFields`): `FieldImport` lấy từ
  `DocumentField.FieldImport` (rỗng nếu null), `OrderFieldImport` đánh `1, 2, …` **riêng cho từng loại**, theo `OrderTableView` rồi `Id`.
- Tạo: một transaction — lưu dự án lấy `Id`, rồi gắn loại văn bản + audit, lưu lần hai; lỗi → rollback và xoá file sheet.
- Sửa: **so khác biệt** với danh sách cũ — loại bị bỏ thì xoá cứng (kể cả dòng đã xoá mềm) ở cả hai bảng; loại mới thì gắn như trên;
  loại giữ nguyên **không đụng** (giữ `FieldImport` / thứ tự đã chỉnh, giữ `CreatedDate`). Tất cả trong một transaction.
- Xoá dự án (API cũ): xoá mềm luôn các dòng ở cả hai bảng; API Admin xoá thêm 14 bảng khác ([25](25-xoa-du-an-danh-sach-cap.md)). `GET {id}` trả `documentTypeIds` đang gắn.
- Xoá trường (`DELETE document-field/{id}` hoặc `quick-delete`) → xoá mềm dòng của trường đó ở mọi dự án
  (`DocumentFieldService.DeleteProjectDocumentFieldsAsync`); `OrderFieldImport` các trường còn lại **không đánh lại** (để hở,
  giống `OrderTableView`). **Thêm** trường sau khi loại đã gắn vào dự án thì chưa đồng bộ sang dự án.

Trường văn bản theo dự án: bảng `construction.ProjectDocumentTypeDocumentField` (`ProjectId`, `DocumentTypeId`,
`DocumentFieldId`, `FieldImport` bắt buộc, `OrderFieldImport`, cột tạo / sửa / xoá, cờ `Deleted`), khoá ghép ba cột,
index `IX_ProjectDocumentTypeDocumentField_DocumentFieldId`; migration `Add_ProjectDocumentTypeDocumentField`. Chưa có
khoá ngoại; chưa có API riêng — chỉ được ghi qua CRUD dự án (xem trên).

⚠️ Khoá ghép + xoá mềm (cả hai bảng): gán lại cặp đã xoá mềm bằng `Add` sẽ trùng khoá chính — phải xoá cứng trước
(như `DeleteDocumentTypesAsync`) hoặc khôi phục dòng cũ (`IgnoreQueryFilters()`), đừng thêm dòng mới.

## Trạng thái — `ProjectConst`

| Hằng | Giá trị | Nghĩa |
|---|---|---|
| `DEPLOYING` | `1` | Đang triển khai (gán khi tạo) |
| `DOING` | `2` | Đang làm |
| `ACCEPTING` | `3` | Đang nghiệm thu: khoá import (`341`), kiểm tra / QA vẫn được |
| `DONE` | `4` | Đã xong |

Vào / ra `ACCEPTING` và sang `DONE` **chỉ** qua API nghiệm thu hoặc tự chuyển khi văn bản cuối QA đạt (dự án không QA: văn bản cuối được kiểm) — bảng chuyển, luật và mã
`340`–`342` ở [22-nghiem-thu-du-an.md](22-nghiem-thu-du-an.md).
| `EXPIRED` | `5` | Hết hạn: job tự chuyển khi qua `EndedAt`, chặn thao tác hồ sơ như `DONE` (mã `323`); mở lại bằng `PUT` |

## Có / không QA — `IsQaEnabled`

Task 28 (2026-11-13): QA đi kèm khi chủ đầu tư và Admin chọn; dự án cần nhanh thì chỉ nhập liệu. Cột `bit NOT NULL` mặc định `1`,
migration `Add_Project_IsQaEnabled`.

- **Chỉ đặt lúc tạo** (`Project.Create`); không có API / trường nào đổi sau đó. Cờ theo dự án, không theo loại văn bản.
- Dự án tạo trước Task 28 = **có QA** (default cột), giữ nguyên hành vi cũ. Audit tạo dự án ghi `IsQaEnabled` ngay sau `Status`.
- Dự án không QA: văn bản xong khi `CHECKED`, tự vào nghiệm thu ở `PUT submission/check` ([22](22-nghiem-thu-du-an.md)); 4 API ghi QA
  → `345` "Dự án không bật QA" (`Project.EnsureQaEnabled`, [13](13-ho-so-qa.md), [18](18-qa-cham-mau.md)); API đọc QA không chặn,
  ra rỗng / 0 — FE dựa vào `isQaEnabled` để ẩn màn / cột QA. Phần Operation (giao QA, phiếu `DONE`, tiền QA) do Task 15 / 12 / 14.

## Luật nghiệp vụ

- `Name` **duy nhất** trong các dự án chưa xoá (`ProjectDomainService.EnsureUniqueNameAsync`) → trùng trả `409`
  "Tên dự án đã tồn tại"; khi sửa không tính trùng với chính nó.
- Sửa: `Status` ngoài `ProjectConst.STATUSES` → `400` "Trạng thái dự án không hợp lệ" (kiểm trong `Project.Update`). Đổi `Status`
  sang `ACCEPTING` / `DONE` hoặc rời `ACCEPTING` → `342` "Dùng chức năng nghiệm thu"; giữ nguyên `Status` (kể cả `ACCEPTING` / `DONE`) thì vẫn sửa được.
- `StartedAt` sau `EndedAt` (khi có cả hai) → `400` "Ngày bắt đầu không được sau ngày kết thúc" (tạo và sửa).
- Xoá là **xoá mềm** (`Deleted = true`, global query filter ẩn đi); xem lại sau xoá trả `303`. **Không** xoá Google Sheet (API Admin đổi tên thành `"{Name} (đã xoá)"`).
- Mỗi thao tác ghi để lại audit `Tạo / Cập nhật / Xóa dự án` kèm giá trị cũ và mới.

## Tạo dự án và Google Sheet

Thứ tự trong `ProjectService.Create`:

1. Kiểm tên trùng, dựng `Project.Create` (kiểm ngày, gán `DEPLOYING`, `GoogleSheetName = Name`) — lỗi ở đây thì **chưa** gọi Google.
2. Đọc tài khoản Google từ bảng cấu hình ([08-cau-hinh-du-an.md](08-cau-hinh-du-an.md)); thiếu dòng nào → `304` nêu tên.
   `IGoogleSheetExternal.CreateSpreadsheetAsync(account, Name, GoogleSheetName)`: tạo file trong thư mục `GoogleFolderId`,
   tên file = `Name`, sheet đầu đổi tên = `GoogleSheetName`. Lỗi Google → `205`, không lưu gì.
3. `LinkGoogleSheet(url)` (`https://docs.google.com/spreadsheets/d/<id>/edit`), lưu dự án, gắn loại văn bản + audit
   (audit có đúng `Id`), tất cả trong một transaction.
4. Lưu DB lỗi → rollback, `TryDeleteSpreadsheetAsync` xoá file vừa tạo rồi ném lại lỗi gốc.

URL lưu đúng dạng `GoogleSheetReaderExternal` đọc được, nên dùng thẳng cho `sheet-header` / `map-field-import`.

## Sửa dự án và đổi tên file

`ProjectService.Update`: kiểm tên trùng, `Project.Update`; **chỉ khi `Name` đổi** thì `RenameSpreadsheetAsync` đổi tên file
Google Sheet (Drive `files.update`) **trước** khi lưu — lỗi Google → `206`, chưa lưu gì. Lưu DB lỗi thì rollback, nếu đã
đổi tên thì `TryRenameSpreadsheetAsync` đổi tên file về tên cũ rồi ném lại lỗi gốc. Tên tab (`GoogleSheetName`) không đổi theo.

Cấu hình phía Google: [../architecture/05-infrastructure-external.md](../architecture/05-infrastructure-external.md#ispreadsheetwriter--tạo-google-sheet).

## Đã kiểm thực tế

Ngày 2026-10-15, gọi API thật bằng `admin`, `FolderId` rỗng: `status = 9` khi tạo → `400` (luật cũ, nay body tạo không còn
`status`); `startedAt` sau `endedAt` → `400`;
body hợp lệ → `205` (Drive báo `File not found` cho thư mục rỗng), không lưu dự án; `GET` / `PUT` / `DELETE` id không tồn tại
→ `303`; không token → `401`; seeder thêm đủ 4 quyền `Function.Construction.Project.*`.

Ngày 2026-10-16: `dropdown` trả đủ 2 dự án sắp theo tên; `paging` trang 1 cỡ 2 trả 2 item, `totalItems = 2`; `status=2` trả
rỗng; `keyword=" test "` khớp cả hai; `GET project` trả 2 dự án, id 2 trước id 1; không token → `401`. Sau khi thêm `documentTypeIds`: `GET 1` trả `documentTypeIds: []`; tạo và sửa với
`[999999]` → `301`, không gọi Google, không lưu gì. Sửa dự án 1 với `[22, 26]` → 20 + 7 trường, thứ tự `1..20` và `1..7`
khớp `OrderTableView`; rồi `[26, 30]` → 22 bị xoá, 26 giữ nguyên `CreatedDate`, 30 thêm 12 trường; rồi `[]` → hai bảng trống. Loại văn bản tạm 3 trường gắn vào dự án 1: xoá một trường → dòng đó `Deleted = 1`; `quick-delete`
hai trường còn lại → cả hai `Deleted = 1`, có `DeletedBy`.

Chưa có `GoogleRefreshToken` → tạo trả `304` "Thiếu cấu hình dự án: GoogleRefreshToken". Chưa kiểm luồng tạo thành công,
đổi tên, xoá trên dự án thật (cần người dùng xác thực Google trước).

Ngày 2026-11-13 (Task 28, DB dev, dự án tạm `ZZTEST28` đã xoá) — **10/10**: dự án cũ sau migration đều `isQaEnabled = true`; tạo
`isQaEnabled = false` → `false`, không gửi → `true` (DB, `GET {id}`, `paging`, `GET` danh sách khớp); audit tạo có `IsQaEnabled`;
`PUT` gửi `isQaEnabled = true` → cờ vẫn `false`. Tạo thật nên còn 2 file Google Sheet trên Drive (xoá dự án không xoá file).

## Giới hạn

⚠️ Chưa có unique index cho `Name` (index `IX_Project_Name` không unique); hai request tạo cùng tên đồng thời vẫn có thể lọt.

⚠️ Xoá dự án không xoá file Google Sheet. Muốn đồng bộ thêm thì thêm method vào `IGoogleSheetExternal`, đừng gọi Google API
thẳng từ service.

⚠️ Xoá file khi tạo lỗi và đổi tên ngược khi sửa lỗi là **bù trừ**, không phải transaction: nếu cả lưu DB lẫn bước bù trừ
đều lỗi thì file mồ côi hoặc lệch tên (đã ghi log `TryDeleteSpreadsheetAsync` / `RenameSpreadsheetAsync`).

> **Tiếp:** [08-cau-hinh-du-an.md](08-cau-hinh-du-an.md) — cấu hình dự án và xác thực tài khoản Google.
