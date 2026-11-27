# Yêu cầu từng mẫu gói — bảng đối chiếu có nguồn

> **Phần 10/11** · trước: [09-goi-mau-chi-tiet.md](09-goi-mau-chi-tiet.md) · mục lục: [README.md](README.md)

Một bảng cho **5 loại gói** sh.be dựng (`PackageConst` 1–5). Mỗi ô ghi **nguồn**: `GM` = gói mẫu
([09](09-goi-mau-chi-tiet.md)), `tr.N` = trang N của [tai-lieu/TT05-2025-TT-BNV.txt](tai-lieu/TT05-2025-TT-BNV.txt)
(tìm `===== TRANG N`). Rà ngày 2026-11-27 trực tiếp trên bản chữ thông tư, không chép từ tài liệu cũ.

## 1. Thứ tự trọng tài — áp đúng thứ tự, không tự chọn

1. **Gói mẫu** — cho `SIP_hoso` (là loại của gói mẫu), trừ 3 lỗi ở [04 §11](04-sip-goi-mau.md).
2. **Bảng mô tả** của phụ lục ("Quy tắc: …", cột Yêu cầu) — thắng ví dụ XML của chính phụ lục đó.
3. **`.xsd`** của phụ lục — cho **bộ trường** `simpledc` (đã chốt ở [16-dong-goi-cau-hinh.md](../nghiep-vu/16-dong-goi-cau-hinh.md), seed Task 37).
4. **Ví dụ XML** — chỉ để tham khảo cách viết; ví dụ trong thông tư tự mâu thuẫn nhiều chỗ (§3).
5. Không nguồn nào quy định → **hỏi người dùng** (§4). Không tự đặt luật, không mượn kssm / soHoaSaoMai cho
   phần **nội dung gói** (kssm luôn ghi `OAISPACKAGETYPE="SIP"`, không có PREMIS — sai cho AIP / DIP).

## 2. Bảng 5 mẫu

