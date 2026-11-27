# amdSec, structMap, PREMIS của AIP và DIP — rà từng phần tử

> **Phần 11/11** · trước: [10-yeu-cau-tung-mau.md](10-yeu-cau-tung-mau.md) · mục lục: [README.md](README.md)
>
> Rà lượt 4, ngày 2026-11-27. Mỗi dòng ghi nguồn: số trang thông tư, `GM` (gói mẫu `goi-mau/`), hoặc "PM chốt kỹ
> thuật" (thông tư và gói mẫu đều không nói). Chỗ các nguồn lệch nhau thì xử theo thứ tự ở [10 §1](10-yeu-cau-tung-mau.md).
> SIP không có PREMIS và có `amdSec` rỗng (GM), nên file này chỉ nói về AIP_hoso, AIP_tailieu và DIP.

## 1. `amdSec` — 6 bảng giống nhau

Đã so các bảng: AIP_hoso gốc tr.43–45 và rep1 tr.57–59; AIP_tailieu gốc tr.120–122 và rep1 tr.134–135; DIP gốc
tr.302–303 và rep1 tr.313–315. Nội dung giống nhau, chỉ khác chỗ ngắt trang và chữ "dữ liệu đặc tả" / "siêu dữ liệu".

| Phần tử / thuộc tính | Giá trị phát | Nguồn |
|---|---|---|
| `amdSec@ID`, `digiprovMD@ID`, `mdRef@ID` | `uuid-{UUID HOA}` | bảng |
| `digiprovMD@STATUS` | `CURRENT` | chỉ ví dụ (bảng không nói) |
| `mdRef@LOCTYPE` | `URL` | bảng ("luôn") |
| `mdRef@xlink:type` | `simple` | ví dụ + GM (`mdRef` của `dmdSec`) |
| `mdRef@MDTYPE` | `PREMIS` | bảng (ví dụ gốc ghi `OTHER`, ví dụ rep1 ghi `PREMIS`, nên theo bảng) |
| `mdRef@xlink:href` | gốc: `metadata/preservation/PREMIS.xml`; rep1: `metadata/preservation/PREMIS_rep1.xml` (tính từ thư mục rep1) | bảng + tên file theo cây tr.36 |
| `mdRef@MIMETYPE` | `application/xml` | GM (C13). Danh sách mimetype mục 5 không có XML; `text/xml` chỉ là ví dụ |
| `mdRef@SIZE` / `CREATED` / `CHECKSUM` / `CHECKSUMTYPE` | byte thật; giờ có mili giây + `+07:00`; SHA-256 viết HOA; `SHA-256` | bảng + GM |

## 2. `structMap` METS gốc

Nguồn: AIP_hoso tr.48–51, AIP_tailieu tr.125–129, DIP tr.306–309. GM (SIP) cho khuôn chung.

| Phần tử | Giá trị | Nguồn |
|---|---|---|
| `structMap` | `ID` uuid, `TYPE="PHYSICAL"`, `LABEL="CSIP"` | bảng (`ID`, `LABEL`); `TYPE` từ ví dụ + GM |
| `div` cấp 1 | `ID`, `LABEL` = `OBJID` của gói | bảng ("map với ID của file METS gói"); GM |
| `div LABEL="Metadata"` | `ID`, `DMDID` = ID `dmdSec` của `EAD.xml`, `ADMID` = ID `amdSec` | bảng tr.49, 126 |
| `div LABEL="Schemas"` + `fptr FILEID` | ID `fileGrp USE="Schemas"` | bảng + GM |
| `div LABEL="Representations/rep1"` + `mptr` | `xlink:type="simple"`, `xlink:href="representations/rep1/METS.xml"`, `xlink:title` = ID `fileGrp` rep1, `LOCTYPE="URL"` | bảng + GM |
| `div LABEL="Documentation"` + `fptr` | **chỉ DIP** (có file xác thực, C12): `FILEID` = ID `fileGrp USE="Documentation"` (USE theo tr.304) | bảng (không bắt buộc) |

