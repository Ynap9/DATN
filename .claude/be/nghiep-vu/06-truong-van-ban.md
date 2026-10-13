# Trường của loại văn bản (Construction)

> **Phần 6/6** · trước: [05-loai-van-ban.md](05-loai-van-ban.md) · mục lục: [README.md](README.md)

Cấu hình các trường của một loại văn bản, bảng `construction.DocumentField`. Mỗi bản ghi thuộc một `DocumentType` qua
`DocumentTypeId`; có cờ hiển thị, thứ tự ở bảng (`OrderTableView`) và danh sách (`OrderListView`), độ rộng cột, cờ tính
phí, tên trường OCR, cột tạo / sửa và xoá mềm.

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Dropdown theo loại văn bản | `GET api/construction/document-field/dropdown/{documentTypeId}` | `Function.Construction.DocumentField.View` |
| Danh sách có phân trang | `GET api/construction/document-field/paging` | `Function.Construction.DocumentField.View` |
| Xem chi tiết | `GET api/construction/document-field/{id}` | `Function.Construction.DocumentField.View` |
| Lấy header từng sheet của Google Sheet | `GET api/construction/document-field/sheet-header?url=...` | `Function.Construction.DocumentField.Create` |
| Tạo mới | `POST api/construction/document-field` | `Function.Construction.DocumentField.Create` |
| Thêm nhanh nhiều trường | `POST api/construction/document-field/quick-create` | `Function.Construction.DocumentField.Create` |
| Cập nhật | `PUT api/construction/document-field/{id}` | `Function.Construction.DocumentField.Update` |
| Cập nhật cấu hình một trường | `PUT api/construction/document-field/{id}/setting` | `Function.Construction.DocumentField.Update` |
| Kéo thả thứ tự dạng bảng | `PUT api/construction/document-field/{id}/order-table-view` | `Function.Construction.DocumentField.Update` |
| Kéo thả thứ tự dạng danh sách | `PUT api/construction/document-field/{id}/order-list-view` | `Function.Construction.DocumentField.Update` |
| Xoá | `DELETE api/construction/document-field/{id}` | `Function.Construction.DocumentField.Delete` |
| Xoá nhanh nhiều trường | `DELETE api/construction/document-field/quick-delete` | `Function.Construction.DocumentField.Delete` |

Body tạo: `documentTypeId`, `name`, `field`, `fieldImport`, `isShow`, `isChargeable`, `fieldNameOcr`, `isFieldOcr`.
Body sửa: mọi trường trừ `documentTypeId` (kể cả `fieldType`, `isShowTableView`, `isShowListView`, `width`). Thuộc tính
`order` vẫn còn trong DTO cho FE cũ nhưng **bị bỏ qua** — thứ tự chỉ đổi qua hai API kéo thả.

## Luật nghiệp vụ

- Tạo: loại văn bản phải tồn tại và chưa xoá → nếu không, lỗi `301`.
- `Field` **duy nhất trong cùng loại văn bản** (bản ghi chưa xoá) → trùng trả `409` "Trường `<field>` đã tồn tại trong loại
  văn bản". Khi sửa, bản ghi đang sửa không tính là trùng với chính nó.

### Thêm nhanh

Body: `{ "documentTypeId": 1, "fieldNames": "SoHieu;NgayKy;TrichYeu" }` — chỉ có tên trường, phân cách bằng `;`.

- Mỗi tên tạo một trường với `Name = Field = FieldImport = tên`; các cột khác như tạo thường (`IsShow`, `IsChargeable`,
  `IsFieldOcr` là `false`, `FieldNameOcr` rỗng, hai cờ hiển thị `true`, `FieldType = "string"`).
- Tách chuỗi trong `DocumentFieldService.SplitFieldNames`: bỏ khoảng trắng hai đầu, bỏ mục rỗng, bỏ tên lặp trong chuỗi
  (không phân biệt hoa thường, giữ lần xuất hiện đầu).
- Không còn tên nào → `400` "Danh sách tên trường không được để trống". Loại văn bản không tồn tại → `301`.
- Thứ tự: lớn nhất (kể cả trường đã xoá) `+ 1` cho tên đầu, tăng dần theo thứ tự trong chuỗi.
- **Tất cả hoặc không**: một tên đã tồn tại → `409` nêu tên đó, không trường nào được thêm. Chạy trong transaction, có
  `LockOrderAsync` như tạo thường; một bản ghi audit "Thêm nhanh trường của loại văn bản" chứa danh sách trường mới.