| Mục | AIP_hoso (PL I) | AIP_tailieu (PL II) | SIP_hoso (PL III) | SIP_tailieu (PL IV) | DIP (PL V) |
|---|---|---|---|---|---|
| Cây thư mục | tr.34–36 | tr.112–113 | tr.183–185 · GM | tr.241 | tr.294–295 |
| `metadata/preservation/` + PREMIS | **bắt buộc** gốc + rep1, tr.35–36 | **bắt buộc**, tr.112–113 | không có · GM | không có | cây: không bắt buộc tr.294–295; **có** theo Q5 (bảng phần tử tr.302, 313) |
| `.fetch.txt` (tài liệu liên kết) | có, tr.36, tr.68 | không | có, tr.185, tr.211 | không quy định | không |
| `mets/@OBJID` gốc | `urn:{mã phông}:uuid:{UUID HOA}` — tr.38 | như AIP_hoso — tr.115 | `uuid-{UUID HOA}` — tr.187 · GM | `uuid-{UUID HOA}` — tr.244 | `uuid-{UUID HOA}` — tr.297 |
| Tên thư mục gói | OBJID đổi `:` → `_` — tr.34 | như trái — tr.111 | = OBJID · GM | = OBJID | OBJID đổi `:` → `_` — tr.293 |
| `mets/@TYPE` | `Mixed` mặc định — tr.38 | `Collection` — tr.116 | `Mixed` · GM | `Collection` — tr.244 | `MIXED` mặc định — tr.297 |
| `PROFILE` gốc | `http://www.ra.ee/METS/v01/IP.xml` — tr.39 (ví dụ) | như trái — tr.116 | E-ARK-CSIP · GM | E-ARK-CSIP — tr.244 | `https://earkdip.dilcis.eu/profile/E-ARK-DIP.xml` — tr.296, 298 |
| `csip:OAISPACKAGETYPE` | `AIP` — tr.41, 54 | `AIP` — tr.118, 132 | `SIP` — tr.189 · GM | `SIP` — tr.246 | `DIP` — tr.299 (bảng; ví dụ tr.311 ghi nhầm `SIP`) |
| `agent` gốc | `OTHER/SOFTWARE` + note `SOFTWARE VERSION` — tr.41 | như trái — tr.118 | **GM**: `OTHER/SOFTWARE`, note `IDENTIFICATIONCODE` = mã phông | tr.246 | tr.300 |
| `agent` rep1 | `ORGANIZATION` + `IDENTIFICATIONCODE` — tr.54 | tr.132 | **GM**: `INDIVIDUAL`, name = tài khoản | tr.258 | tr.311 |
| `altRecordID` | không có | không có | **GM**: `fileCode` (tr.190 ghi mã đăng ký nộp) | tr.247 | không có |
| `amdSec` | **có nội dung**, `digiprovMD` → `metadata/preservation/PREMIS.xml` — tr.43–45, 57–59 | tr.120–122, 134–135 | PL III không nhắc `amdSec`; **GM**: rỗng có `ID` | PL IV không nhắc → theo GM (C13) | bảng phần tử ghi `digiprovMD` **bắt buộc** — tr.302, 313–314; mâu thuẫn cây thư mục, xem §3 |
| `structMap` div rep1 | `TYPE="NORMALIZED"` — tr.67 | `NORMALIZED` — tr.143 | `ORIGINAL` · GM | tr.266–267 | tr.321 |
| `structMap` nối PREMIS | `@ADMID` — tr.49, 64 | tr.126, 142 | không | không | `@ADMID` / `@AMDID` — tr.307, 319, 321 |
| EAD cấp gói (`.xsd`) | 18 trường — tr.101 | 4 trường — tr.173 | 18 · GM (`.xsd` thông tư chỉ 16, tr.231) | 5 — tr.284 | 7 (yêu cầu khai thác) — tr.348 |
| EAD tài liệu văn bản (`.xsd`) | 19, **không `docId`** — tr.102 | 19, không `docId` — tr.174 | 21 · GM (`.xsd` thông tư 19, tr.232) | seed 21 theo hình dạng GM (`.xsd` thông tư 19, thiếu `riskRecovery*`, tr.285) | 20, có `docId`, không `organName` — tr.349 |
| EAD ảnh / media (`.xsd`) | 16 / 17 — tr.104–105 | 18 / 19 — tr.175, 177 | seed 18 / 19 theo GM (`.xsd` thông tư 16 / 17, tr.233–234) | seed 18 / 19 theo GM (`.xsd` 16 / 17, tr.286–287) | 18 / 19 — tr.350, 353 |
| Mã tài liệu (bảng) | `arcDocCode` = mã CQ lưu trữ + mã hồ sơ + STT 7 — tr.82 | tr.154 | `docCode` = mã định danh + năm + số ký hiệu hồ sơ + STT 7 — tr.217–218; **GM** nối bằng `.` | `docCode` = mã định danh + năm + **số lần nộp 2 + STT trong lần nộp 7** — tr.270 | `arcDocCode` — tr.324, 335 |
| Mã gói (bảng) | `arcFileCode` | `arcFileCode` (mã gói tin) | `fileCode` | `fileCode` = mã định danh + năm + số lần nộp + STT — tr.268 | `requestID` |
| Tên file trong `data/` | `File1.pdf` chung chung — tr.36 | tr.113 | **GM**: `{docId}.pdf` | tr.241 | tr.295 |
| Tên file metadata tài liệu | `[Tiêu chuẩn]_[Loại]_[Tên file]` — tr.35 | tr.112 | **GM**: `EAD_DOC_{docId}.xml` | tr.241 | tr.294 |
| Ngày rút gọn `MM/YYYY`, `YYYY` | `startDate`, `endDate` tr.72; `issuedDate` tr.77–78 | `issuedDate` tr.149 | không, chỉ `DD/MM/YYYY` — tr.214, 219 | không — tr.272 | không — tr.327 |
| PREMIS `eventType` | số hóa `transfer`; SIP→AIP `information package creation` — tr.97–98 | `transfer` — tr.169 | — | — | bảng tr.345 chép y AIP; sh.be ghi `dissemination` (Q7) |
| Tên ZIP | không quy định | không quy định | `STT + OBJID` — tr.238; **GM**: GUID thường | `STT + OBJID` — tr.291 | không quy định |

