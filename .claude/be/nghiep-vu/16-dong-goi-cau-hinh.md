# Đóng gói — cấu hình metadata TT05 (Packaging)

> **Phần 16/18** · trước: [15-han-du-an.md](15-han-du-an.md) · mục lục: [README.md](README.md)

Nền của module đóng gói: dữ liệu cho biết **loại gói nào có những trường metadata nào**, đúng thứ tự, kiểu, độ dài, mức bắt buộc,
theo Thông tư 05/2025/TT-BNV. Quy định gốc ở [../dong-goi/README.md](../dong-goi/README.md). Cập nhật 2026-10-25: đã có bảng + seed
và 3 API **chỉ đọc**; cấu hình chỉ vào bằng seeder.

## Mô hình — ba bảng schema `packaging`

```text
PackageCodeValue ──(CodeGroup)── PackageSchemaField >──(PackageSchemaId, FK)── PackageSchema
```

| Bảng | Một dòng là | Khoá duy nhất (lọc `[Deleted] = 0`) |
|---|---|---|
| `PackageSchema` | Một cặp (loại gói, loại metadata) = một file XML metadata | `(PackageType, MetadataType)` |
| `PackageSchemaField` | Một phần tử trong `<simpledc>` của file đó, `SortOrder` = vị trí từ `1` | `(PackageSchemaId, ElementName)` |
| `PackageCodeValue` | Một giá trị hợp lệ của trường mã | `(CodeGroup, Code)` |

| `PackageType` (`PackageConst`) | Phụ lục | `MetadataType` (`PackageSchemaConst`) | File | `XsdFileName` / `MetadataFilePrefix` |
|---|---|---|---|---|
| `1` `AIP_hoso` | I | `1` Cấp gói (`Hồ sơ` / `Gói tài liệu` / `Yêu cầu khai thác`) | `EAD.xml` | `ead.xsd` / `EAD` |
| `2` `AIP_tailieu` | II | `2` Văn bản | `EAD_DOC_*.xml` | `ead_doc.xsd` / `EAD_DOC_` |
| `3` `SIP_hoso` | III | `3` Phim âm bản, ảnh | `EAD_PIC_*.xml` | `ead_pic.xsd` / `EAD_PIC_` |
| `4` `SIP_tailieu` | IV | `4` Ghi âm, ghi hình | `EAD_MEDIA_*.xml` | `ead_media.xsd` / `EAD_MEDIA_` |
| `5` `DIP` | V | | | |

5 × 4 = **20 schema**. Tên schema ≤ 31 ký tự vì dùng làm tên sheet Excel mẫu.

**Không có bảng trường dùng chung:** cùng một phần tử nhưng mỗi phụ lục khai khác (`mode` 30 ở cấp gói, 20 ở tài liệu; `description`
2000 / 500; `source` Boolean(1) ở SIP_tailieu, String(100) ở AIP_tailieu), nên thuộc tính lưu thẳng trên từng dòng `PackageSchemaField`.

## Số trường theo schema

| Loại gói | Cấp gói | Văn bản | Phim âm bản, ảnh | Ghi âm, ghi hình | Nguồn |
|---|---|---|---|---|---|
| `AIP_hoso` | 18 | 19 | 16 | 17 | `.xsd` trang 101–105 |
| `AIP_tailieu` | 4 | 19 | 18 | 19 | `.xsd` trang 173–177 |
| `SIP_hoso` | 18 | 21 | 18 | 19 | **gói mẫu** (thông tư `.xsd` chỉ 16 ở cấp gói, thiếu `riskRecovery*`) |
| `SIP_tailieu` | 5 | 21 | 18 | 19 | cấp gói trang 284; tài liệu cùng hình dạng `SIP_hoso` |
| `DIP` | 7 | 20 | 18 | 19 | `.xsd` trang 349–353 |

Tổng 333 trường, 82 mã. Danh sách phần tử đúng thứ tự nằm ở `PackagingSeedData.Schemas`.

Các chỗ chốt cần nhớ:

- SIP (cả hai biến thể) dùng `docCode` ở văn bản **và** pic/media — thông tư §5.2–5.3 ghi `docCode`; `arcDocCode` trong `.xsd` gói mẫu là
  lỗi #2 ([../dong-goi/04-sip-goi-mau.md](../dong-goi/04-sip-goi-mau.md)).
- AIP văn bản theo `.xsd` (19, `arcFileCode` + `docOrdinal` + `docCode`), **không** theo khung XML 21 trường ở bảng mô tả cùng phụ lục.
- PL I và PL II mâu thuẫn ở pic/media: giữ hai bộ riêng ([../dong-goi/05-aip.md](../dong-goi/05-aip.md)).
- DIP văn bản **không có `organName`**, có `numberOfPage` — theo `.xsd`, dù bảng mô tả ngược lại.
- `AIP_tailieu` cấp gói **không có `totalDoc`**.
- `docOrdinal` không có bảng mô tả nào trong thông tư: chọn `NUMBER` dài `7` (bằng số thứ tự tài liệu 7 ký tự).
- `numberOfPaper` thiếu trong bảng mô tả `SIP_hoso` (lỗi thông tư); tên, kiểu, độ dài lấy từ bảng `AIP_hoso`.

## Thuộc tính một trường

| Cột | Ý nghĩa |
|---|---|
| `DisplayName` | Tên tiếng Việt **đúng như bảng mô tả của phụ lục đó**, kể cả `(nếu có)` — vì vậy cùng phần tử có thể khác tên giữa các loại gói |
| `DataType` | `PackageSchemaFieldConst`: `1` TEXT · `2` NUMBER · `3` DATE · `4` BOOLEAN (nhận `1`/`0`) |
| `MaxLength` | Độ dài theo bảng; Date để `null` |
| `CodeGroup` | Nhóm mã trong `PackageCodeValue`; `null` nếu không phải trường mã. Boolean (`riskRecovery`, `process`, `docAttached`) không có nhóm |
| `AllowMultipleValues` | `true` duy nhất với `language` ("được chọn 1 hoặc nhiều giá trị") |
| `Description` | Cột "Mô tả" rút gọn (quy tắc ghép mã, định dạng ngày); không chép lại danh sách mã |

## Luật `Requirement`

| Trường | Mức | Điều kiện |
|---|---|---|
| Tên có `(nếu có)` | `OPTIONAL` | |
| `description` (mọi phụ lục) | `OPTIONAL` | Chốt 2026-10-21: gói mẫu để rỗng `description` của hồ sơ |
| `paperFileCode` | `CONDITIONAL` | `confidenceLevel` = `02` (hồ sơ số hóa) |
| `numberOfPaper` | `CONDITIONAL` | `confidenceLevel` = `02` — bảng `AIP_hoso` ghi "bắt buộc nhập với hồ sơ số hóa" |
| `riskRecoveryStatus` | `CONDITIONAL` | `riskRecovery` = `1` |
| `process` | `CONDITIONAL` | `confidenceLevel` = `01,03` |
| Còn lại | `REQUIRED` | |

`ConditionElement` / `ConditionValues` chỉ có giá trị ở dòng `CONDITIONAL`; nhiều mã cách nhau dấu phẩy. Điều kiện thắng `(nếu có)`
khi thông tư ghi rõ lúc nào bắt buộc.

⚠️ Vì `DisplayName` theo đúng từng phụ lục nên mức bắt buộc cũng lệch theo phụ lục: `codeNumber`, `codeNotation` là `REQUIRED` ở
`SIP_hoso` (bảng không ghi "nếu có") nhưng `OPTIONAL` ở `SIP_tailieu`, AIP; `confidenceLevel` cấp gói `REQUIRED` ở `AIP_hoso`,
`OPTIONAL` ở `SIP_hoso`. Đây là chữ của thông tư, đừng "sửa cho đồng bộ".

⚠️ AIP cho phép `startDate`, `endDate`, `issuedDate` dạng `MM/YYYY`, `YYYY`: mới ghi ở `Description`, chưa có chỗ nào kiểm.

## Bảng mã — 13 nhóm (`PackageCodeGroupConst`)

