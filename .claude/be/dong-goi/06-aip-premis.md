# Chuẩn AIP — PREMIS và tài liệu liên kết

> **Phần 6/8** · trước: [05-aip.md](05-aip.md) · mục lục: [README.md](README.md)

## 6. PREMIS — siêu dữ liệu bảo quản

Thứ làm nên khác biệt lớn nhất giữa AIP và SIP. Chuẩn PREMIS v3.0
(`http://www.loc.gov/premis/v3`), bốn phần tử:

```xml
<premis xmlns:premis="http://www.loc.gov/premis/v3"
        xmlns:xlink="http://www.w3.org/1999/xlink"
        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        xsi:schemaLocation="http://www.loc.gov/premis/v3
                            http://www.loc.gov/standards/premis/premis-3-0-draft.xsd"
        version="3.0">
  <object><objectIdentifier>…</objectIdentifier></object>
  <event><eventIdentifier>…</eventIdentifier></event>
  <agent><agentIdentifier>…</agentIdentifier></agent>
</premis>
```

| Phần tử | Vai trò |
|---|---|
| `premis` | gốc; `@xmlns` bắt buộc, `@version` không bắt buộc |
| `object` | đối tượng được bảo quản |
| `event` | **mỗi sự kiện bảo quản/sao lưu là một `event` riêng biệt** |
| `agent` | tác nhân thực hiện sự kiện |

### 6.1. `object`

Bắt buộc: `@xmlID` · `objectIdentifier` (`objectIdentifierType` vd `File`/`Doc`/`Pic`/`Media`, và
`objectIdentifierValue` — **cặp này phải là duy nhất trong kho**) · `objectCategory` (nhận `bitstream`, `file`,
`intellectual entity`, `representation`).

Không bắt buộc nhưng đáng dùng: `preservationLevel` (`Type` vd *Bit preservation* / *Logical preservation*;
`Value` vd Low/Medium/High; `Role` vd requirement/intention/capability; `Rationale`; `DateAssigned`) ·
`originalName` (tên lúc thu thập, trước khi kho đổi tên) · `objectCharacteristics` (`compositionLevel`,
**`fixity`** với `messageDigestAlgorithm` = **SHA-256** và `messageDigest`, `size`, `format` gồm
`formatDesignation` và `formatRegistry` kiểu PRONOM `fmt/353`) · `storage`/`store` (`contentLocation` với
`Type`/`Value`, `storageMedium` vd *Hard disk*) · `signatureInformation`.

`signatureInformation/signature` là chỗ ghi chữ ký số của đối tượng: `signatureEncoding` (vd `base64`),
`signer`, `signatureMethod` (vd `DSA-SHA 1`), `signatureValue`, `keyInformation`. Đây là móc nối tự nhiên giữa
luồng ký số của sh.be (module DigitalSignature) và siêu dữ liệu bảo quản — bản ký xong có đủ dữ liệu để điền nhóm này.

### 6.2. `event` — nơi ghi lại "gói này từ đâu ra"

Bắt buộc: `eventIdentifier` (`Type` vd `UUID`/`local`, `Value`) · `eventType` · `eventDateTime`.

`eventType` lấy giá trị từ từ vựng **Event Type của Library of Congress** (vd `validation`, `virus check`), và
TT05 chốt cứng hai trường hợp nghiệp vụ:

| Tình huống | `eventType` |
|---|---|
| Số hóa | **`transfer`** |
| Chuyển `SIP` → `AIP_hoso` | **`information package creation`** |

Không bắt buộc: `eventDetailInformation` · `eventOutcomeInformation` (`eventOutcome` — thành công / thành công
một phần / thất bại, vd mã `00`; `eventOutcomeDetail/eventOutcomeDetailNote`) · `linkingAgentIdentifier`
(`Type`, `Value`, **`linkingAgentRole`** nhận `authorizer`, `implementer`, `validator`, `executing program`) ·
`linkingObjectIdentifier` (`Type`, `Value`, **`linkingObjectRole`** nhận `source`, `outcome`).

Cặp `linkingObjectRole = source` / `outcome` chính là cách diễn đạt "gói SIP nào đẻ ra gói AIP nào" — đọc được
cả chuỗi nguồn gốc chỉ bằng PREMIS.

### 6.3. `agent`

Bắt buộc: `agentIdentifier` · `agentIdentifierType` · `agentIdentifierValue` · `agentName`. Ví dụ trong TT05
dùng `linkingAgentIdentifierType = software` với giá trị `E-ARK Web 0.9.3 (task: SIP_to_AIP_hoso_Reset)` — tức
tên **và** phiên bản **và** tác vụ của phần mềm đã tạo gói.

## 7. Tài liệu liên kết trong `AIP_hoso`

Cùng ý tưởng holey file như SIP nhưng **nội dung giàu hơn hẳn**, vì AIP phải trỏ được tới vị trí vật lý thật:

| | `AIP_hoso` (trang 68) |
|---|---|
| Khi nào dùng | Gói `AIP_hoso_2` tham chiếu tài liệu đã nằm trong gói `AIP_hoso_1` đã lưu trong hệ thống |
| Quy tắc tên | `Tên của tài liệu liên kết.Định dạng tài liệu lưu trữ` |
| Đuôi | `.fetch.txt` |
| Nội dung | `urn_{mã phông}_{uuid của gói AIP_hoso_1}_{Đường dẫn đến tài liệu lưu trữ} {size} {đường dẫn đến tài liệu trong gói AIP_hoso_2} {ID tài liệu lưu trữ}` |
| Ràng buộc | **Chỉ tham chiếu được tài liệu trong cùng một phông** |

So sánh: bản `SIP_hoso` chỉ ghi `{Định danh tài liệu}`. Nghĩa là khi chuyển SIP → AIP, holey file **phải được
viết lại**, không chép nguyên — lúc đó mới biết uuid gói đích, kích thước và đường dẫn vật lý.

`structMap` của `AIP_hoso` nối chúng bằng `div[@LABEL="MetadataLink/Holey"]` (kèm `@DMDID` và `@ADMID`), song
song với `div[@LABEL="MetadataLink/File"]` cho tài liệu thường và `div[@LABEL="AttachmentFile"]` cho tài liệu
có đính kèm.

## 8. Những gì AIP **không** quy định

- **Không có quy tắc đặt tên file ZIP.** Phụ lục I và II chỉ nói gói được nén ZIP khi truyền nhận và lưu trữ.
  Chỉ SIP mới có mục "5. Quy định đặt tên file nén ZIP".
- **Không có trường `source` ở `AIP_hoso`** (chỉ `AIP_tailieu` có).
- Danh sách mimetype và các bảng mã (`typeName`, `language`, `maintenance`, `mode`, `confidenceLevel`) **giống
  hệt SIP** — xem [03-sip-metadata.md §7–§8](03-sip-metadata.md), không lặp lại ở đây.

> **Tiếp:** [07-dip.md](07-dip.md) — gói giao cho người dùng, metadata cấp gói mô tả *yêu cầu khai thác*.