### Xoá nhanh

Body (gửi kèm `DELETE`): `{ "documentTypeId": 1, "ids": [1, 2, 3] }` — xoá **theo một loại văn bản**.

- Bỏ id lặp; danh sách rỗng → `400` "Danh sách trường cần xóa không được để trống". Loại văn bản không tồn tại → `301`.
- **Tất cả hoặc không**: có id không tồn tại, đã xoá, hoặc thuộc loại văn bản khác → `302` "Trường không tồn tại trong loại
  văn bản: Id = …", không xoá trường nào.
- Khoá loại văn bản (`LockOrderAsync`) **trước** khi nạp các trường, trong cùng transaction.
- Như xoá một trường: xoá mềm, **không đổi thứ tự** của trường nào. Một bản ghi audit "Xóa nhanh trường của loại văn bản"
  chứa dữ liệu các trường trước khi xoá.

### Cập nhật cấu hình

Body: `{ "isShow": true, "isChargeable": false, "isFieldOcr": true, "fieldNameOcr": "so_hieu" }` — **một** trường mỗi lần.

- Chỉ ghi đè bốn cột `IsShow`, `IsChargeable`, `IsFieldOcr`, `FieldNameOcr` qua `DocumentField.UpdateSetting`; tên, `Field`,
  thứ tự giữ nguyên, nên không kiểm `Field` trùng và không `LockOrderAsync`.
- `fieldNameOcr` chỉ được ghi khi `isFieldOcr = true`; gửi `isFieldOcr = false` thì `FieldNameOcr` **giữ nguyên giá trị cũ**
  (FE không cần gửi), để bật lại OCR không phải nhập lại tên.
- Trường không tồn tại hoặc đã xoá → `302`. Một `SaveChanges`, không transaction; audit "Cập nhật cấu hình trường của loại
  văn bản" kèm giá trị cũ và mới.
- Chưa gọi thử API thật (người dùng tự kiểm).

### Header từ Google Sheet

`?url=<link Google Sheet>` → `[{ "sheetName": "Sheet1", "headers": ["Số hiệu", "Ngày ký"] }]`: mọi sheet theo đúng thứ tự,
header là dòng `1` đã bỏ ô trống và trim. Chỉ đọc, không ghi DB, không audit. Link sai → `202`; sheet chưa chia sẻ cho
service account hoặc không tồn tại → `203`. Cơ chế đọc: [../architecture/05-infrastructure-external.md](../architecture/05-infrastructure-external.md).

### Thứ tự

Hai cột `OrderTableView` (dạng bảng) và `OrderListView` (dạng danh sách) **độc lập**: lúc tạo bằng nhau, kéo thả cột nào
chỉ đổi cột đó. Trong một loại văn bản, mỗi cột của các trường chưa xoá **không trùng**, bắt đầu từ `1`, **không bắt buộc
liên tục** (xoá để lại chỗ hở). Entity đổi từng cột qua `ChangeOrderTableView` / `ChangeOrderListView`.

| Thao tác | Thứ tự |
|---|---|
| Tạo | Số lớn nhất **của cả hai cột** trong loại văn bản `+ 1`, tính cả trường đã xoá mềm, gán cho cả hai cột; chưa có trường nào thì `1` |
| Kéo thả | Xem bảng dưới; chỉ cột ứng với API |
| Sửa (`PUT {id}`) | **Không đổi** thứ tự |
| Xoá | **Không đổi thứ tự của trường nào**: trường bị xoá giữ thứ tự cũ, các trường khác giữ nguyên |

**Kéo thả** — body `{ "targetOrder": n }`; `from` là thứ tự hiện tại của trường được kéo, `to = targetOrder` kẹp vào
`[1, số lớn nhất của cột đó trong các trường chưa xoá]`:

| Trường hợp | Trường được kéo | Các trường chưa xoá khác |
|---|---|---|
| Kéo xuống (`from < to`) | thành `to` | thứ tự trong `(from, to]` giảm `1`; lớn hơn `to` giữ nguyên |
| Kéo lên (`from > to`) | thành `to` | thứ tự trong `[to, from)` tăng `1`; nhỏ hơn `to` giữ nguyên |
| `from = to` | không đổi | không đổi |

