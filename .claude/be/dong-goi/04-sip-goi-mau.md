# Gói mẫu SIP_hoso — chốt theo gói mẫu

> **Phần 4/8** · trước: [03-sip-metadata.md](03-sip-metadata.md) · mục lục: [README.md](README.md)

Các `§` trong file này trỏ về [02-sip-cau-truc-mets.md](02-sip-cau-truc-mets.md) (§1–§3) và
[03-sip-metadata.md](03-sip-metadata.md) (§4–§9). Bản giải nén của gói mẫu:
`C:\Users\Admin\Downloads\b6acafed-a809-4b78-ae9f-0373d50d52f0\`.

## 10. Gói mẫu chuẩn — những gì chốt theo gói mẫu

| | |
|---|---|
| File ZIP | `b6acafed-a809-4b78-ae9f-0373d50d52f0.zip` (~87 MB) |
| Thư mục gói | `uuid-0F203B84-6641-4CDA-9F52-DAAE55FEF6B1` |
| Loại | **`SIP_hoso`** — Phụ lục III |
| Nội dung | 137 tài liệu PDF + 137 `EAD_DOC_*.xml`, hồ sơ `H49.64.33.2001.160` "Tập tờ khai đăng ký kết hôn … phường Hà Tu năm 2001" |
| Phần mềm sinh | `ArchiveNex`, mã phông `P_KHCN`, đóng gói 12/6/2026 |

**Vì sao chắc chắn là `SIP_hoso` chứ không phải `SIP_tailieu`:** `schemas/ead.xsd` khai đúng **18 trường** mở
đầu bằng `fileCode` và có `numberOfPaper` — trùng khít khung `SIP_hoso` ở §4.1; `SIP_tailieu` chỉ có 5 trường
và **bắt buộc** có `source`, mà gói mẫu không có `source` ở đâu cả. Thêm `mets/@TYPE="Mixed"` và việc không có
`metadata/preservation/` thì loại trừ nốt Phụ lục IV lẫn AIP.

**Kết quả kiểm tra toàn vẹn — gói mẫu sạch tuyệt đối:**

| Phép kiểm | Kết quả |
|---|---|
| Checksum SHA-256 + `SIZE` của mọi tham chiếu (10 ở METS gốc, 274 ở METS rep1) | **284/284 đúng** |
| Ánh xạ `MetadataLink/File`: `@DMDID` → `EAD_DOC_X.xml` và `fptr@FILEID` → `X.pdf` | **137/137 khớp tên** |
| File trong `data/` được METS tham chiếu | 137/137, **không file mồ côi, không tham chiếu treo** |
| `dmdSec` và `<file>` được dùng trong `structMap` | 137/137 và 137/137 |
| Bộ tag của 137 file `EAD_DOC` | **đồng nhất tuyệt đối**, chỉ một bộ 21 tag |

Nói cách khác: cơ chế nối metadata ↔ tài liệu, băm và đếm của gói mẫu là **chuẩn để bắt chước**. Sai lệch so
với thông tư nằm ở câu chữ, không nằm ở tính đúng đắn.

**Bảng chốt — thông tư viết một đằng, gói mẫu làm một nẻo, theo gói mẫu:**

| # | Chỗ | Thông tư | **Gói mẫu (chuẩn)** |
|---|---|---|---|
| 1 | Bộ phần tử METS của SIP | 5 phần tử, không có `amdSec` | thêm **`<amdSec ID="uuid-…"/>` rỗng** ở cả METS gốc lẫn rep1 |
| 2 | `agent/note` ở METS gốc | `SOFTWARE VERSION` | **`IDENTIFICATIONCODE` = mã phông** `P_KHCN`; không ghi phiên bản phần mềm ở đâu |
| 3 | `agent` ở METS rep1 | `TYPE="ORGANIZATION"`, tên cơ quan | **`TYPE="INDIVIDUAL" OTHERTYPE=""`**, tên = tài khoản người đóng gói `cvlt` |
| 4 | `altRecordID` | mã đăng ký yêu cầu nộp | **`fileCode` của hồ sơ** |
| 5 | `mets/@OBJID` của rep1 | `uuid-{UUID}` riêng | **chuỗi cố định `rep1`**, không phải uuid |
| 6 | `mdRef/@MDTYPE` cấp gói | `OTHER` | **`DC`** (giống cấp tài liệu) |
| 7 | `mdRef/@MDTYPEVERSION` | `1.0` | **`1.0.0`** |
| 8 | `mdRef/@MIMETYPE` | `text/xml` | **`application/xml`** |
| 9 | Mimetype của `.xsd` | không quy định | **`application/octet-stream`** |
| 10 | Tên file schema | `EAD.xsd`, `EAD_doc.xsd` | **chữ thường**: `ead.xsd`, `ead_doc.xsd`, `ead_pic.xsd`, `ead_media.xsd` |
| 11 | Tên file metadata tài liệu | `EAD_doc_File1.xml` | **`EAD_DOC_` viết hoa** + `docId`: `EAD_DOC_H49.64.33.2001.160.1.xml` |
| 12 | Tên file trong `data/` | không quy định rõ | **`{docId}.pdf`**: `H49.64.33.2001.160.1.pdf` |
| 13 | Tên file ZIP | `[STT][OBJID]` | **GUID thường, không tiền tố, khác `OBJID`** (xem §9) |
| 14 | `format` của hồ sơ | String(50) tự do | dùng **mã `01`/`02`/`03`** như bảng của AIP |
| 15 | Thứ tự phần tử trong `EAD.xml` | theo thứ tự khung XML | **thứ tự xáo trộn** — hợp lệ vì cả hai `.xsd` dùng `xs:all`; **đừng ép thứ tự khi đọc** |

Hai điểm gói mẫu làm **đúng y thông tư**, đáng ghi để khỏi nghi ngờ: ID dùng `uuid-{UUID}` ở mọi nơi trừ
`<file>` dùng `ID-{UUID}`; và số thứ tự trong `docCode` đủ **7 ký tự** (`0000001` … `0000137`).

## 11. Ba lỗi của gói mẫu, đừng học theo

Gói mẫu là chuẩn cho **hình dạng**, không phải chuẩn cho **ba chỗ tự mâu thuẫn** sau. Cả ba đều kiểm được bằng
chính dữ liệu trong gói:

⚠️ **1. `docCode` sai năm ở toàn bộ 137 file.** `fileCode` của hồ sơ là `H49.64.33.**2001**.160` và `docId` là
`H49.64.33.**2001**.160.1`, nhưng `docCode` lại ghi `H49.64.33.**1994**.160.0000001`. Theo §5.1, `docCode` phải
là `fileCode` + số thứ tự 7 ký tự, tức đúng ra là `H49.64.33.2001.160.0000001`. Lệch **137/137 file**, luôn là
năm `1994` — một hằng số bị ghi cứng ở đâu đó trong bộ sinh. **Khi sinh gói mới thì lấy năm từ `fileCode`**,
đừng chép hằng số này.

⚠️ **2. Dữ liệu không hợp lệ với chính schema đi kèm.** `schemas/ead_doc.xsd` khai phần tử **`arcDocCode`** (tên
của AIP), trong khi cả **137/137** file `EAD_DOC_*.xml` đều ghi **`docCode`** (tên của SIP). Đem validate là
trượt hết. Thông tư đứng về phía dữ liệu — SIP dùng `docCode`. Vậy **giữ `docCode` trong dữ liệu và sửa
`ead_doc.xsd` thành `docCode`**, đừng đổi ngược lại.

⚠️ **3. `paperFileCode` để trống dù hồ sơ là bản số hóa.** `EAD.xml` có `confidenceLevel = 02` (số hóa), mà
theo §4.1 thì `paperFileCode` **bắt buộc nhập với hồ sơ số hóa**; trong gói mẫu thẻ này rỗng. Cùng kiểu:
`inforSign` và `description` của hồ sơ cũng rỗng (hai trường này thì không bắt buộc nên chấp nhận được), và
`process`/`autograph` rỗng ở cả 137 tài liệu (đúng luật, vì `confidenceLevel = 02` không thuộc diện bắt buộc
`process`).

> **Tiếp:** [05-aip.md](05-aip.md) — gói bảo quản: cấu trúc, METS, metadata khác SIP thế nào.
