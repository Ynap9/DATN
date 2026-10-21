# Chuẩn SIP — cấu trúc gói và METS (Phụ lục III & IV)

> **Phần 2/8** · trước: [01-thong-tu.md](01-thong-tu.md) · mục lục: [README.md](README.md)

> Hai hình dạng gói khi thu nộp **khác Hệ thống**. `SIP_hoso` = Phụ lục III (trang 182–238), `SIP_tailieu` =
> Phụ lục IV (trang 239–291). Cập nhật 2026-09-08.

> 🎯 **Gói mẫu là hàng chuẩn.** `b6acafed-a809-4b78-ae9f-0373d50d52f0.zip` → thư mục
> `uuid-0F203B84-6641-4CDA-9F52-DAAE55FEF6B1` (137 tài liệu, do `ArchiveNex` sinh 12/6/2026) là **`SIP_hoso`**
> và là bản mẫu để học theo. **Chỗ nào gói mẫu khác câu chữ thông tư thì làm theo gói mẫu** — trừ ba lỗi thật.
> Đối chiếu đầy đủ ở [04-sip-goi-mau.md](04-sip-goi-mau.md). Bản giải nén có ở
> `C:\Users\Admin\Downloads\b6acafed-a809-4b78-ae9f-0373d50d52f0\`.

## 1. Chọn `SIP_hoso` hay `SIP_tailieu`

Cả hai đều là SIP, khác nhau ở **gói nói về cái gì**, quyết định bởi Điều 17:

| | `SIP_hoso` (PL III) | `SIP_tailieu` (PL IV) |
|---|---|---|
| Đơn vị đóng gói | Một **hồ sơ** cùng các tài liệu trong nó | Một **lô tài liệu rời lẻ** |
| Điều kiện dùng | Cơ quan đã lập hồ sơ | Chưa lập hồ sơ; Hệ thống có tìm kiếm thông minh |
| `EAD.xml` cấp gói | **18 trường** — mô tả hồ sơ đầy đủ | **5 trường** — chỉ là đầu lô |
| Tài liệu liên kết `.fetch.txt` | Có quy định | **Không quy định** (xem ⚠️ [03-sip-metadata.md §6](03-sip-metadata.md)) |
| `mets/@TYPE` | `Mixed` | `Collection` |

Phần còn lại — cây thư mục, bộ phần tử METS, metadata từng tài liệu, schema, mimetype, quy tắc ZIP — **giống nhau**.

## 2. Cấu trúc vật lý (chung cho cả hai)

```text
<OBJID>/                                  tên thư mục = mets/@OBJID, vd uuid-7D0D1987-0F1C-...
├── METS.xml                              bắt buộc, 01 — mô tả cấu trúc gói
├── metadata/                             bắt buộc, 01
│   └── descriptive/                      bắt buộc, 01
│       └── EAD.xml                       bắt buộc, 01 — mô tả thông tin chung của gói
├── representations/                      bắt buộc, 01
│   └── rep1/                             bắt buộc, 01 — đúng một bản đại diện
│       ├── METS.xml                      bắt buộc, 01
│       ├── metadata/descriptive/
│       │   ├── EAD_doc_<tên>.xml         ≥01 — mỗi tài liệu một file
│       │   ├── EAD_pic_<tên>.xml         quy tắc tên: [Tiêu chuẩn]_[Loại tài liệu]_[Tên file]
│       │   └── EAD_media_<tên>.xml
│       └── data/                         bắt buộc, 01
│           ├── File1.pdf …               ≥01 — văn bản: PDF/A hai lớp (tuỳ chọn, xem 08-pdfa.md)
│           └── File2.doc.fetch.txt       chỉ SIP_hoso — trỏ tới tài liệu đã nộp lần trước
├── schemas/                              bắt buộc, 01
│   ├── METS.xsd                          bắt buộc, 01
│   └── EAD.xsd · EAD_doc.xsd · EAD_media.xsd · EAD_pic.xsd     ≥01 file
└── documentation/                        KHÔNG bắt buộc
    └── UserManual.pdf                    KHÔNG bắt buộc
