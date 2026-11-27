# Gói mẫu SIP_hoso — phân tích từng file (bản sao trong repo)

> **Phần 9/10** · trước: [08-pdfa.md](08-pdfa.md) · mục lục: [README.md](README.md)

Bổ sung cho [04-sip-goi-mau.md](04-sip-goi-mau.md) (15 điểm chốt + 3 lỗi). File này ghi **mọi chi tiết đo được**
trên gói mẫu để bộ sinh gói bắt chước đúng từng byte có nghĩa. Đo lại ngày 2026-11-27 bằng script, không chép từ
tài liệu cũ.

## 1. Bản sao trong repo — đọc ở đây, không cần máy người dùng

Gói gốc nằm ở `C:\Users\Admin\Downloads\b6acafed-a809-4b78-ae9f-0373d50d52f0\` (chỉ máy người dùng có). Bản sao
trong repo: [goi-mau/](goi-mau/) — giữ nguyên cây thư mục, **đủ mọi file XML / XSD** (2 METS, 1 EAD, 137
EAD_DOC, 8 schema), PDF chỉ giữ **1 file** `H49.64.33.2001.160.1.pdf` để thử bộ soi PDF (136 PDF còn lại 113,7 MB
không đưa vào git). Danh sách đủ 285 file kèm kích thước + SHA-256: [goi-mau/danh-sach-file.tsv](goi-mau/danh-sach-file.tsv).

Lưu ý: Vì thiếu 136 PDF, chạy kiểm checksum trên bản sao sẽ báo thiếu file ở `data/` — đó là đúng. Muốn kiểm đủ 284
tham chiếu thì chạy trên bản gốc ở máy người dùng.

## 2. Số đo tổng

| Đo | Kết quả |
|---|---|
| Tổng file | 285 = 140 `.xml` + 137 `.pdf` + 8 `.xsd` |
| Tham chiếu có CHECKSUM + SIZE | 284 (METS gốc 10: 1 `mdRef` + 8 schema + 1 rep1/METS · METS rep1 274: 137 `mdRef` + 137 `file`) — **284/284 đúng** |
| ID trong 2 METS | `uuid-{UUID}` × 429 · `ID-{UUID}` × 146 (chỉ phần tử `<file>`); UUID **viết hoa** |
| `EAD.xml` ↔ dữ liệu | `totalDoc` 137 = số PDF · `numberOfPage` 233 = tổng `numberOfPage` của 137 EAD_DOC · `numberOfPaper` 141 |
| PDF | 137/137 `%PDF-1.7`, **PDF/A-2b** (`pdfaid:part=2`, `conformance=B`), Producer `pikepdf 9.9.0; modified using iTextSharp`, tổng 114,7 MB |
| Chữ ký số trong PDF | **137/137 đã ký**: `/Type/Sig`, `/SubFilter/ETSI.CAdES.detached`, `/M(D:20260416015634+07'00')`, `/Reason` "Ký số tài liệu" |

Nghĩa là gói mẫu đi đúng thứ tự [08-pdfa.md](08-pdfa.md) chốt: **PDF/A trước, ký sau**, rồi mới đóng gói. Gói
không chứa gì chứng minh chữ ký (không PREMIS — SIP không có), chữ ký chỉ nằm trong PDF.

## 3. Định dạng byte của file XML

| File | Khai báo đầu | BOM | Xuống dòng | Thụt lề |
|---|---|---|---|---|
| `METS.xml` (gốc + rep1) | `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>` | không | LF | 4 dấu cách |
| `EAD.xml`, `EAD_DOC_*.xml` | `… standalone="no"?>` | không | LF | 4 dấu cách |
| `*.xsd` | `<?xml version="1.0" encoding="UTF-8"?>` | không | LF, **không** xuống dòng cuối file | 4 dấu cách |

Lưu ý: `SIZE` và `CHECKSUM` tính trên **đúng byte ghi ra đĩa**. Bộ sinh phải ghi file xong (UTF-8 **không BOM** —
`new UTF8Encoding(false)`) rồi mới băm; băm chuỗi trước khi ghi, hoặc để .NET tự chèn BOM, là lệch checksum.

## 4. METS gốc — đủ thuộc tính

`mets`: `xmlns:xsi` · `xmlns:sip` · `xmlns` (METS) · `xmlns:csip` · `xmlns:xlink` (thứ tự khai đúng như vậy) ·
`OBJID="uuid-0F203B84-…"` · `LABEL` = `title` của hồ sơ · `TYPE="Mixed"` · `csip:CONTENTINFORMATIONTYPE="MIXED"` ·
`PROFILE="https://earkcsip.dilcis.eu/profile/E-ARK-CSIP.xml"` · `xsi:schemaLocation` 4 cặp trỏ `schemas/…`.

Lưu ý: Namespace khai `https://DILCIS.eu/…` (chữ hoa) nhưng `schemaLocation` viết `https://dilcis.eu/…` (chữ
thường). Gói mẫu để vậy — **chép nguyên**, đừng "sửa cho khớp".

