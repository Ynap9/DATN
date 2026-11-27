# Loại văn bản (Construction)

> **Phần 5/22** · trước: [04-nhat-ky-he-thong.md](04-nhat-ky-he-thong.md) · mục lục: [README.md](README.md)

Danh mục loại văn bản, bảng `construction.DocumentType`. Entity `DocumentType` có `Id`, `Name`, `DefaultView`
(mặc định `1`), cột tạo / sửa và xoá mềm.

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Dropdown `[{ id, name }]` sắp theo `name` | `GET api/construction/document-type/dropdown` | `Function.Construction.DocumentType.View` |
| Danh sách có phân trang | `GET api/construction/document-type/paging` | `Function.Construction.DocumentType.View` |
| Xem chi tiết | `GET api/construction/document-type/{id}` | `Function.Construction.DocumentType.View` |
| Danh sách đầy đủ các trường của loại văn bản | `GET api/construction/document-type/{id}/document-fields` | `Function.Construction.DocumentType.View` |
| Tải template Excel nhập liệu | `GET api/construction/document-type/{id}/excel-template` | `Function.Construction.DocumentType.View` |
| Tạo mới | `POST api/construction/document-type` | `Function.Construction.DocumentType.Create` |
| Cập nhật | `PUT api/construction/document-type/{id}` | `Function.Construction.DocumentType.Update` |
| Xoá | `DELETE api/construction/document-type/{id}` | `Function.Construction.DocumentType.Delete` |

Body tạo / sửa: `{ "name": "...", "defaultView": 1 }`. Bỏ `defaultView` khi tạo thì nhận `1`.

## Luật nghiệp vụ

- `Name` **duy nhất** trong các bản ghi chưa xoá (`DocumentTypeDomainService.EnsureUniqueNameAsync`) → trùng trả `409`
  "Tên loại văn bản đã tồn tại". Khi sửa, bản ghi đang sửa không tính là trùng với chính nó.
