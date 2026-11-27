# Dự án (Construction)

> **Phần 7/22** · trước: [06-truong-van-ban.md](06-truong-van-ban.md) · mục lục: [README.md](README.md)

Danh mục dự án, bảng `construction.Project`. Google Sheet riêng của dự án tạo khi chọn `isGoogleSheetEnabled` lúc tạo (Task 54A); không chọn thì `UrlGoogleSheetSync = ""`, chỉ nhập bằng Excel.
Trường: `Name`, `StartedAt`, `EndedAt`, `Status`, `UrlGoogleSheetSync`, `GoogleSheetName`, `IsQaEnabled` ([Có / không QA](#có--không-qa--isqaenabled)), cột tạo / sửa / xoá, cờ `Deleted` (`ISoftDeleted`, đổi tên từ `IsDeleted` ở migration `Rename_Project_Deleted_Add_Submission`).

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Danh sách (không phân trang) | `GET api/construction/project` | `Function.Construction.Project.View` |
| Dropdown | `GET api/construction/project/dropdown` | `Function.Construction.Project.View` |
| Danh sách phân trang theo cặp dự án – loại văn bản ([25](25-xoa-du-an-danh-sach-cap.md)) | `GET api/construction/project/paging` | `Function.Construction.Project.View` |
| Xem chi tiết | `GET api/construction/project/{id}` | `Function.Construction.Project.View` |
| Tạo mới (kèm tạo Google Sheet khi chọn) | `POST api/construction/project` | `Function.Construction.Project.Create` |
| Cập nhật | `PUT api/construction/project/{id}` | `Function.Construction.Project.Update` |
| Xoá | `DELETE api/construction/project/{id}` | `Function.Construction.Project.Delete` |
| Xoá kèm dữ liệu phụ thuộc, chỉ Admin ([25](25-xoa-du-an-danh-sach-cap.md)) | `DELETE api/construction/project/admin/delete/{id:int}` | `Function.Construction.Project.Delete` |
| Tổng hợp nghiệm thu, xuất PDF biên bản nghiệm thu ([22-nghiem-thu-du-an.md](22-nghiem-thu-du-an.md)) | `GET api/construction/project/{id:int}/acceptance`, `.../acceptance/report` | `Function.Construction.Project.View` |
| Bắt đầu / hoàn tất nghiệm thu, trả về đang triển khai | `POST .../{id:int}/acceptance/start`, `.../finish`, `.../reopen` | `Function.Construction.Project.Accept` |
| Hạn dự án: danh sách, chạy job, trạng thái job, lịch sử mail ([15-han-du-an.md](15-han-du-an.md)) | `GET deadline`, `POST deadline-check/run`, `GET deadline-check/status`, `GET deadline-notification/paging` | `Function.Construction.Project.Deadline` |

Body tạo: `{ "name", "startedAt", "endedAt", "isQaEnabled", "isGoogleSheetEnabled", "documentTypeIds": [] }` (hai cờ không gửi = `true`; `isGoogleSheetEnabled` chỉ dùng lúc tạo, không lưu cột). Hệ thống tự gán `status = DOING` (đang triển khai)
và `googleSheetName = name`; FE không gửi hai trường này. Tạo xong trả `data` cùng dạng `GET {id}` (`FindProjectByIdDto`:
`id`, `name`, `startedAt`, `endedAt`, `status`, `urlGoogleSheetSync`, `googleSheetName`, `isQaEnabled`, `documentTypeIds`).
Body sửa: `{ "name", "startedAt", "endedAt", "status", "documentTypeIds": [] }` — **không** sửa được `urlGoogleSheetSync`,
`googleSheetName`, `isQaEnabled` (gửi kèm thì bị bỏ qua); đổi `name` thì tên file Google Sheet đổi theo (nếu dự án có Sheet).

Dropdown trả `[{ id, name }]` sắp theo `name`. Danh sách không phân trang (`GetListProjectDto`: `id`, `name`, `startedAt`,
`endedAt`, `status`, `urlGoogleSheetSync`, `isQaEnabled`) sắp mới tạo trước (`CreatedDate` giảm dần, rồi `Id` giảm dần). Paging từ Task 29
trả theo cặp dự án – loại văn bản, Task 38 thêm lọc `creatorKeyword` / khoảng ngày và trường `creatorName` ([25](25-xoa-du-an-danh-sach-cap.md#danh-sách-cặp--get-apiconstructionprojectpaging)).
Cả ba API bỏ dự án đã xoá mềm và giới hạn theo người gọi ([Phạm vi xem](#phạm-vi-xem--task-30)).

## Phạm vi xem — Task 30

Task 30 (2026-11-13): Leader / Data / QA được gán `Project.View` chỉ thấy dự án **được phân** = có dòng `ProjectMember` chưa gỡ của mình ở
≥ 1 loại văn bản của dự án, mọi vai trò, kể cả đang tắt (cùng luật paging). Admin / SuperAdmin thấy mọi dự án như cũ.

| API | Ngoài phạm vi | Leader của dự án | Data / QA của dự án |
|---|---|---|---|
| `GET project`, `GET project/dropdown` | Không hiện (không dự án nào → `[]`) | Có link | `urlGoogleSheetSync = null`, xét **theo từng dòng** |
| `GET project/{id}` | `303` như dự án không tồn tại | Như cũ, `myProgresses = null` | Link `null`, thêm `myProgresses` |
| `GET project/{id}/acceptance` | `303` | Xem đủ | Xem đủ như Leader ([22](22-nghiem-thu-du-an.md#api)) |

- Người xem tổng = Admin, hoặc Leader chưa gỡ ở ≥ 1 cặp của dự án (không xét `IsActive`); luật ở `ProjectDomainService.EnsureViewableAsync`
  (trả `true` / `false`, ngoài dự án ném `303` để không lộ dự án có tồn tại), đọc vai trò qua `ProjectRepository.GetMemberRolesAsync`.
- `myProgresses` (`GetListProjectMyProgressDto`): mỗi cặp mình là Data / QA chưa gỡ và `ProjectDocumentType` chưa xoá, sắp theo tên loại
  văn bản (loại đã xoá cuối, giữ tên cũ): `documentTypeId`, `documentTypeName`, `viewerRole`, `totalBox`, `totalProfileRange`,
  `totalSubmission`, `checkedCount`, `progressPercent`, `filledFieldCount` (chỉ Data). Đếm bằng **cùng hàm** với cột "của tôi" ở paging
  (`ProjectQuery.CountMyHeldSubmissionsAsync`) nên số luôn khớp ([25](25-xoa-du-an-danh-sach-cap.md#danh-sách-cặp--get-apiconstructionprojectpaging)).
- `viewerRoles` (`GetListProjectViewerRoleDto`, Task 42 bước 0, mọi người gọi): `[{ documentTypeId, viewerRole }]` của cặp chưa gỡ, sắp `documentTypeId`. Admin → mọi
  cặp `Admin`; người khác → `Role` ở `ProjectMember` của cặp (không xét `IsActive`), cặp không tham gia thì không có dòng. Để FE biết Leader quản lý cặp nào;
  quyền xem văn bản vẫn theo `ResolveRestrictedUserIdAsync` (Leader cần `ViewAllSubmission` + đang hoạt động), nên Leader thiếu một trong hai vẫn chỉ thấy văn bản mình giữ.
- Không đổi luật bấm nghiệm thu (`337`); ba API start / finish / reopen vẫn trả `GET acceptance` ở cuối.

## Loại văn bản của dự án — `ProjectDocumentType`

Bảng nối `construction.ProjectDocumentType` (`ProjectId`, `DocumentTypeId`, cột tạo / sửa / xoá, cờ `Deleted`), khoá ghép
`(ProjectId, DocumentTypeId)`, index `IX_ProjectDocumentType_DocumentTypeId`; migration `Add_ProjectDocumentType`.
Chưa có khoá ngoại sang `Project` / `DocumentType` (giống `DocumentField`).
Ghi qua CRUD dự án (`documentTypeIds`), theo mẫu `User` – `UserRole`:

- `documentTypeIds` bỏ trùng; rỗng → `400` "Chọn ít nhất 1 metadata" (Task 40, `ProjectDomainService.EnsureHasDocumentTypes`, cả tạo
  lẫn sửa); id không tồn tại hoặc đã xoá → `301` "Loại văn bản không tồn tại" (`DocumentTypeDomainService.EnsureAllExistAsync`),
  kiểm **trước** khi gọi Google.
- Sửa mà danh sách metadata đổi (thêm hoặc gỡ) khi dự án `ACCEPTING` → `341`, `DONE` → `313`, kèm câu "… không đổi được metadata"
  (Task 40, `Project.EnsureDocumentTypesChangeable`, kiểm trước khi gọi Google); sửa tên / ngày giữ nguyên danh sách thì vẫn được.
- Gỡ loại mà cặp (dự án, loại) còn ≥ 1 văn bản chưa xoá → `347` "Metadata đã có văn bản trong dự án, không gỡ được: <tên>" (Task 40,
  `ProjectDomainService.EnsureDocumentTypesRemovableAsync`). Trong transaction sửa, **khoá import** từng cặp sắp gỡ (như import, đang
  import → `310`) rồi mới đếm, để import không chen văn bản sau lúc đếm. Muốn gỡ thì Admin xoá hết văn bản của cặp trước.
- Gắn một loại văn bản (`ProjectService.AddDocumentTypesAsync`) = thêm dòng `ProjectDocumentType` **và** chép mọi trường chưa
  xoá của loại đó vào `ProjectDocumentTypeDocumentField` (`ProjectDocumentType.AssignDocumentFields`): `FieldImport` lấy từ
  `DocumentField.FieldImport` (rỗng nếu null), `OrderFieldImport` đánh `1, 2, …` **riêng cho từng loại**, theo `OrderTableView` rồi `Id`.
- Tạo: một transaction — lưu dự án lấy `Id`, rồi gắn loại văn bản + audit, lưu lần hai; lỗi → rollback và xoá file sheet.
- Sửa: **so khác biệt** với danh sách cũ, một transaction. Loại giữ nguyên **không đụng** (giữ `FieldImport` / thứ tự đã chỉnh).
  Loại bị bỏ (gỡ, chỉ khi chưa có văn bản) thì **xoá mềm** ở cả hai bảng (Task 36, trước đó xoá cứng). Loại thêm vào mà từng gỡ thì **khôi phục** dòng cũ
  (`ProjectDocumentType.Restore` + `RestoreDocumentFields`): giữ `FieldImport` / `OrderFieldImport` cũ, trường đã xoá trong lúc gỡ
  vẫn xoá, trường thêm sau khi gỡ thì chép mới nối cuối thứ tự (`max + 1`, theo `OrderTableView`); loại chưa từng gắn thì gắn như trên.
- Xoá dự án (API cũ): xoá mềm luôn các dòng ở cả hai bảng; API Admin xoá thêm 14 bảng khác ([25](25-xoa-du-an-danh-sach-cap.md)). `GET {id}` trả `documentTypeIds` đang gắn.
- Xoá trường (`DELETE document-field/{id}` hoặc `quick-delete`) → xoá mềm dòng của trường đó ở mọi dự án
  (`DocumentFieldService.DeleteProjectDocumentFieldsAsync`); `OrderFieldImport` các trường còn lại **không đánh lại** (để hở,
  giống `OrderTableView`). **Thêm** trường sau khi loại đã gắn vào dự án thì chưa đồng bộ sang dự án.

Trường văn bản theo dự án: bảng `construction.ProjectDocumentTypeDocumentField` (`ProjectId`, `DocumentTypeId`,
`DocumentFieldId`, `FieldImport` bắt buộc, `OrderFieldImport`, cột tạo / sửa / xoá, cờ `Deleted`), khoá ghép ba cột,
index `IX_ProjectDocumentTypeDocumentField_DocumentFieldId`; migration `Add_ProjectDocumentTypeDocumentField`. Chưa có
khoá ngoại; chưa có API riêng — chỉ được ghi qua CRUD dự án (xem trên).

Lưu ý: Khoá ghép + xoá mềm (cả hai bảng): gán lại cặp đã xoá mềm bằng `Add` sẽ trùng khoá chính — phải khôi phục dòng cũ
(`GetDeletedDocumentTypesAsync` / `GetDeletedDocumentFieldsAsync` dùng `IgnoreQueryFilters()`), đừng thêm dòng mới, đừng quay lại xoá cứng.

## Trạng thái — `ProjectConst`

| Hằng | Giá trị | Nghĩa |
|---|---|---|
| `DOING` | `2` | Đang triển khai (gán khi tạo). Giá trị `1` (`DEPLOYING`) đã gộp vào đây từ Task 38 (`UPDATE` 1 → 2 chạy tay) |
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
- Xoá là **xoá mềm** (`Deleted = true`, global query filter ẩn đi); xem lại sau xoá trả `303`. **Không** xoá Google Sheet (API Admin đổi tên thành `"{Name} (đã xoá)"` nếu dự án có Sheet).
- Mỗi thao tác ghi để lại audit `Tạo / Cập nhật / Xóa dự án` kèm giá trị cũ và mới.

## Tạo dự án và Google Sheet

Thứ tự trong `ProjectService.Create`:

1. Kiểm tên trùng, dựng `Project.Create` (kiểm ngày, gán `DOING`, `GoogleSheetName = Name`) — lỗi ở đây thì **chưa** gọi Google.
2. Chỉ khi `isGoogleSheetEnabled = true`: đọc tài khoản Google từ bảng cấu hình ([08-cau-hinh-du-an.md](08-cau-hinh-du-an.md)); thiếu dòng nào → `304` nêu tên.
   `IGoogleSheetExternal.CreateSpreadsheetAsync(account, Name, GoogleSheetName)`: tạo file trong thư mục `GoogleFolderId`,
   tên file = `Name`, sheet đầu đổi tên = `GoogleSheetName`. Lỗi Google → `205`, không lưu gì.
3. `LinkGoogleSheet(url)` (`https://docs.google.com/spreadsheets/d/<id>/edit`), lưu dự án, gắn loại văn bản + audit
   (audit có đúng `Id`), tất cả trong một transaction.
4. Lưu DB lỗi → rollback, có file vừa tạo thì `TryDeleteSpreadsheetAsync` xoá rồi ném lại lỗi gốc.

Cờ `false` bỏ bước 2 (không cần kết nối Google) và không `LinkGoogleSheet`. "Có Sheet" = `Project.HasGoogleSheet()` (`UrlGoogleSheetSync`
khác rỗng); dự án không Sheet đổi tên / xoá không gọi Google, import nguồn Sheet dự án → `348` ([11](11-ho-so-import.md)).
URL lưu đúng dạng `GoogleSheetReaderExternal` đọc được, nên dùng thẳng cho `sheet-header` / `map-field-import`.

## Sửa dự án và đổi tên file

`ProjectService.Update`: kiểm tên trùng, `Project.Update`; **chỉ khi `Name` đổi và dự án có Sheet** thì `RenameSpreadsheetAsync` đổi tên file
Google Sheet (Drive `files.update`) **trước** khi lưu — lỗi Google → `206`, chưa lưu gì. Lưu DB lỗi thì rollback, nếu đã
đổi tên thì `TryRenameSpreadsheetAsync` đổi tên file về tên cũ rồi ném lại lỗi gốc. Tên tab (`GoogleSheetName`) không đổi theo.
Cấu hình phía Google: [../architecture/05-infrastructure-external.md](../architecture/05-infrastructure-external.md#ispreadsheetwriter--tạo-google-sheet).

## Đã kiểm thực tế

Ngày 2026-10-15, API thật bằng `admin`, `FolderId` rỗng: `status = 9` khi tạo → `400` (luật cũ); `startedAt` sau `endedAt` → `400`; body hợp lệ → `205` (Drive
`File not found`), không lưu; `GET` / `PUT` / `DELETE` id không tồn tại → `303`; không token → `401`; seeder đủ 4 quyền `Function.Construction.Project.*`.

Ngày 2026-10-16: `dropdown` đủ 2 dự án sắp tên; `paging` cỡ 2 ra 2 item, `totalItems = 2`; `status=2` rỗng; `keyword=" test "` khớp cả hai; `GET project` id 2 trước id 1.
`documentTypeIds`: `GET 1` ra `[]`; `[999999]` → `301`, không gọi Google, không lưu. Sửa với `[22, 26]` → 20 + 7 trường, thứ tự `1..20`, `1..7` khớp `OrderTableView`;
`[26, 30]` → 22 bị xoá, 26 giữ `CreatedDate`, 30 thêm 12 trường; `[]` → hai bảng trống. Xoá một / `quick-delete` trường → dòng cấu hình `Deleted = 1`, có `DeletedBy`.

Ngày 2026-11-24 (Task 54A, `ZZTEST54A` đã dọn, token đặt rỗng tạm rồi trả đúng SHA-256): chưa kết nối + cờ `false` → `200`, URL rỗng; cờ `true` / không gửi
→ `304`, không lưu; đổi tên, xoá kèm dữ liệu dự án không Sheet → `200`, không gọi Google, log không cảnh báo; đã kết nối + cờ `true` → có Sheet, xoá đổi tên "(đã xoá)".

Ngày 2026-11-13 (Task 28, DB dev, dự án tạm `ZZTEST28` đã xoá) — **10/10**: dự án cũ sau migration đều `isQaEnabled = true`; tạo
`isQaEnabled = false` → `false`, không gửi → `true` (DB, `GET {id}`, `paging`, `GET` danh sách khớp); audit tạo có `IsQaEnabled`;
`PUT` gửi `isQaEnabled = true` → cờ vẫn `false`. Tạo thật nên còn 2 file Google Sheet trên Drive (xoá dự án không xoá file).

Ngày 2026-11-13 (Task 30, DB dev, `ZZTEST30` đã dọn) — danh sách + dropdown **21/21**, chi tiết + nghiệm thu **32/32**: Admin thấy đủ; Leader có link; Data / QA
link `null`; người đang tắt vẫn thấy, đã gỡ / ngoài dự án `[]` / `303`; Leader P1 + Data P2 thấy link P1, không P2; `myProgresses` Data 10 / 4 = 40%, ô 12 khớp
paging và SQL, QA 4 / 1 = 25%; nghiệm thu Data / QA giống Leader. 2026-11-14 gộp checklist **43/43**, thêm user chỉ `Admin` / `SuperAdmin` và dự án đã xoá mềm.

`viewerRoles` (2026-11-21, `ZZTEST42S0` đã dọn) — 6/6: Admin ra mọi cặp `Admin`; Leader M1 + Data M2 ra đúng hai dòng, `myProgresses` vẫn `null`; Data ra một dòng,
`myProgresses` vẫn là mảng; gỡ cặp thì mất dòng dù thành viên còn; người ngoài `303`.

## Giới hạn

Lưu ý: Chưa có unique index cho `Name` (index `IX_Project_Name` không unique); hai request tạo cùng tên đồng thời vẫn có thể lọt.
Lưu ý: Xoá dự án không xoá file Google Sheet. Muốn đồng bộ thêm thì thêm method vào `IGoogleSheetExternal`, đừng gọi Google API thẳng từ service.

Lưu ý: Xoá file khi tạo lỗi và đổi tên ngược khi sửa lỗi là **bù trừ**, không phải transaction: nếu cả lưu DB lẫn bước bù trừ
đều lỗi thì file mồ côi hoặc lệch tên (đã ghi log `TryDeleteSpreadsheetAsync` / `RenameSpreadsheetAsync`).

> **Tiếp:** [08-cau-hinh-du-an.md](08-cau-hinh-du-an.md) — cấu hình dự án và xác thực tài khoản Google.