| Khối | Nội dung đo được |
|---|---|
| `metsHdr` | `CREATEDATE` = `LASTMODDATE` = `2026-06-12T11:45:37.690+07:00`, `RECORDSTATUS="NEW"`, `csip:OAISPACKAGETYPE="SIP"` |
| `agent` | `ROLE="CREATOR" TYPE="OTHER" OTHERTYPE="SOFTWARE"`, `name` = `ArchiveNex`, `note csip:NOTETYPE="IDENTIFICATIONCODE"` = `P_KHCN` (mã phông) |
| `altRecordID` | `TYPE="SUBMISSIONAGREEMENT"` = `H49.64.33.2001.160` (`fileCode`) |
| `dmdSec` | 1 khối, `ID`, `CREATED`, `STATUS="CURRENT"`; `mdRef` có `ID`, `LOCTYPE="URL"`, `MDTYPE="DC"`, `MDTYPEVERSION="1.0.0"`, `xlink:type="simple"`, `xlink:href="metadata/descriptive/EAD.xml"`, `MIMETYPE="application/xml"`, `SIZE`, `CREATED`, `CHECKSUM`, `CHECKSUMTYPE="SHA-256"` |
| `amdSec` | rỗng, chỉ `ID` |
| `fileSec` | có `ID`; `fileGrp USE="Schemas"` 8 file theo thứ tự `DILCISExtensionMETS` · `DILCISExtensionSIPMETS` · `mets1_12` · `xlink` · `ead` · `ead_doc` · `ead_pic` · `ead_media`; `fileGrp USE="Representations/rep1"` 1 file `representations/rep1/METS.xml` (`application/xml`) |
| `file` | `ID="ID-…"`, `MIMETYPE`, `SIZE`, `CREATED`, `CHECKSUM`, `CHECKSUMTYPE`; con `FLocat xlink:type="simple" xlink:href=… LOCTYPE="URL"` |
| `structMap` | `ID`, `TYPE="PHYSICAL" LABEL="CSIP"` → `div LABEL="{OBJID}"` → 3 `div`: `Metadata` (`DMDID`) · `Schemas` (`fptr FILEID` = **ID của fileGrp**, không phải ID file) · `Representations/rep1` (`mptr xlink:href="representations/rep1/METS.xml" xlink:title="{ID fileGrp rep1}" LOCTYPE="URL"`) |

`CREATED` của `file` rep1/METS là `42.883`, muộn nhất gói (các mốc khác từ 37.690 tới 37.915): METS gốc chứa checksum
của rep1/METS nên **rep1/METS phải ghi xong trước** METS gốc. Mọi mốc giờ có mili giây và `+07:00`.

## 5. METS rep1

`mets`: cùng bộ namespace + `PROFILE` CSIP, nhưng `OBJID="rep1"`, `LABEL=""` (rỗng), `TYPE="Mixed"`,
`schemaLocation` trỏ `../../schemas/…`. `metsHdr` như gốc nhưng **không có `altRecordID`**, `agent`
`TYPE="INDIVIDUAL" OTHERTYPE=""`, `name` = `cvlt` (tài khoản đóng gói), note `IDENTIFICATIONCODE` = `P_KHCN`.

| Khối | Nội dung |
|---|---|
| `dmdSec` | **137 khối**, mỗi tài liệu 1 khối, `mdRef xlink:href="metadata/descriptive/EAD_DOC_{docId}.xml"` (đường dẫn tính từ rep1) |
| `amdSec` | rỗng, chỉ `ID` |
| `fileSec` | **1** `fileGrp USE="Data"` gồm 137 `file` `application/pdf`, `FLocat` → `data/{docId}.pdf` |
| `structMap` | `div TYPE="ORIGINAL" LABEL="rep1"` → `div LABEL="MetadataLink"` chứa 137 `div LABEL="MetadataLink/File" DMDID="{dmdSec tài liệu}"` + `fptr FILEID="{ID file PDF}"`; rồi `div LABEL="Data"` + `fptr FILEID="{ID fileGrp Data}"` |