| Nhóm | Mã |
|---|---|
| `maintenance` | `01` Vĩnh viễn … `07` Khác (7) |
| `mode` | `01` Công khai · `02` Sử dụng có điều kiện · `03` Mật |
| `language` | `01` Tiếng Việt … `11` Khác (11) |
| `confidenceLevel` | `01` Gốc điện tử · `02` Số hóa · `03` Hỗn hợp |
| `format` | `01` Tốt · `02` Bình thường · `03` Hỏng — gói mẫu dùng mã cho cả SIP |
| `riskRecoveryStatus` | `01` Đã dự phòng · `02` Chưa dự phòng |
| `typeName` | `01` Nghị quyết … `32` Khác (32) |
| `source` | `0` Văn bản đi · `1` Văn bản đến |
| `purpose` | `01` Cá nhân · `02` Công vụ · `03` Công vụ đặc biệt |
| `feeObjectType` | `01` Học sinh/sinh viên… … `10` Khác (10) |
| `typePic` | `01` Phim âm bản · `02` Ảnh |
| `colour` | `01` Màu · `02` Đen trắng |
| `typeMedia` | `01` Ghi âm · `02` Ghi hình (đúng chữ thông tư) |

`SortOrder` của mã = thứ tự trong nhóm, từ `1`.

## Seeder

`SeedPackaging` chạy sau `SeedRolePermission` mỗi lần khởi động; dữ liệu ở `PackagingSeedData`, logic ở `SeedPackaging`:

1. Thêm `PackageCodeValue` có `(CodeGroup, Code)` chưa có.
2. Thêm `PackageSchema` có `(PackageType, MetadataType)` chưa có → `SaveChanges` để có `Id`.
3. Mỗi schema: thêm `PackageSchemaField` có `ElementName` chưa có, `SortOrder` = vị trí trong danh sách.
4. Có thêm gì → một `AuditLog` `Khởi tạo cấu hình đóng gói: x mã, y schema, z trường` (`Create`, nguồn `SeedPackaging`).

**Chỉ thêm cái thiếu, không sửa bản ghi đã có** (người dùng có thể đã chỉnh tay). Muốn đổi dữ liệu đã seed thì sửa thẳng DB hoặc
chờ API ghi; sửa `PackagingSeedData` không tác động lên DB đang chạy.

⚠️ Dòng bị xoá mềm không tính là "đã có": khởi động lại sẽ seed bù một dòng mới (unique index có filter cho phép).

## API — `api/packaging/config`

Cả ba cần `Function.Packaging.Config.View` (một key cho xem và tải Excel, giống `DocumentType`); không ghi gì nên không có audit.

| Endpoint | Vào | Ra |
|---|---|---|
| `GET dropdown` | — | Loại gói theo thứ tự `PACKAGE_TYPES`, mỗi loại `{ packageType, code, name, schemas: [{ packageSchemaId, packageType, metadataType, name }] }`; loại không có schema thì bỏ |
| `GET paging` | `packageType` (bắt buộc), `metadataType` (bỏ trống = mọi loại), `keyword` (khớp `DisplayName` hoặc `ElementName`), `pageSize` (`-1` = hết), `pageNumber` | `{ items, totalItems }`; sắp `MetadataType` → `SortOrder` → `Id`; mỗi dòng kèm `codes: [{ code, name }]` (rỗng nếu không phải trường mã) |
| `GET excel-template` | `packageType`, `metadataTypes` lặp lại (bỏ trống = mọi loại, trùng được gộp) | File `Mau metadata <code>_template.xlsx`, mỗi schema một sheet tên `Name`, dòng 1 = `DisplayName` theo `SortOrder` |

| Lỗi | Mã |
|---|---|
| `packageType` ngoài `1`–`5` (cả `paging` và `excel-template`) | `701` |
| `metadataType` / một phần tử `metadataTypes` ngoài `1`–`4` | `702` |
| `excel-template`: các schema đã chọn không còn trường nào | `703` |