Ví dụ `1..6`, kéo trường `1` tới `5` → trường đó thành `5`, `2..5` thành `1..4`, `6` giữ nguyên. Luật nằm ở
`DocumentFieldDomainService.MoveOrderAsync` (dùng chung cho hai cột); audit chỉ ghi trường được kéo, không ghi trường bị dịch.
Trường không tồn tại hoặc đã xoá → `302`. Chưa gọi thử API thật (người dùng tự kiểm).

⚠️ Tạo mới phải lấy `MAX + 1`, **không** lấy `COUNT + 1`: khi đã có chỗ hở (còn `3, 4, 5`), `COUNT + 1 = 4` trùng thứ tự
đang dùng. `MAX` tính cả trường đã xoá (`GetMaxOrderAsync` dùng `IgnoreQueryFilters()`) và lấy **cột lớn hơn** của mỗi dòng:
kéo lên có thể làm số lớn nhất của một cột giảm (`1, 3, 5` kéo `5` lên `1` → `1, 2, 4`), chỉ lấy một cột thì trường mới
trùng ở cột kia.

Kéo thả chỉ dịch giá trị trong khoảng giữa nên trường đang dùng không trùng nhau, nhưng có thể trùng số với trường đã xoá.

**Không trùng khi ghi đồng thời:** tạo / sửa / xoá / kéo thả chạy trong transaction và mở đầu bằng `LockOrderAsync`, khoá dòng
`DocumentType` cha (`UPDLOCK, HOLDLOCK`). Request cùng loại văn bản xếp hàng, request sau đọc thứ tự request trước đã lưu;
khác loại văn bản thì không chặn nhau. Cũng nhờ khoá này mà kiểm `Field` trùng không lọt khi đồng thời.

⚠️ Kéo thả phải nạp trường **sau** khi khoá: service lấy `DocumentTypeId` bằng `FindDocumentTypeIdAsync` (không theo dõi
entity), khoá, rồi mới `FindByIdAsync`. Nạp entity trước khoá thì `from` có thể là số cũ, và EF không nạp lại entity đang
theo dõi khi đọc danh sách trong khoá → dịch sai, trùng thứ tự. Đừng "gọn code" bằng cách gộp về một lần `FindByIdAsync`.

⚠️ Không dùng unique index cho cột thứ tự: đánh lại thứ tự cập nhật từng dòng, SQL Server kiểm unique sau mỗi `UPDATE`,
nên lúc hai trường đổi chỗ sẽ trùng tạm thời và báo lỗi. Ghi trường mới mà bỏ `LockOrderAsync` là mở lại lỗi trùng thứ tự.

### Khác

- Tạo: `FieldType = "string"`, `IsShowTableView`, `IsShowListView` là `true` (`1`), `Width` rỗng. Body sửa không gửi hai cờ
  hiển thị thì cũng nhận `true`. DB có default `1` cho hai cột, cấu hình `HasDefaultValue(true).HasSentinel(true)`: EF bỏ
  qua giá trị `true` khi insert (DB tự điền `1`) và vẫn gửi `false` khi cần. Đừng bỏ `HasSentinel` — thiếu nó thì `false`
  bị bỏ qua và thành `1` (cùng bẫy với `DocumentType.DefaultView`).
- Sửa không đổi được `DocumentTypeId`.
- Dropdown trả `{ id, name }` các trường chưa xoá của **một** loại văn bản, sắp theo `OrderTableView` rồi `Id`; loại văn
  bản không tồn tại hoặc chưa có trường thì trả danh sách rỗng (không báo lỗi, giống dropdown loại văn bản).
- Danh sách lọc theo `documentTypeId` (tuỳ chọn) và `keyword` trên `Name` hoặc `Field`; sắp theo loại văn bản, rồi
  `OrderTableView`, rồi `Id`.
- Xoá là xoá mềm; xem lại sau xoá trả `302`. Mỗi thao tác ghi để lại audit log kèm giá trị cũ và mới.

## Đã kiểm thực tế

> Các bước "chuyển thứ tự", `order = 99`, `order = 0` dưới đây kiểm luật **cũ** của `PUT {id}` (đánh lại cả hai cột `1..n`),
> nay đã thay bằng kéo thả — không còn đúng với code hiện tại.