```

Ánh xạ schema theo loại đối tượng: gói/hồ sơ → `EAD.xsd`; văn bản → `EAD_doc.xsd`; video, âm thanh →
`EAD_media.xsd`; phim âm bản, ảnh → `EAD_pic.xsd`.

**SIP không có `metadata/preservation/` và không có `PREMIS.xml`.** Siêu dữ liệu bảo quản là thứ lưu trữ lịch
sử thêm vào lúc chuyển SIP → AIP.

Gói mẫu **không có** thư mục `documentation/` (đúng, vì nó không bắt buộc) và có đủ 8 file trong `schemas/`:
`mets1_12.xsd` · `xlink.xsd` · `DILCISExtensionMETS.xsd` · `DILCISExtensionSIPMETS.xsd` · `ead.xsd` ·
`ead_doc.xsd` · `ead_pic.xsd` · `ead_media.xsd`. Tức **nộp cả bốn `ead*.xsd` kể cả khi gói chỉ có văn bản**,
và tên file schema viết **thường** (`ead.xsd`, không phải `EAD.xsd` như bảng thông tư).

## 3. METS

Thông tư mô tả năm phần tử: `mets` → `metsHdr` → `dmdSec` → `fileSec` → `structMap`.

⚠️ **Gói mẫu phát thêm `<amdSec ID="uuid-…"/>` rỗng**, ở **cả METS gốc lẫn METS của rep1**, đặt giữa `dmdSec`
và `fileSec`. Thông tư không kể `amdSec` trong bộ phần tử của SIP, nhưng đây là chỗ chốt theo gói mẫu: **sinh
một `amdSec` rỗng có `@ID`**. Giữ nó rỗng — SIP không có PREMIS để trỏ tới.

```xml
<mets xmlns="http://www.loc.gov/METS/"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      xmlns:xlink="http://www.w3.org/1999/xlink"
      xmlns:csip="https://DILCIS.eu/XML/METS/CSIPExtensionMETS"
      xmlns:sip="https://DILCIS.eu/XML/METS/SIPExtensionMETS"
      OBJID="uuid-7D0D1987-0F1C-47A7-8FD6-CC5C7DE4064F"
      LABEL="Tài liệu về hoạt động tổ chức cán bộ … năm 2020"
      TYPE="Collection" csip:CONTENTINFORMATIONTYPE="MIXED"
      PROFILE="https://earkcsip.dilcis.eu/profile/E-ARK-CSIP.xml"
      xsi:schemaLocation="http://www.loc.gov/METS/ schemas/mets1_12.xsd
                          http://www.w3.org/1999/xlink schemas/xlink.xsd
                          https://dilcis.eu/XML/METS/CSIPExtensionMETS schemas/DILCISExtensionMETS.xsd
                          https://dilcis.eu/XML/METS/SIPExtensionMETS schemas/DILCISExtensionSIPMETS.xsd">
```

| Thuộc tính `mets` | Giá trị cho SIP |
|---|---|
| `OBJID` | `uuid-{UUID}` — **không có tiền tố `urn:`**, UUID viết hoa. Tên thư mục gói lấy nguyên chuỗi này |
| `LABEL` | Mô tả nội dung gói (lấy từ `title` của metadata), không bắt buộc |
| `TYPE` | `Mixed` (hoso) / `Collection` (tailieu) |
| `PROFILE` | `https://earkcsip.dilcis.eu/profile/E-ARK-CSIP.xml` — **cả gốc lẫn rep1** |
| `csip:CONTENTINFORMATIONTYPE` | `MIXED`; không bắt buộc ở gốc nhưng **bắt buộc trong METS của rep1** |
| `xsi:schemaLocation` | `schemas/mets1_12.xsd` ở gốc; `../../schemas/mets1_12.xsd` trong rep1 |

**`metsHdr`** — `CREATEDATE` (bắt buộc), `LASTMODDATE`, `RECORDSTATUS` (`NEW` mặc định · `SUPPLEMENT` ·
`REPLACEMENT` · `TEST` · `OTHER`), **`csip:OAISPACKAGETYPE="SIP"`**, và:

| Phần tử | Gói gốc | Gói rep1 |
|---|---|---|
| `agent/@ROLE` | `CREATOR` | `CREATOR` |
| `agent/@TYPE` | `OTHER` kèm `@OTHERTYPE="SOFTWARE"` | `ORGANIZATION` |
| `agent/name` | tên phần mềm đóng gói, vd `VietNam Fonds Archival System` | tên cơ quan, vd `bnv` |
| `agent/note/@csip:NOTETYPE` | `SOFTWARE VERSION` | `IDENTIFICATIONCODE` → **mã phông**, vd `Phong_BNV` |
| `altRecordID/@TYPE` | `SUBMISSIONAGREEMENT`, giá trị là **mã đăng ký yêu cầu nộp** | — |

Gói mẫu làm khác ba chỗ trong bảng trên, và **đây là bản chốt theo**:

| Chỗ | Thông tư | **Gói mẫu — làm theo cái này** |
|---|---|---|
| `agent/note` ở **gốc** | `SOFTWARE VERSION` mang số phiên bản | `IDENTIFICATIONCODE` mang **mã phông** `P_KHCN`. Tức mã phông xuất hiện ở **cả hai** METS, còn số phiên bản phần mềm **không được ghi ở đâu cả** |
| `agent` ở **rep1** | `TYPE="ORGANIZATION"`, `name` = tên cơ quan | `TYPE="INDIVIDUAL" OTHERTYPE=""`, `name` = **tài khoản người đóng gói** (`cvlt`) |
| `altRecordID` | mã đăng ký yêu cầu nộp | **`fileCode` của hồ sơ** (`H49.64.33.2001.160`) |

Trích đúng từ gói mẫu:

```xml
<metsHdr CREATEDATE="2026-06-12T11:45:37.690+07:00" LASTMODDATE="2026-06-12T11:45:37.690+07:00"
         RECORDSTATUS="NEW" csip:OAISPACKAGETYPE="SIP">
    <agent ROLE="CREATOR" TYPE="OTHER" OTHERTYPE="SOFTWARE">
        <name>ArchiveNex</name>
        <note csip:NOTETYPE="IDENTIFICATIONCODE">P_KHCN</note>
    </agent>
    <altRecordID TYPE="SUBMISSIONAGREEMENT">H49.64.33.2001.160</altRecordID>
</metsHdr>
```

**`dmdSec`** trỏ tới các file trong `metadata/`: `@ID` = `uuid-{UUID}`, `@CREATED`, `@STATUS` (vd `CURRENT`), bên
trong là `mdRef` mang `@LOCTYPE="URL"`, `@MDTYPE`, `@MDTYPEVERSION`, `@xlink:href` (đường dẫn tính từ thư mục
gốc), `@MIMETYPE`, `@SIZE` (byte), `@CREATED`, `@CHECKSUMTYPE` (**SHA-256** mặc định), `@CHECKSUM`.

**`fileSec`** gom theo `fileGrp/@USE`: ở gốc là `Schemas`, `Representations/rep1`, tùy chọn `Documentation`;
trong rep1 là `Data` và — khi có tài liệu liên kết — **`Holeyfile`**. Mỗi `<file>` dùng `ID-{UUID}` (khác
`uuid-`), kèm `@MIMETYPE`, `@CREATED`, `@CHECKSUMTYPE`, `@CHECKSUM`, và `FLocat/@xlink:href` +
`@xlink:type="simple"` + `@LOCTYPE="URL"`.

Giá trị gói mẫu dùng, chốt theo:

| Thuộc tính | Giá trị trong gói mẫu | Ghi chú |
|---|---|---|
| `mdRef/@MDTYPE` | **`DC`** ở **cả** cấp gói lẫn cấp tài liệu | thông tư gợi ý `OTHER` cho cấp gói — gói mẫu dùng `DC` cho cả hai |
| `mdRef/@MDTYPEVERSION` | **`1.0.0`** | thông tư ghi mặc định `1.0` |
| `mdRef/@MIMETYPE` | **`application/xml`** | thông tư ví dụ `text/xml` |
| `file/@MIMETYPE` cho `.xsd` | **`application/octet-stream`** | bảng mimetype của thông tư không có mục nào cho `.xsd` |
| `file/@MIMETYPE` cho `METS.xml` của rep1 | `application/xml` | |
| `file/@MIMETYPE` cho tài liệu | `application/pdf` | |
| `@CHECKSUMTYPE` | `SHA-256`, giá trị băm **viết HOA** | |
| `@CREATED` | `2026-06-12T11:45:37.707+07:00` | có mili giây và **offset `+07:00`**, không dùng `Z` |

`fileGrp` trong gói mẫu: METS gốc có đúng hai nhóm `Schemas` (8 file) và `Representations/rep1` (1 file —
chính `rep1/METS.xml`); METS của rep1 có **đúng một** nhóm `Data` (137 file). Không có nhóm `Documentation`,
không có `Holeyfile` (gói này không dùng tài liệu liên kết).

**`structMap`** — `@LABEL="CSIP"`, `@TYPE="PHYSICAL"`:

| METS gốc | METS rep1 |
|---|---|
| `div[@LABEL="Metadata"]` → `@DMDID` trỏ `dmdSec` | `div[@LABEL="rep1"]` với `@TYPE="ORIGINAL"` |
| `div[@LABEL="Schemas"]/fptr@FILEID` → fileGrp `Schemas` | `div[@LABEL="Data"]/fptr@FILEID` → fileGrp `Data` |
| `div[@LABEL="Representations/rep1"]/mptr@xlink:href` → `representations/rep1/METS.xml`, `@xlink:title` = ID của fileGrp `Representations/rep1` | `div[@LABEL="MetadataLink"]/div[@LABEL="MetadataLink/File"]`: `@DMDID` = `dmdSec` của tài liệu, `fptr@FILEID` = file trong `data/` — đây là chỗ **nối metadata với tài liệu** |
| `div[@LABEL="Documentation"]/fptr` (tùy chọn) | `div[@LABEL="MetadataLink/Holey"]` cho tài liệu liên kết; `div[@LABEL="AttachmentFile"]` khi một tài liệu có file đính kèm |

Quy ước ID dùng chung: `uuid-{UUID}` ở mọi nơi, riêng `<file>` dùng `ID-{UUID}`; UUID **viết hoa**. Kiểu ngày giờ
`YYYY-MM-DDThh:mm:ss.sTZD`. Hàm băm mặc định **SHA-256**.

> **Tiếp:** [03-sip-metadata.md](03-sip-metadata.md) — metadata EAD cấp gói, cấp tài liệu, bảng mã, mimetype, tên ZIP.
