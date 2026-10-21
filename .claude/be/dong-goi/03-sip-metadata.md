# Chuẩn SIP — metadata, bảng mã, mimetype, tên ZIP

> **Phần 3/8** · trước: [02-sip-cau-truc-mets.md](02-sip-cau-truc-mets.md) · mục lục: [README.md](README.md)

## 4. `EAD.xml` — metadata cấp gói

### 4.1. `SIP_hoso` — 18 trường, mô tả một hồ sơ

```xml
<simpledc>
  <fileCode/><title/><maintenance/><mode/><language/><startDate/><endDate/><keyword/>
  <totalDoc/><numberOfPaper/><numberOfPage/><format/><inforSign/><confidenceLevel/>
  <paperFileCode/><riskRecovery/><riskRecoveryStatus/><description/>
</simpledc>
```

| Trường | Tên tiếng Việt | Kiểu | Dài | Quy tắc |
|---|---|---|---|---|
| `fileCode` | Mã hồ sơ | String | 100 | Mã định danh cơ quan/tổ chức/cá nhân + năm hình thành hồ sơ + số và ký hiệu hồ sơ |
| `title` | Tiêu đề hồ sơ | String | 1000 | |
| `maintenance` | Thời hạn lưu trữ | String | 100 | `01`–`07`; **nguồn nộp và sưu tầm chỉ nhận `01` (vĩnh viễn)** |
| `mode` | Chế độ sử dụng | String | 30 | `01` công khai · `02` có điều kiện · `03` mật |
| `language` | Ngôn ngữ | String | 100 | `01`–`11`, **chọn một hoặc nhiều** |
| `startDate` / `endDate` | Thời gian bắt đầu / kết thúc | Date | | `DD/MM/YYYY` |
| `keyword` | Từ khóa (nếu có) | String | 100 | |
| `totalDoc` | Tổng số tài liệu trong hồ sơ | Number | 10 | văn bản + tài liệu kỹ thuật + âm bản/ảnh + ghi âm/phim |
| `numberOfPaper` | Số lượng tờ | Number | 10 | dành riêng tài liệu giấy số hóa — **xem ⚠️ dưới** |
| `numberOfPage` | Số lượng trang | Number | 10 | |
| `format` | Tình trạng vật lý (nếu có) | String | 50 | |
| `inforSign` | Ký hiệu thông tin (nếu có) | String | 30 | |
| `confidenceLevel` | Mức độ tin cậy (nếu có) | String | 40 | `01` gốc điện tử · `02` số hóa · `03` hỗn hợp |
| `paperFileCode` | Mã hồ sơ gốc giấy (nếu có) | String | 100 | `[Mã cơ quan lưu trữ].[Số kho/giá/hộp].[Số hồ sơ giấy]` — **bắt buộc với hồ sơ số hóa** |
| `riskRecovery` | Chế độ dự phòng | Boolean | 1 | `1` có · `0` không |
| `riskRecoveryStatus` | Tình trạng dự phòng | String | 2 | `01` đã dự phòng · `02` chưa — **bắt buộc khi `riskRecovery=1`** |
| `description` | Ghi chú | String | 2000 | tên người lập hồ sơ + nội dung cần làm rõ |