Lưu ý: bảng DIP tr.307 ghi **ngược** mô tả của 2 dòng 7–8 ("ADMID: ID của `dmdSec` trỏ EAD", "DMDID: ID của `amdSec`
trỏ PREMIS"). Bảng AIP (tr.49, 126) ghi đúng chiều, và `mets1_12.xsd` cũng định nghĩa DMDID trỏ `dmdSec`, ADMID trỏ
phần tử trong `amdSec`. Vì vậy: **DMDID = dmdSec, ADMID = amdSec**. Đừng chép theo chữ của tr.307.

Bẫy: `ADMID` trỏ vào đâu. Bảng (tr.49, 64, 126, 307, 319) ghi "ID của thẻ `amdSec`". Chú thích trong `mets1_12.xsd`
và ví dụ rep1 (tr.67) lại trỏ ID của `digiprovMD`. Theo thứ tự trọng tài (bảng đứng trên `.xsd` và ví dụ), ta chọn
**ID của `amdSec`**. Cả hai cách đều qua kiểm schema, vì ADMID kiểu `IDREFS` chỉ cần trỏ một ID có thật.

## 3. `structMap` METS rep1

Nguồn: AIP_hoso tr.62–68, AIP_tailieu tr.139–145, DIP tr.317–321; SIP_hoso tr.206–210 (để so với GM).

| Phần tử | Giá trị | Nguồn |
|---|---|---|
| `structMap` | như METS gốc | bảng + GM |
| `div` cấp 1 | `ID`, `TYPE` = `NORMALIZED` (AIP) / `ORIGINAL` (DIP, SIP), `LABEL="rep1"` | `LABEL`: bảng ("mặc định là rep1", tr.63, 318) + GM; ví dụ ghi uuid nên bỏ. `TYPE`: chỉ có trong ví dụ (tr.67, 143, 321) và GM |
| `div LABEL="Metadata"` + `fptr FILEID` | **AIP + DIP**: `FILEID` = ID của `mdRef` trong `amdSec` rep1 | bảng tr.63, 318 (bắt buộc). Ví dụ không có div này, nên theo bảng |
| `div LABEL="MetadataLink"` > mỗi tài liệu 1 `div LABEL="MetadataLink/File"` | `ID`, `DMDID` = ID `dmdSec` của `EAD_*` tài liệu đó, `ADMID` = ID `amdSec` rep1 (AIP + DIP; SIP không có), `fptr FILEID` = ID `file` của tài liệu | bảng + GM |
| `div LABEL="MetadataLink/Holey"` | không phát (chỉ dùng khi có `.fetch.txt`, ngoài phạm vi) | bảng |
| `div LABEL="AttachmentFile"` | **không phát** khi không có tài liệu đính kèm | bảng ghi "bắt buộc" cả ở SIP_hoso tr.206–210, nhưng GM (SIP_hoso) không có div này. Đọc là "bắt buộc khi có đính kèm" |
| `div LABEL="Data"` + `fptr` | `FILEID` = ID `fileGrp USE="Data"` | bảng + GM |

Thứ tự các div: `Metadata` → `MetadataLink` → `Data`. GM xếp `MetadataLink` trước `Data`; `Metadata` đứng đầu theo
thứ tự dòng của bảng.

## 4. PREMIS — 3 bảng giống nhau

Đã so 3 bảng: AIP_hoso tr.90–101, AIP_tailieu tr.162–173, DIP tr.339–348. Dòng bắt buộc của cả 3 bảng như nhau:
`premis@xmlns`, `object@xmlID`, `objectIdentifier` (`Type` + `Value`), loại đối tượng, `event` (`eventIdentifier`,
`eventType`, `eventDateTime`), `agent` (`agentIdentifier` `Type` + `Value`, `agentName`, `agentType`). Mọi dòng khác
đều ghi "không bắt buộc".

| Phần tử | Giá trị | Nguồn |
|---|---|---|
| `premis` | `xmlns:premis="http://www.loc.gov/premis/v3"`, `xmlns:xsi`, `xmlns:xlink`, `xsi:schemaLocation` như ví dụ, `version="3.0"` | bảng + ví dụ tr.91 |
| Loại đối tượng | thuộc tính `xsi:type` trên `object`, **không** phát thẻ `<objectCategory>` | người dùng 27/11 (Q6). Bảng đòi `objectCategory`, nhưng PREMIS v3 không có thẻ đó; ví dụ tr.94 dùng `xsi:type="file"` |
| `PREMIS.xml` (gốc, "thông tin bảo quản của gói" tr.34) | 1 `object xsi:type="intellectualEntity"`: `objectIdentifierType` `OBJID`, value = OBJID của gói | PM chốt kỹ thuật (bảng chỉ ví dụ File / Doc / Pic / Media cho file) |
| `PREMIS_rep1.xml` ("của bản đại diện" tr.36) | mỗi file data 1 `object xsi:type="file"`: `objectIdentifierType` `Doc` / `Pic` / `Media` theo loại metadata, value = ID `file` trong METS rep1 | `Type`: bảng (ví dụ); value: PM chốt kỹ thuật |
| `object@xmlID` | `uuid-{UUID HOA}` | bảng ("hệ thống sinh"), khuôn ID của METS |
| `objectCharacteristics` của file | `compositionLevel` `0`; `fixity` (`messageDigestAlgorithm` `SHA-256`, `messageDigest` = SHA-256 **chữ thường** như ví dụ); `size`; `format/formatDesignation/formatName` = mimetype của file | ví dụ tr.95–96 (bảng không nói); `formatName` = mimetype: PM chốt kỹ thuật (không có công cụ tra PRONOM, nên bỏ `formatRegistry`) |
| `signatureInformation`, `preservationLevel`, `storage`, `originalName` | không phát | bảng "không bắt buộc"; bảng không có nguồn để điền chữ ký |
| `event` | 1 event mỗi file PREMIS: `eventIdentifierType` `UUID`, value uuid chữ thường như ví dụ; `eventDateTime` có mili giây + `+07:00` | bảng tr.97 (ví dụ `UUID`); ví dụ dòng 23 ghi `local` nên theo bảng |
| `eventType` | AIP: `transfer` (số hoá, tr.97, 169). DIP: `dissemination` | AIP theo bảng; DIP theo người dùng 27/11 (Q7): bảng DIP tr.345 chép y AIP, không có trường hợp tạo DIP |
| `linkingAgentIdentifier` | `Type` `LOCAL`, `Value` = `agentIdentifierValue`, `Role` `executing program` | bảng tr.98–99 ("liên kết đến giá trị tác nhân hiện có"; vai trò lấy trong danh sách) |
| `agent` | `agentIdentifierType` `LOCAL`; `agentIdentifierValue` `Thi công số hoá {SoftwareVersion}`; `agentName` `Thi công số hoá`; `agentType` `Software` | ví dụ tr.101 (`E-ARK Web 0.9.3` / `E-ARK Web` / `Software`) + C10 |

Bẫy: chuỗi `E-ARK Web 0.9.3 (task: SIPtoAIP_hosoReset)` chỉ có ở `linkingAgentIdentifierValue` trong ví dụ event
(tr.100), và **không khớp** với `agentIdentifierValue` của agent (tr.101). Bảng ghi giá trị liên kết phải bằng giá
trị của agent đang có. Vì vậy **không thêm "(task: …)"** (plan C10 đã sửa theo).

Lưu ý: chưa có file `premis.xsd` v3 trong repo. Khi Ánh làm Task 66, kiểm PREMIS sinh ra bằng bản schema chính thức
(link ở `schemaLocation`), chỉ dùng để thử, không đưa vào gói. Nếu schema đòi thêm thẻ nào thì báo PM, đừng tự thêm.

> **Tiếp:** hết — quay lại [README.md](README.md).