- Kiểm `701` / `702` ở `PackageSchemaPolicy` (domain). Loại gói hợp lệ mà không có trường thì `paging` trả rỗng, **không** báo lỗi.
- `paging` chạy 3 câu: đếm, lấy trang, rồi **một** câu lấy mã cho mọi `CodeGroup` trong trang (không truy vấn theo từng dòng).
- Schema không có trường bị bỏ khỏi file Excel; tiêu đề cột là tên tiếng Việt, tên thẻ xem ở `paging`. Chưa đánh dấu cột bắt buộc
  (để tới khi chốt bước kiểm tra metadata).

## Đã kiểm thực tế

Ngày 2026-10-25 trên DB dev (`SO_HOA_DB`, SQL Server Docker), migration `Init_Packaging`:

- Migration chỉ tạo schema `packaging`, 3 bảng, 4 index (3 unique có filter `[Deleted]=(0)`), FK `PackageSchemaField → PackageSchema`
  (`NO_ACTION`); `has-pending-model-changes` → "No changes".
- 20 schema, mỗi cặp một dòng; số trường từng schema khớp bảng trên; `SortOrder` liên tục `1..n`, không trùng `ElementName`.
- Mọi `CodeGroup` trên trường đều có mã; `AllowMultipleValues` chỉ ở 12 dòng `language`; 4 trường `CONDITIONAL` đúng điều kiện.
- `SIP_hoso` văn bản có `docCode`, không `arcDocCode`; DIP văn bản không `organName`; `AIP_tailieu` cấp gói không `totalDoc`.
- Chạy app lần 2: không thêm dòng, không thêm audit. Xoá mềm một trường rồi chạy lại: seed bù đúng 1 dòng, audit "0 mã, 0 schema,
  1 trường" (đã dọn dữ liệu thử).

API, gọi thật bằng `admin` / `viewer` cùng ngày:

- `dropdown`: 5 loại đúng thứ tự, mỗi loại 4 schema đúng tên. `paging` `SIP_hoso` `pageSize=-1`: 76 dòng đúng thứ tự;
  `metadataType=2` chỉ 21 trường văn bản; `keyword=docCode` và `keyword=Mã lưu trữ` đều ra đúng `docCode`; `maintenance` 7 mã đúng
  thứ tự, `title` không có mã, `language` cho chọn nhiều; trang 2 (`pageSize=5`) không trùng trang 1, `totalItems` vẫn 76.
- `packageType=0` / `6` → `701`, `metadataType=9` → `702` ở cả `paging` lẫn `excel-template`; xoá mềm hết trường "Phim âm bản, ảnh" →
  `excel-template` chỉ loại đó → `703`, `paging` trả rỗng, file đủ loại còn 3 sheet (đã khôi phục).
- Excel `SIP_hoso`: 4 sheet đúng tên, thứ tự, số cột 18 / 21 / 18 / 19, dòng 1 in đậm + cố định; `metadataTypes=1&metadataTypes=1`
  → 1 sheet. Excel mẫu cũ của `DocumentType` vẫn tải được.
- Không token → `401`; `viewer` (không có quyền) → `403` cả 3 API; Swagger nhóm `packaging` đủ 3 route.

Thời gian (máy dev, SQL Server trong Docker, build Debug, đo từ client, 5 lần sau khi warm-up): `dropdown` 330–380 ms, `paging` 590–660 ms,
`excel-template` ~400 ms. ⚠️ Không đạt mục tiêu 30 / 50 / 300 ms của bản task, nhưng nền của môi trường cũng vậy: API sẵn có
`document-type/dropdown` 380–450 ms, `user/me` 220–350 ms. Thời gian gần như tỉ lệ với số lượt đi – về DB (`[Permission]` truy vấn quyền
mỗi request + 1 / 3 câu của API), nên phải đo lại trên môi trường deploy trước khi kết luận chậm.

## 🔬 Chưa thi công

API ghi cấu hình, phiên kiểm tra metadata, đóng gói ZIP / METS / EAD / PREMIS.

> **Tiếp:** [17-thong-ke-qa.md](17-thong-ke-qa.md) — thống kê QA theo trường, người nhập, tỉ lệ đúng, dòng thời gian văn bản.