- So khớp tên **không phân biệt hoa thường** (theo collation mặc định của SQL Server).
- Xoá là **xoá mềm**: bản ghi biến mất khỏi danh sách và chi tiết (lỗi `301`), tên được dùng lại. Còn dự án chưa xoá đang gắn
  loại này → `346` (Task 29, [25](25-xoa-du-an-danh-sach-cap.md#chặn-xoá-loại-văn-bản-đang-dùng--346)).
- Danh sách lọc `keyword` theo `Name`, sắp mới tạo trước; `pageSize = -1` lấy tất cả. Mỗi dòng có thêm số trường, số dự án đang
  dùng, ngày tạo ([dưới](#danh-sách--số-trường-số-dự-án-đang-dùng--task-39)).
- `{id}/document-fields` trả **mọi cột** (`GetListDocumentFieldDto`) của các trường chưa xoá thuộc loại văn bản, không phân
  trang, sắp theo `OrderTableView` rồi `Id`. Loại văn bản không tồn tại hoặc đã xoá → `301`; chưa có trường → `[]`. Nằm ở
  `DocumentTypeService` / `DocumentTypeQuery` (theo yêu cầu), không ở `DocumentFieldService`.
- `{id}/excel-template` trả **file** `.xlsx` (không bọc `ApiResponse`): một sheet tên = `Name` của loại văn bản, dòng `1`
  in đậm, cố định khi cuộn, mỗi trường chưa xoá một cột theo `OrderTableView` rồi `Id`; header là `FieldImport`, rỗng hoặc
  toàn khoảng trắng thì lấy `Name` của trường — điền xong import lại thì `map-field-import` khớp đủ. Tên file `<Name>_template.xlsx`.
  Tên sheet theo luật Excel: ký tự `[ ] : * ? / \` thành `_`, cắt còn 31 ký tự. Loại văn bản không tồn tại → vẫn trả JSON
  `ApiResponse` lỗi `301` (HTTP 200); chưa có trường → file chỉ có sheet trống. Chỉ đọc, không audit.
- Mỗi thao tác ghi để lại audit log `Tạo / Cập nhật / Xóa loại văn bản` kèm giá trị cũ và mới.

## Phạm vi xem — Task 36

Task 36 (2026-11-14): năm API đọc (dropdown, paging, `{id}`, `{id}/document-fields`, `{id}/excel-template`) giới hạn theo người gọi.
Admin / SuperAdmin thấy mọi loại như cũ; người khác chỉ thấy loại **được phân** = có dòng `ProjectMember` chưa gỡ của mình ở một cặp
dự án – loại văn bản **đang gắn** (`ProjectDocumentType` chưa xoá), mọi vai trò, kể cả đang tắt — cùng luật paging dự án
([25](25-xoa-du-an-danh-sach-cap.md)). Loại bị gỡ khỏi dự án thì thành viên cặp đó không thấy nữa, chọn lại thì thấy lại.

- Dropdown / paging: chỉ trả loại trong phạm vi (không có → `[]`, `totalItems = 0`); luật ở `DocumentTypeQuery.FilterViewable`.
- `{id}`, `{id}/document-fields`, `{id}/excel-template`: ngoài phạm vi → `301` như loại không tồn tại, để không lộ loại có tồn tại;
  luật ở `DocumentTypeDomainService.EnsureViewableAsync` (đọc qua `DocumentTypeRepository.IsAssignedToUserAsync`, cùng điều kiện với query).
- API ghi (tạo / sửa / xoá) không đổi — Leader / Data / QA không có quyền Create / Update / Delete.
- Đã kiểm 2026-11-14 bằng API thật (bản build mới chạy cổng riêng, dữ liệu ZZTEST36 đã dọn): Admin / Leader / Data / QA / người ngoài
  ở cả năm API, gỡ rồi chọn lại loại — 46/46 cùng phần gỡ metadata ở [07](07-du-an.md#loại-văn-bản-của-dự-án--projectdocumenttype).

## Danh sách — số trường, số dự án đang dùng — Task 39

Task 39 (2026-11-20): `GET paging` trả thêm (chỉ thêm, trường cũ `id`, `name`, `defaultView` giữ nguyên) để Admin biết metadata nào
đang dùng mà không mở từng cái. Đếm bằng subquery `COUNT` trong **một** câu SQL (`DocumentTypeQuery.FindPaging` chiếu `Select` tay).

| Trường | Luật |
|---|---|
| `fieldCount` | Số `DocumentField` chưa xoá của loại |
| `projectCount` | Số dự án **khác nhau** chưa xoá đang gắn loại (`ProjectDocumentType` chưa xoá), mọi trạng thái kể cả `DONE` / `EXPIRED` — cùng luật chặn xoá `IsUsedByProjectAsync`: `> 0` thì xoá trả `346`. Người khác Admin chỉ đếm dự án mình là thành viên chưa gỡ của đúng cặp đó (cùng điều kiện `FilterViewable`) |
| `createdDate` | `DocumentType.CreatedDate`, có thể `null` |

Lưu ý: Vì người khác Admin chỉ đếm trong phạm vi mình, metadata còn dự án khác dùng vẫn có thể ra `projectCount = 0` với họ. FE ẩn nút
Xóa theo số này nên nếu sau này gán `DocumentType.Delete` cho Leader / Data / QA, họ thấy Xóa rồi nhận `346`. Hiện chỉ Admin có quyền
Delete nên không gặp (chốt 2026-11-20 giữ đúng spec); đừng "sửa" bằng cách đếm mọi dự án cho người khác — lộ số dự án ngoài phạm vi.

Đã kiểm 2026-11-20 bằng API thật (bản build mới chạy cổng riêng, dữ liệu ZZTEST39 tạo bằng API + SQL, đã dọn): 3 trường có trong
response; xoá mềm 1 trường → `fieldCount` −1; gắn 2 dự án (`DOING` + `DONE`) → `projectCount` 2; Leader / Data chỉ là thành viên dự án
đầu → chỉ thấy metadata đó, `projectCount` 1; `346` khi còn dự án; xoá mềm dự án → giảm; về 0 thì xoá được; số 3 metadata thật khớp SQL
đếm tay và không đổi sau test. SQL log: một câu, nhánh Admin EF bỏ hẳn điều kiện thành viên. Trình duyệt cùng ngày (Admin / Leader /
Data / User): menu, route mới, `project-config` → not found, 7 cột, ẩn / hiện "Xóa", User không thấy menu và bị guard chặn URL.

## Đã kiểm thực tế

Ngày 2026-10-12, gọi API thật bằng tài khoản `admin`: tạo, tạo trùng (`409`), phân trang, xem chi tiết, sửa
`defaultView = 0` (lưu đúng `0`), xoá, xem lại sau xoá (`301`), gọi không token (`401`), Swagger nhóm
`construction` (`200`), seeder thêm đủ 6 quyền, audit log ghi đủ 3 thao tác.

Template Excel (2026-10-15): loại có 7 trường → đúng 7 header theo thứ tự, tên sheet = tên loại; tên có ký tự cấm và dài
hơn 31 ký tự → sheet đã thay `_` và cắt đúng; `fieldImport` rỗng / toàn khoảng trắng → lấy `Name`; không tồn tại → `301`;
không token → `401`.

## Giới hạn

Lưu ý: Chưa có unique index trong DB cho `Name`; hai request tạo cùng tên gần như đồng thời vẫn có thể lọt. Muốn chặn hẳn
cần index có lọc `Deleted = 0` — là đổi schema, hỏi trước.

Lưu ý: Chưa kiểm `Name` rỗng; `POST` với `name` rỗng hiện vẫn tạo được bản ghi.

> **Tiếp:** [06-truong-van-ban.md](06-truong-van-ban.md) — các trường của một loại văn bản.