Lưu ý: File `EAD_DOC_*.xml` **không** có trong `fileSec` — chỉ được tham chiếu qua `dmdSec/mdRef`. Đừng thêm nhóm
`Metadata` vào `fileSec`, gói mẫu không có.

## 6. `EAD.xml` và `EAD_DOC_*.xml`

`EAD.xml`: gốc `<simpledc>` **không namespace**, đủ 18 thẻ, thứ tự xáo (`confidenceLevel`, `totalDoc`, `endDate`,
`inforSign`, …). Thẻ không có giá trị vẫn **phát thẻ rỗng tự đóng** `<inforSign/>`, `<description/>`,
`<paperFileCode/>` — không bỏ thẻ.

137 `EAD_DOC`: đúng **một bộ 21 thẻ** và **một thứ tự** giống hệt nhau ở cả 137 file: `issuedDate`,
`confidenceLevel`, `process`, `docId`, `subject`, `inforSign`, `typeName`, `format`, `docCode`, `description`,
`language`, `autograph`, `riskRecoveryStatus`, `codeNumber`, `numberOfPage`, `mode`, `organName`, `codeNotation`,
`keyword`, `maintenance`, `riskRecovery`. Hai thẻ rỗng ở cả 137 file: `process`, `autograph`.

Giá trị gặp: `typeName` {02, 27, 32} · `format` {02} · `language`/`mode`/`maintenance` {01} · `confidenceLevel` {02}
· `riskRecovery` {0} · `riskRecoveryStatus` {02} · `codeNotation` 7 giá trị, 6 có dấu `/` đầu (`/TK`, `/GT/UB`, `/QĐTTLH`…), 1 không (`TL/TA`) — ghi nguyên ô, không tự thêm `/` ·
`docId` `H49.64.33.2001.160.1` … `.137` (STT **không đệm**) · `docCode` `…160.0000001` … `.0000137` (đệm 7, sai năm —
lỗi #1 ở 04).

Lưu ý: `.xsd` dùng `xs:all` nên thứ tự nào cũng hợp lệ, nhưng gói mẫu giữ **một thứ tự cố định** ở cả 137 file ⇒
`SIP_hoso` phát **đúng thứ tự gói mẫu** (gói mẫu thắng, 10 §1); 4 loại còn lại không có gói mẫu ⇒ theo `SortOrder`
(thứ tự `.xsd`). Thẻ có trong seed mà gói mẫu không có (ảnh / media) xếp theo `SortOrder` (plan D5).

## 7. Schema đi kèm

8 file, tên chữ thường. 4 file `ead*.xsd` khai đúng bộ thẻ của `SIP_hoso` (18 / 21 / 18 / 19) — riêng
`ead_doc.xsd`, `ead_pic.xsd`, `ead_media.xsd` khai `arcDocCode` thay `docCode` (lỗi #2 ở 04: sinh gói mới thì
sửa schema thành `docCode`). `DILCISExtensionSIPMETS.xsd` chỉ 4 thuộc tính (`FILEFORMATNAME`, `FILEFORMATVERSION`,
`FORMATREGISTRY`, `FORMATREGISTRYKEY`); `mets1_12.xsd` 135 322 byte, đầu file ghi "METS: Metadata Encoding and
Transmission Standard", giấy phép CC0 của Digital Library Federation. Bộ sinh **chép nguyên byte** 4 file
`mets1_12.xsd`, `xlink.xsd`, `DILCISExtensionMETS.xsd`, `DILCISExtensionSIPMETS.xsd` từ `goi-mau/` làm tài nguyên
nhúng; 4 file `ead*.xsd` **sinh theo bộ trường của đúng loại gói** (seed Task 37), giữ khuôn byte như gói mẫu
(`xs:all`, `xs:string`, thụt 4, không xuống dòng cuối) — plan D6.

> **Tiếp:** [10-yeu-cau-tung-mau.md](10-yeu-cau-tung-mau.md) — bảng yêu cầu 5 loại gói, mỗi dòng ghi nguồn.