Điều 8.1.d (tr.6): tên tệp tin số hóa **tối thiểu** gồm mã hồ sơ + `.` + số thứ tự tài liệu trong hồ sơ; hồ sơ
không tách tài liệu thì tên tệp = mã hồ sơ. Gói mẫu `{docId}.pdf` = `H49.64.33.2001.160.1` đúng luật này.

## 3. Thông tư tự mâu thuẫn — đã thấy khi rà 27/11

| Chỗ | Bảng mô tả | Ví dụ / chỗ khác | Theo |
|---|---|---|---|
| OBJID AIP | `urn:{Fondcode}:uuid:{UUID HOA}` (tr.38, 115) | `urn:uuid:7d0d…` chữ thường (tr.37), `urn:Phong_BNV:uuid-DB15…` gạch nối (tr.39) | bảng |
| `mets/@TYPE` AIP_hoso | `Mixed` (tr.38) | `TYPE="AIP"` (tr.37) | bảng |
| Tên PREMIS rep1 | `PREMIS_rep1.xml` (cây tr.36) | `PREMIS_uuid-60552F0B-….xml` (ví dụ tr.58) | bảng cấu trúc (tr.36) |
| OAISPACKAGETYPE DIP | `DIP` (tr.299) | `SIP` (tr.311) | bảng |
| PREMIS DIP | cây thư mục: `PREMIS.xml`, `PREMIS_rep1.xml` **không bắt buộc** (tr.294–295) | bảng phần tử: `amdSec/digiprovMD` → PREMIS **bắt buộc** (tr.302, 313–314); `div Metadata` trỏ `amdSec` bắt buộc (tr.307) | Q5 → sinh PREMIS (thoả cả hai) |
| `ADMID` | "ID của thẻ `amdSec`" (tr.49, 64, 126, 307, 319) | `mets1_12.xsd` + ví dụ rep1 tr.67: ID `digiprovMD` | bảng ([11 §2](11-premis-amdsec-structmap.md)) |
| DMDID / ADMID div Metadata DIP | tr.307 ghi ngược mô tả hai dòng | AIP tr.49, 126 + `mets1_12.xsd` đúng chiều | AIP + `.xsd` |
| `structMap` rep1 div `Metadata` | bắt buộc (tr.63, 318) | ví dụ tr.67 không có | bảng |
| `AttachmentFile` rep1 | "bắt buộc" (cả SIP_hoso tr.206–210) | GM không có khi không có đính kèm | GM: chỉ phát khi có đính kèm |
| `linkingAgentIdentifierValue` | bằng `agentIdentifierValue` đang có (tr.98–99) | ví dụ tr.100 thêm "(task: …)" | bảng |
| UUID DIP | viết hoa (tr.297) | `uuid-4422c185-…` thường (tr.296) | bảng |
| `docCode` SIP | không nói dấu nối | GM nối bằng `.` | GM |

Lưu ý: [05-aip.md](05-aip.md) trước 27/11 ghi OBJID AIP theo **ví dụ** (`urn:uuid:…` hoặc `urn:{mã phông}:uuid-…`) —
đã sửa theo bảng.

## 4. Chưa nguồn nào quy định — người dùng đã chốt 27/11