Ngày 2026-10-12, gọi API thật bằng `admin`: tạo 2 trường (thứ tự `1`, `2`), tạo trùng `Field` (`409`), loại văn bản
không tồn tại (`301`), phân trang lọc theo loại văn bản, xem chi tiết, xoá, xem lại sau xoá (`302`), seeder thêm đủ 5
quyền. Thứ tự: tạo 4 trường ra `1–4`; chuyển trường cuối lên `1`, rồi xuống `3`; gửi `order = 99` thì về cuối; xoá
một trường thì các trường sau dồn lên; tạo tiếp ra `n + 1`. Mọi bước hai cột thứ tự bằng nhau, hai cờ hiển thị `true`.

Ngày 2026-10-13, bắn đồng thời: 20 lệnh tạo, rồi 10 lệnh chuyển thứ tự, rồi 5 xoá + 5 tạo cùng lúc. Sau mỗi lượt thứ tự
đúng `1..20`, không trùng, hai cột bằng nhau, không deadlock.

Cùng ngày, sau khi đổi luật xoá, chạy lại trên DB sạch, kiểm bằng `GET paging` sau **mỗi** bước (không trùng, hai cột
bằng nhau, hai cờ hiển thị `true`, đúng từng số thứ tự mong đợi): tạo `1..5`, chuyển lên / xuống, `order = 99` và
`order = 0`; xoá trường `2` rồi `1` → còn `3, 4, 5`; tạo mới ra `6`; xoá `6` rồi tạo mới ra `7`; tạo trùng `Field`
(`409`); hai loại văn bản đếm riêng; ba lượt gọi đồng thời — tất cả đúng. Trong DB, tính cả trường đã xoá, số thứ tự khác
nhau bằng đúng số dòng của mỗi loại văn bản.

Thêm nhanh, kiểm bằng `GET` sau mỗi lần gọi: `SoHieu;NgayKy;TrichYeu` → `1, 2, 3`; ` NguoiKy ; ;CoQuan;nguoiky;` → thêm
đúng 2 trường `4, 5`; xoá trường `5` rồi thêm nhanh 2 trường → `6, 7`; có tên đã tồn tại → `409`, không thêm gì; chuỗi
toàn `;` → `400`; loại văn bản không tồn tại → `301`; tạo thường sau đó → `8`; 5 lệnh thêm nhanh đồng thời (mỗi lệnh 3
trường) → `1..15` không trùng; `name = field = fieldImport` ở mọi trường; ghi đủ 8 bản ghi audit.

Xoá nhanh theo loại văn bản, kiểm bằng `GET` sau mỗi lần gọi: có `1..6`, xoá `2, 4` → còn `1, 3, 5, 6`; gửi kèm id của
loại văn bản khác → `302`, cả hai loại không đổi; xoá `6` (gửi lặp) → còn `1, 3, 5`; thêm mới → `7`; loại văn bản không
tồn tại → `301`; danh sách rỗng → `400`; có id không tồn tại hoặc đã xoá → `302`, không xoá gì; `DELETE {id}` cũ vẫn
chạy; 4 lệnh xoá nhanh + 2 lệnh thêm nhanh đồng thời → tất cả `200`, thứ tự không trùng; ghi đủ 7 bản ghi audit; trong DB
trường đã xoá giữ nguyên thứ tự.

Dropdown, đối chiếu với `GET paging` cùng loại văn bản: thêm 4 trường → đủ 4 theo thứ tự; chuyển trường cuối lên `1` →
dropdown đổi theo; xoá một trường → không còn trong dropdown; loại văn bản khác chỉ ra trường của nó; loại văn bản rỗng
hoặc không tồn tại → `[]`; `GET {id}` không bị route `dropdown` che; không token → `401`.

## Giới hạn

⚠️ Chưa có unique index cho `(DocumentTypeId, Field)`; trùng `Field` được chặn bằng code dưới khoá ở trên. Ghi thẳng vào DB
(script, tool) thì không có gì chặn.

⚠️ Xoá loại văn bản không xoá các trường của nó; trường vẫn còn nhưng không tạo thêm được vào loại văn bản đã xoá.

⚠️ Chưa kiểm `Name`, `Field` rỗng, giống `DocumentType`.

> **Hết** · mục lục: [README.md](README.md)