⚠️ **Bản thân TT05 tự mâu thuẫn ở đây, tới hai lần.** Khung XML trang 212 liệt kê **18** phần tử, có
`numberOfPaper`; nhưng bảng mô tả chi tiết trang 212–217 chỉ đánh số **17**, nhảy từ `totalDoc` (#9) sang
`numberOfPage` (#10). Tệ hơn, **`EAD.xsd` ở mục 4 của chính Phụ lục III chỉ khai 16 phần tử** — thiếu hẳn
`riskRecovery` và `riskRecoveryStatus` mà khung XML có. Gói mẫu có đủ **18** ở cả schema lẫn dữ liệu, nên
**theo gói mẫu: sinh đủ 18**. Đừng "sửa" bộ sinh cho khớp bảng hay cho khớp schema của thông tư.

### 4.2. `SIP_tailieu` — 5 trường, chỉ là đầu lô

| Trường | Tên tiếng Việt | Kiểu | Dài | Quy tắc |
|---|---|---|---|---|
| `fileCode` | Mã gói tin SIP_tailieu | String | 100 | Mã định danh cơ quan + năm hình thành tài liệu + **số lần nộp lưu (2 ký tự, `01`)** + **số thứ tự tài liệu trong lần nộp (7 ký tự, `0000001`)** |
| `title` | Tiêu đề gói tin | String | 1000 | tóm tắt nội dung và thời gian tài liệu trong gói |
| `source` | Nguồn gốc | Boolean | 1 | `0` = văn bản đi · `1` = văn bản đến |
| `totalDoc` | Tổng số tài liệu trong gói tin | Number | 10 | |
| `description` | Ghi chú | String | 2000 | |

`source` **chỉ có ở `SIP_tailieu`** — không loại gói nào khác trong TT05 có trường này. Đây là dấu hiệu nhận
dạng chắc chắn nhất khi phải đoán một gói lạ thuộc phụ lục nào.

## 5. Metadata từng tài liệu

Mỗi tài liệu một file XML trong `rep1/metadata/descriptive/`, tên theo `[Tiêu chuẩn]_[Loại tài liệu]_[Tên file]`.

### 5.1. `EAD_doc.xml` — tài liệu văn bản, 21 trường (cả hai biến thể SIP)

| Trường | Tên tiếng Việt | Kiểu | Dài | Quy tắc |
|---|---|---|---|---|
| `docId` | Mã định danh tài liệu | String | 25 | vd `H49.64.33.2001.160.1` |
| `docCode` | Mã lưu trữ của tài liệu | String | 100 | `fileCode` + số thứ tự tài liệu trong hồ sơ, **số thứ tự 7 ký tự** (`0000001`) |
| `maintenance` | Thời hạn lưu trữ | String | 100 | `01`–`07`; **lấy mặc định từ `maintenance` của hồ sơ** |
| `typeName` | Tên loại tài liệu | String | 10 | `01`–`32`, xem §7 |
| `codeNumber` | Số của tài liệu | String | 11 | nếu có |
| `codeNotation` | Ký hiệu của tài liệu | String | 30 | nếu có |
| `issuedDate` | Ngày tháng năm tài liệu | Date | | `DD/MM/YYYY` |
| `organName` | Cơ quan, tổ chức, cá nhân ban hành | String | 200 | |
| `subject` | Trích yếu nội dung | String | 500 | |
| `language` | Ngôn ngữ | String | 100 | `01`–`11`, một hoặc nhiều |
| `numberOfPage` | Số lượng trang | Number | 4 | |
| `inforSign` | Ký hiệu thông tin (nếu có) | String | 30 | |
| `keyword` | Từ khóa (nếu có) | String | 100 | ghi từ mang trọng tâm thông tin |
| `mode` | Chế độ sử dụng | String | 20 | `01`/`02`/`03` |
| `confidenceLevel` | Mức độ tin cậy (nếu có) | String | 30 | `01`/`02`/`03` |
| `autograph` | Bút tích (nếu có) | String | 2000 | |
| `format` | Tình trạng vật lý (nếu có) | String | 50 | |
| `riskRecovery` / `riskRecoveryStatus` | Dự phòng | Boolean / String | 1 / 2 | như §4.1 |
| `process` | Quy trình xử lý (nếu có) | Boolean | 1 | `1` = có file luồng xử lý đi kèm. **Bắt buộc với tài liệu điện tử xử lý trên Hệ thống**, áp dụng cho `confidenceLevel` `01` và `03` |
| `description` | Ghi chú | String | 500 | |

### 5.2. `EAD_pic.xml` — phim âm bản / ảnh

`docCode` · `maintenance` · `typePic` (`01` phim âm bản, `02` ảnh) · `archivesNumber` (Số lưu trữ đặc thù, 50) ·
`inforSign` · `eventName` (Tên sự kiện, 500) · `imageTitle` (Tiêu đề phim/ảnh, 500) · `photographer` (Tác giả,
300) · `photoPlace` (Địa điểm chụp, 300) · `photoTime` (Thời gian chụp, date) · `colour` (`01` màu, `02` đen
trắng) · `filmSize` (Cỡ phim/ảnh, 30) · `docAttached` (Tài liệu đi kèm, bool) · `mode` · `format` ·
`riskRecovery` · `riskRecoveryStatus` · `description`

### 5.3. `EAD_media.xml` — ghi âm / ghi hình

`docCode` · `maintenance` · `typeMedia` (`01` âm thanh, `02` video) · `archivesNumber` · `inforSign` ·
`eventName` · `movieTitle` (500) · `recorder` (Tác giả, 300) · `recordPlace` (300) · `recordDate` · `language` ·
`playTime` (Thời lượng, 8) · `docAttached` · `mode` · `quality` (Chất lượng, 50 — "bình thường, mờ, lẫn tạp âm,
tiếng lúc to lúc nhỏ") · `format` · `riskRecovery` · `riskRecoveryStatus` · `description`

Mọi schema đều lấy `<simpledc>` làm phần tử gốc (complexType `elementContainer`, group `elementsGroup`) và
import `xml.xsd`.

## 6. Tài liệu liên kết — holey file

Dùng khi nguồn nộp **đã nộp tài liệu đó ở lần trước** và không nộp lại nội dung file.

| | `SIP_hoso` (trang 211) |
|---|---|
| Đuôi file | `.fetch.txt`; tên đặt theo `Tên tài liệu liên kết.Định dạng tài liệu lưu trữ` → `File2.doc.fetch.txt` |
| Nội dung | chỉ `{Định danh tài liệu}` — văn bản → `Mã định danh tài liệu`; phim âm bản/ảnh → tiêu đề; phim/âm thanh → tiêu đề |
| Khai trong METS | `fileSec/fileGrp[@USE='Holeyfile']/file`, nối bằng `structMap/div/div/div[@LABEL="MetadataLink/Holey"]` |

⚠️ Hai chỗ chính bản TT05 tự lệch, **đừng lặng lẽ chuẩn hóa lại**: phụ lục viết `@USER='Holeyfile'` trong khi
mọi chỗ khác dùng `@USE`; và ví dụ `structMap` của `SIP_tailieu` (trang 267) đã dùng `LABEL="MetadataLink/Holey"`
mặc dù Phụ lục IV **không hề định nghĩa** cơ chế tài liệu liên kết.

## 7. Bảng mã dùng chung cho mọi loại gói

**`typeName`** — `01` Nghị quyết · `02` Quyết định · `03` Chỉ thị · `04` Quy chế · `05` Quy định · `06` Thông cáo ·
`07` Thông báo · `08` Hướng dẫn · `09` Chương trình · `10` Kế hoạch · `11` Phương án · `12` Đề án · `13` Dự án ·
`14` Báo cáo · `15` Tờ trình · `16` Giấy ủy quyền · `17` Phiếu gửi · `18` Phiếu chuyển · `19` Phiếu báo ·
`20` Biên bản · `21` Hợp đồng · `22` Công văn · `23` Công điện · `24` Bản ghi nhớ · `25` Bản thỏa thuận ·
`26` Giấy mời · `27` Giấy giới thiệu · `28` Giấy nghỉ phép · `29` Thư công · `30` Bản đồ · `31` Bản vẽ kỹ thuật ·
`32` Khác

**`language`** — `01` Tiếng Việt · `02` Anh · `03` Pháp · `04` Nga · `05` Trung · `06` Việt Anh · `07` Việt Nga ·
`08` Việt Pháp · `09` Hán Nôm · `10` Việt Trung · `11` Khác

**`maintenance`** — `01` Vĩnh viễn · `02` 70 năm · `03` 50 năm · `04` 30 năm · `05` 20 năm · `06` 10 năm · `07` Khác

**`mode`** — `01` Công khai · `02` Sử dụng có điều kiện · `03` Mật ·
**`confidenceLevel`** — `01` Gốc điện tử · `02` Số hóa · `03` Hỗn hợp ·
**`format`** (cách AIP diễn giải) — `01` Tốt · `02` Bình thường · `03` Hỏng ·
**`RECORDSTATUS`** — `NEW` · `SUPPLEMENT` · `REPLACEMENT` · `TEST` · `OTHER` ·
**`csip:OAISPACKAGETYPE`** — `SIP` · `AIP` · `DIP`

## 8. Mimetype và định dạng

| Code | Loại | Extension → mimetype |
|---|---|---|
| DOC | Văn bản | `.txt`→`text/plain` · `.rtf` (1.8/1.9.1)→`application/rtf` · `.docx`→`…wordprocessingml.document` · **`.pdf/a` hai lớp**→`application/pdf` · `.doc`→`application/msword` · `.odt` (1.2)→`…opendocument.text` |
| OTHER | Bảng tính | `.csv`→`text/csv` · `.xlsx`→`…spreadsheetml.sheet` · `.xls`→`application/vnd.ms-excel` · `.ods`→`…opendocument.spreadsheet` |
| OTHER | Trình chiếu | `.htm`→`text/html` · `.pptx`→`…presentationml.presentation` · `.ppt`→`application/vnd.ms-powerpoint` · `.odp`→`…opendocument.presentation` |
| PIC | Hình ảnh | `.jpeg`/`.jpg`→`image/jpeg` · `.gif` (89a)→`image/gif` · `.tif`/`.tiff`→`image/tiff` · `.png`→`image/png` |
| MEDIA | Video | MPEG-1/2/4→`video/mpeg` · `.avi`→`video/x-msvideo` · `.wmv`→`video/x-ms-wmv` · `.mov`/`.qt`→`video/quicktime` |
| MEDIA | Âm thanh | `.mp3`→`audio/mpeg` · `.wma`→`audio/x-ms-wma` · `.aac`→`audio/aac` |

Định dạng nội dung trong gói: văn bản và tài liệu kỹ thuật là **PDF/A hai lớp**; âm bản/ảnh là JPEG; ghi âm,
ghi hình là MPEG-4, MP3, avi, wma, wmv.

⚠️ **PDF/A không phải cổng chặn của gói.** Chính bảng trên nhận `.txt`, `.rtf`, `.doc`, `.docx`, `.odt` bên
cạnh `.pdf/a`, nên gói vẫn hợp lệ về cấu trúc khi tệp văn bản không phải PDF/A — ràng buộc PDF/A của Điều 8
gắn vào **khâu số hóa từ bản giấy**, không gắn vào khâu đóng gói. Trong luồng đóng gói của dự án, sinh PDF/A
là **tuỳ chọn**; mức tuân thủ, bẫy thứ tự với ký số và điều kiện bật ở [08-pdfa.md](08-pdfa.md).

## 9. Quy tắc đặt tên file nén ZIP

> `Tên ZIP = [Số thứ tự gói trong lần nộp] + [ID gói]`

Số thứ tự do người dùng tự đánh theo số Ả-rập; ID gói lấy từ `mets/@OBJID` của phần tử gốc METS.xml. Câu chữ
giống hệt nhau ở cả hai phụ lục (trang 238 và trang 291).

⚠️ **Chỉ SIP có quy tắc đặt tên ZIP.** Phụ lục I, II và V đều nói gói được nén ZIP khi truyền nhận nhưng
**không quy định tên file**.

🎯 **Gói mẫu làm khác, và chốt theo gói mẫu:** tên ZIP là **một GUID thường viết chữ nhỏ**,
`b6acafed-a809-4b78-ae9f-0373d50d52f0.zip` — **không có tiền tố `uuid-`, không có số thứ tự, và khác hẳn
`OBJID`** của gói (`uuid-0F203B84-…`). Thư mục gói mang tên `OBJID` **nằm bên trong** file ZIP, chỉ thấy sau
khi giải nén. Nghĩa là một gói có **hai định danh khác nhau**: GUID của file ZIP và `OBJID` của gói bên trong.

> **Tiếp:** [04-sip-goi-mau.md](04-sip-goi-mau.md) — gói mẫu chuẩn: 15 điểm chốt theo gói mẫu và 3 lỗi đừng học theo.