| # | Câu hỏi | Vì sao chưa có nguồn |
|---|---|---|
| Q1 | 15 điểm gói mẫu lệch thông tư (04 §10: `amdSec` rỗng, `MDTYPE="DC"`, `1.0.0`, `application/xml`, schema chữ thường, `EAD_DOC_` hoa, `OBJID` rep1 = `rep1`, agent, ZIP GUID…) có áp cho `SIP_tailieu`, AIP, DIP không? | Gói mẫu chỉ là `SIP_hoso`; thông tư ghi khác cho từng phụ lục |
| Q2 | Tên PDF trong `data/`, khoá ghép PDF ↔ dòng Excel, và cách gom PDF vào gói cho schema **không có `docId`** (AIP văn bản, mọi ảnh / media) và cho DIP (mã gói `requestID`) | Thông tư chỉ ghi `File1.pdf`; Điều 8.1.d chỉ nói "tối thiểu mã hồ sơ + STT"; kssm chỉ gom theo `docId` bắt đầu bằng `fileCode` |
| Q3 | DIP: tệp văn bản xác thực `.pdf/a` 11 thông tin (Điều 42.2, bắt buộc) lấy ở đâu; có sinh PREMIS (không bắt buộc) không | sh.be chưa có công cụ sinh PDF/A ([08](08-pdfa.md)); thông tư không nói tệp đặt ở đâu trong gói |
| Q4 | PREMIS của AIP: `agent` ghi phần mềm gì, `object` có ghi `signatureInformation` từ PDF đã ký không | Thông tư chỉ ví dụ `E-ARK Web 0.9.3`; gói mẫu là SIP nên không có PREMIS |
| Q5 | DIP có sinh PREMIS không, khi cây thư mục (tr.294–295) ghi không bắt buộc mà bảng phần tử (tr.302, 307, 313–314) ghi bắt buộc | Thông tư tự mâu thuẫn, gói mẫu là SIP |
| Q6 | PREMIS: bảng đòi thẻ `<objectCategory>` (tr.91–92, 163, 340), PREMIS v3 không có thẻ này, ví dụ tr.94 dùng `xsi:type` | Trả lời 27/11: dùng `xsi:type` |
| Q7 | `eventType` của DIP: bảng tr.345 chép y AIP (chỉ số hoá / SIP→AIP) | Trả lời 27/11: `dissemination` (từ vựng LoC) |

Trả lời (chi tiết: plan [dong-goi-tt05.plan.md](../plans/dong-goi-tt05.plan.md) C9–C12): Q1 giá trị riêng hệ thống
(tên phần mềm, phiên bản, agent, tài khoản, mã phông) lấy từ cấu hình của mình, gói mẫu chỉ là khuôn · Q2 tên PDF
theo mã tài liệu của schema, gom gói theo mã gói (Điều 8.1.d), DIP 1 Excel = 1 gói · Q3 người dùng tự đưa PDF xác
thực vào thư mục (phần "không PREMIS" bị Q5 thay) · Q4 tên phần mềm "Thi công số hoá" · Q5 DIP **có** PREMIS
(bảng phần tử tr.302, 313–314 bắt buộc `digiprovMD`; cây thư mục tr.294 chỉ ghi không bắt buộc).

## 5. Phần không thuộc TT05 — luồng nhập liệu

Thư mục tải lên, Excel mẫu, ghép tên cột, khớp tên thư mục với mã gói: TT05 **không quy định**. Đây là lựa chọn
kỹ thuật của PM (plan N1–N13), chốt 26/11 "như soHoaSaoMai `dong-goi/kiem-tra`". Code kssm chỉ để xem cách làm,
**không phải nguồn** (người dùng 27/11: nguồn đáng tin chỉ có thông tư + gói mẫu). Luật **giá trị ô** (kiểu, độ dài, mã, bắt buộc)
lấy từ `PackageSchemaField` — tức từ thông tư qua seed Task 37.

> **Tiếp:** [11-premis-amdsec-structmap.md](11-premis-amdsec-structmap.md) — `amdSec`, `structMap`, PREMIS của AIP và DIP rà từng phần tử.
