# Plan: đóng gói TT05 — kiểm tra thư mục tải lên + dựng SIP / AIP / DIP

> **Trạng thái:** đã duyệt 2026-11-27 sau 4 lượt rà (Q1–Q7 → C9–C12, C14; X1 → C13). Task 65 (kiểm) + Task 66 (dựng gói) đã viết cho
> Ánh. Trước khi đọc plan: làm đủ "Luật bắt buộc" ở [../dong-goi/README.md](../dong-goi/README.md).

## Input — cái đã có

- Bộ trường 5 loại gói × 4 loại metadata = 20 schema, 333 trường, 82 mã, seed sẵn (Task 37):
  [../nghiep-vu/16-dong-goi-cau-hinh.md](../nghiep-vu/16-dong-goi-cau-hinh.md). Excel mẫu xuất từ
  `api/v1/packaging/config/excel-template` — đây là file người dùng điền.
- Quy định: [../dong-goi/](../dong-goi/README.md) 11 file + văn bản gốc + gói mẫu trong repo (`goi-mau/`).
- **Nguồn luật chỉ có 2: thông tư + gói mẫu** (người dùng 27/11). Code kssm (`ksts/kssm.be` thư mục `DongGoi`)
  chỉ để xem cách làm luồng nhập, **không phải nguồn**, không chép luật nội dung gói từ kssm.
- Hạ tầng sh.be: `IFileStorageExternal` (MinIO private, link tạm, xoá theo tiền tố), Hangfire
  (`IBackgroundJobExternal.Enqueue`, `RecurringJobRegistrar`), `IExcelReaderExternal`, PdfSharp.

## Quyết định đã chốt — kèm nguồn

| # | Quyết định | Nguồn |
|---|---|---|
| C1 | Dữ liệu lấy từ **thư mục tải lên** (Excel mẫu Task 37 + PDF), không lấy từ dự án, không bảng ghép trường | Người dùng 26/11 |
| C2 | Làm đủ **3 chuẩn = 5 loại gói** `PackageConst` 1–5, cả kiểm lẫn dựng | Người dùng 27/11 |
| C3 | Chỉ Admin dùng, 1 key quyền mới (Admin / SuperAdmin nhận mọi key — `SeedRolePermission.FullPermissionRoles`) | Người dùng 27/11 |
| C4 | 3 bảng mới `PackageSession` / `PackageDossier` / `PackageDocument` + 1 migration chỉ thêm bảng | Người dùng 27/11 |
| C5 | ZIP lưu **MinIO private** + link tải tạm, không Google Drive; phiên + gói dọn sau 7 ngày | Người dùng 27/11 |
| C6 | Nội dung gói (cây thư mục, METS, EAD, PREMIS, checksum, tên file) theo thứ tự trọng tài [10 §1](../dong-goi/10-yeu-cau-tung-mau.md): gói mẫu → bảng mô tả → `.xsd` → ví dụ | README dong-goi, 10 |
| C7 | Ký số và PDF/A chỉ **cảnh báo**, không chặn gói | [08-pdfa.md](../dong-goi/08-pdfa.md), [03 §8](../dong-goi/03-sip-metadata.md) |
| C8 | Ô trống = rỗng / chỉ khoảng trắng; chữ "null" là dữ liệu | Người dùng 27/11 (chốt import) |
| C9 | Giá trị riêng của hệ thống **không chép từ gói mẫu**, lấy từ cấu hình của mình: tên phần mềm, phiên bản, agent, tài khoản người đóng gói, mã phông. Gói mẫu chỉ là khuôn | Người dùng 27/11 (Q1) |
| C10 | Tên phần mềm = **"Thi công số hoá"** (tiêu đề app FE). PREMIS agent: `LOCAL` / `Thi công số hoá {phiên bản}` / `Thi công số hoá` / `Software`, **không** thêm "(task: …)" (bảng tr.98–101, [11 §4](../dong-goi/11-premis-amdsec-structmap.md)) | Người dùng 27/11 (Q4) |
| C11 | Tên PDF + khoá ghép = mã tài liệu có trong schema: `docId` → không có thì `docCode` / `arcDocCode` → AIP_hoso ảnh / media `arcFileCode` + `.` + `docOrdinal` 7 số. Gom gói: mã tài liệu bắt đầu bằng mã gói (Điều 8.1.d). DIP: mỗi Excel khai đúng 1 yêu cầu khai thác = 1 gói | Người dùng 27/11 (Q2) |
| C12 | DIP: người dùng tự đưa 1 file PDF xác thực (Điều 42.2) vào thư mục; BE kiểm có file, cảnh báo nếu không phải PDF/A, đưa vào `documentation/` của gói (tr.294: tệp mô tả bổ sung) | Người dùng 27/11 (Q3) |
| C13 | Khuôn gói mẫu (15 điểm 04 §10) áp cho **cả 5 loại** ở chỗ phụ lục không ghi khác hẳn; chỗ ghi khác hẳn (OBJID AIP, OAISPACKAGETYPE, PROFILE, PREMIS, `NORMALIZED`) theo bảng phụ lục. Agent METS gốc có 2 note: `IDENTIFICATIONCODE` = mã phông + `SOFTWARE VERSION` = phiên bản | Người dùng 27/11 (X1) |
| C14 | DIP **có** `PREMIS.xml` + `PREMIS_rep1.xml`, `amdSec` + `ADMID` như AIP (tr.302, 307, 313–314), `eventType` = `dissemination` (Q7). Mọi PREMIS: loại đối tượng bằng `xsi:type`, không thẻ `objectCategory` (Q6). Chi tiết từng phần tử: [11](../dong-goi/11-premis-amdsec-structmap.md) | Người dùng 27/11 (Q5–Q7) |

## Luồng nhập — TT05 và gói mẫu không quy định ⇒ PM chốt kỹ thuật 27/11 (kssm chỉ tham khảo, không là nguồn)

| # | Luật | Chỗ tham khảo (không phải nguồn) |
|---|---|---|
| N1 | Phiên: tạo (`packageType` + `metadataTypes` gồm cấp gói + ≥1 loại tài liệu) → tải file nhiều đợt kèm đường dẫn tương đối → bắt đầu → hỏi tiến độ → kết quả → huỷ | `KiemTraController` |
| N2 | ≤ 200 file / đợt; Excel chỉ `.xlsx`, `.xls` báo lỗi riêng (ClosedXML không đọc). Đuôi + cỡ file data: N12 (kssm chỉ nhận PDF vì chỉ làm SIP văn bản) | `KiemTraConstants`, plan kssm |
| N3 | **Không kiểm cấu trúc thư mục.** Mỗi PDF dùng Excel ở thư mục **gần nhất** chứa nó (đi ngược lên cha; Excel riêng trong thư mục con thắng Excel chung ở cha). PDF không có Excel nào áp dụng → lỗi file. 2 Excel cùng một thư mục → lỗi cả hai (kssm ghi đè ngầm bản sau — PM chốt kỹ thuật 27/11, không để chọn ngầm) | `DossierLayoutService.TimExcelGanNhat`, `VerifyRunnerService` dòng 100–110, 203–206 |
| N4 | Ghép header ↔ `DisplayName`: chuẩn hoá (bỏ dấu, chữ thường, bỏ phần trong ngoặc), điểm `max(Levenshtein, Dice theo từ)` ≥ 0,8 từng cặp; đủ 100% trường REQUIRED; header thừa bỏ qua | `HeaderMatchService`, `TextSimilarity`, plan kssm QĐ 3 |
| N5 | Soi PDF lúc nhận (SHA-256, số trang, chữ ký, PDF/A) rồi mới đẩy MinIO; bước kiểm không tải lại PDF | plan kssm "Điểm cần chú ý" |
| N6 | **Gói = một dòng sheet cấp gói** (một Excel khai được nhiều hồ sơ). PDF vào gói có `fileCode` mà `docId` = `fileCode` hoặc bắt đầu bằng `fileCode + "."` (khớp mã dài nhất); không khớp mã nào → lỗi file. Tên thư mục **không** dùng để so. Đây là cách của kssm cho SIP văn bản; luật tổng quát cho 5 loại ở N11 | `DossierLayoutService.TimMaHoSoKhopNhat` / `LaMaCuaTaiLieu`, `VerifyRunnerService` dòng 218–227 |
| N7 | Kiểm giá trị ô theo `PackageSchemaField`: REQUIRED / CONDITIONAL, `MaxLength`, NUMBER chữ số, DATE `dd/MM/yyyy`; **chỉ AIP** nhận thêm `MM/yyyy`, `yyyy` ở `startDate`, `endDate` (tr.72), `issuedDate` (tr.77–78, 149); SIP và DIP chỉ `dd/MM/yyyy` (tr.214, 219, 272, 327) — kssm cho cả DIP dạng rút gọn là **sai**, đừng chép, BOOLEAN `1`/`0`, mã thuộc `CodeGroup`, `language` nhiều mã | `MetadataValueService` + seed Task 37 |
| N8 | `SIP_hoso`: `docCode` (văn bản, ảnh, media) = `fileCode` + `.` + 7 chữ số (tr.217–218, 223, 227; GM nối bằng `.`). `SIP_tailieu`: `docCode` chỉ kiểm đuôi 7 chữ số sau dấu `.` cuối, **không** so mã gói (tr.270, 276, 280 ghép theo số lần nộp). `docCode` AIP (không có bảng mô tả, chỉ `.xsd`) và `arcDocCode`: chỉ kiểm độ dài | `KiemMaLuuTru` kssm + trang đã ghi |
| N9 | PDF không có dòng metadata → lỗi file; trùng tên file trong cùng gói → lỗi file; dòng metadata chưa có PDF → **cảnh báo** gói (không chặn) | `VerifyRunnerService` dòng 326, 341, 376–399 |
| N10 | Trạng thái phiên / gói / file: chờ → đang kiểm → đạt / cảnh báo / lỗi; huỷ. Dựng gói chỉ gói không lỗi | `KiemTraConstants`, `PackageBuildRunnerService` |
| N11 | Gom file vào gói (cụ thể hoá C11), xét lần lượt: (1) sheet cấp gói của Excel áp dụng chỉ có **1 dòng** (thường gặp ở `_tailieu`, DIP) → mọi file dùng Excel đó vào gói ấy; (2) sheet tài liệu có cột `arcFileCode` (AIP_hoso mọi loại, AIP_tailieu văn bản) → gói có mã **bằng** ô đó — xét sau (1) vì `arcFileCode` ở sheet văn bản AIP_tailieu chỉ có trong `.xsd` tr.174 (bảng mô tả tr.145–147 dùng `arcDocCode`), không nguồn nào nói nó là mã gói tin; (3) khoá tài liệu = mã gói hoặc bắt đầu bằng `mã gói + "."` → gói khớp mã dài nhất (cách của kssm); (4) không khớp → lỗi file. DIP bắt buộc sheet cấp gói đúng 1 dòng (C11) | C11 + kssm N6 |
| N12 | Đuôi file nhận theo loại tài liệu, lấy từ bảng mimetype [03 §8](../dong-goi/03-sip-metadata.md): văn bản `.pdf` (Điều 8: giấy → PDF/A; GM); ảnh `.jpg .jpeg .png .gif .tif .tiff` (03 §8) + `.pdf` (Điều 8); ghi âm / ghi hình `.mp4 .mpg .mpeg .avi .wmv .mov .qt .mp3 .wma .aac` (03 §8) + `.wav` (Điều 8). Cỡ (PM chốt kỹ thuật): PDF / ảnh ≤ 100 MB như kssm, ghi âm / ghi hình ≤ 1 GB. Soi chữ ký / PDF/A / số trang chỉ với PDF | 03 §8, Điều 8 (01 §4) |
| N13 | DIP: file xác thực (C12) đặt tên `{requestID}.pdf`, nằm cùng thư mục với Excel của gói; thiếu → lỗi gói; không phải PDF/A → cảnh báo | C12, khuôn "tên file = mã" của C11 |

Lưu ý: kssm (tham khảo) **chỉ có khoá `docId`** để ghép PDF ↔ dòng và PDF ↔ gói (`docId` bắt đầu bằng `fileCode`). Cách này chỉ
chạy được với SIP văn bản. AIP (không `docId`, mã gói `arcFileCode`) và DIP (mã gói `requestID`, `docId` không
bắt đầu bằng nó), ảnh / media (không `docId`) đều không gom được → đã chốt ở C11 (khoá file + cách gom gói).

## Dựng gói — theo [10 §2](../dong-goi/10-yeu-cau-tung-mau.md), không theo kssm

| # | Luật | Nguồn |
|---|---|---|
| D1 | Cây thư mục đúng từng phụ lục; AIP và DIP (C14) thêm `metadata/preservation/PREMIS.xml` + `rep1/metadata/preservation/PREMIS_rep1.xml` | tr.34–36, 112–113, 294–295 |
| D2 | METS gốc + rep1: thuộc tính theo bảng 10 §2 từng loại (`OBJID`, `TYPE`, `PROFILE`, `OAISPACKAGETYPE`, `ORIGINAL` / `NORMALIZED`); `amdSec` + `structMap` AIP / DIP từng phần tử theo [11 §1–3](../dong-goi/11-premis-amdsec-structmap.md) | 10 §2, 11 |
| D3 | `SIP_hoso`: bắt chước gói mẫu đủ 15 điểm 04 §10, trừ 3 lỗi 04 §11 (năm trong `docCode` lấy từ `fileCode`; sửa `arcDocCode` → `docCode` trong 3 `ead_*.xsd`; không chép `paperFileCode` rỗng — đã kiểm ở N7) | 04, 09 |
| D4 | Byte XML: UTF-8 không BOM, LF, thụt 4; ghi file xong mới băm SHA-256 viết HOA; mốc giờ có mili giây + `+07:00`; ghi rep1/METS trước METS gốc | 09 §3–4 |
| D5 | `simpledc`: phát đủ thẻ của schema; `SIP_hoso` cấp gói + văn bản theo **thứ tự gói mẫu**, còn lại theo `SortOrder` (`.xsd`); ô trống phát thẻ rỗng tự đóng | 09 §6, 10 §1 |
| D6 | `schemas/`: 4 file METS / xlink / DILCIS chép nguyên byte từ `goi-mau/`; 4 `ead*.xsd` **sinh theo bộ trường của đúng loại gói** (seed Task 37), khuôn byte như gói mẫu (SIP_hoso dùng `docCode`, sửa lỗi #2). Luôn phát đủ 8 file kể cả khi phiên chỉ có văn bản (GM nộp cả 4 `ead*.xsd`) | 09 §7 |
| D7 | `EAD_DOC_{khoá}.xml` + `data/{khoá}.pdf` — khoá theo C11 | GM, 04 §10 #11–12 |
| D8 | AIP: OBJID `urn:{mã phông}:uuid:{UUID HOA}` ⇒ bước dựng **bắt buộc nhập mã phông** (soHoaSaoMai cũng gửi `maPhong`); thư mục gói đổi `:` → `_` | tr.38, 115; thư mục tr.34, 111 |
| D9 | PREMIS AIP + DIP theo [11 §4](../dong-goi/11-premis-amdsec-structmap.md): gốc 1 `intellectualEntity`, rep1 mỗi file 1 `file` (`fixity` + `size` + `format` theo ví dụ tr.95–96), 1 `event` (`transfer` / `dissemination`), 1 `agent` (C10); không `signatureInformation` (không bắt buộc) | tr.90–99 (AIP_hoso, `transfer` ở tr.97), tr.169 (AIP_tailieu) |
| D10 | ZIP: tên GUID chữ thường `.zip`, trong ZIP là thư mục gói; MinIO `packaging/{sessionId}/packages/{guid}.zip`; dựng ra file tạm, không giữ cả gói trong RAM | GM, 03 §9, C5 |
| D11 | DIP: EAD cấp gói = 7 trường yêu cầu khai thác lấy từ sheet cấp gói; tệp xác thực theo C12; PREMIS theo C14 | tr.348, 07 |
| D12 | Kiểm đầu ra: gói `SIP_hoso` dựng lại từ dữ liệu gói mẫu phải trùng cây + bộ thẻ + cấu trúc METS với `goi-mau/` (khác UUID / giờ / checksum), checksum tự kiểm 100% | 09 |

## 5. Câu hỏi chờ người dùng (gom 1 lượt)

Q1–Q7 ([10 §4](../dong-goi/10-yeu-cau-tung-mau.md)) đã trả lời 27/11 → C9–C12, C14. Còn cần người dùng xác nhận:

- X1 (từ trả lời Q1) — người dùng xác nhận 27/11 → C13: **khuôn** gói mẫu (`amdSec` rỗng ở SIP, `MDTYPE="DC"`, `1.0.0`, `application/xml`, schema chữ
  thường, `EAD_DOC_` hoa, `OBJID` rep1 = `rep1`, ZIP GUID) áp cho cả 5 loại ở những chỗ phụ lục không ghi khác hẳn;
  chỗ phụ lục ghi khác hẳn (OBJID AIP, OAISPACKAGETYPE, PROFILE, PREMIS, `NORMALIZED`) theo bảng của phụ lục.
  Phiên bản phần mềm ghi thêm `note csip:NOTETYPE="SOFTWARE VERSION"` ở agent METS gốc (thông tư tr.41, 189, 300)
  bên cạnh note `IDENTIFICATIONCODE` = mã phông của gói mẫu.

## Review plan — 2026-11-27, 4 lượt (đối chiếu thông tư + gói mẫu)

- Lượt 1: bỏ nháp Task 65 cũ (so `docCode` với mã gói cho mọi SIP — sai tr.270; tự chọn khoá PDF); OBJID AIP theo
  bảng tr.38 (D8); gói mẫu 137/137 PDF đã ký + PDF/A-2b nên C7 không trái gói mẫu; `totalDoc` ≠ số file không thành luật.
- Lượt 2: ngày rút gọn chỉ AIP (N7); `docCode` từng loại (N8); thứ tự gom gói (N11); đuôi file (N12); 2 Excel (N3).
- Lượt 3: tên từng thẻ seed ↔ `.xsd` 20 khối — AIP / DIP trùng cả thứ tự, SIP thêm `riskRecovery*` theo GM; trang
  `.xsd` DIP 348, `transfer` AIP_tailieu tr.169, thư mục `:`→`_` tr.34 / 111 / 293; DIP PREMIS → Q5 → C14; kssm
  không còn là nguồn.
- Lượt 4: rà từng phần tử `amdSec` (6 bảng), `structMap` gốc / rep1, PREMIS (3 bảng) → [11](../dong-goi/11-premis-amdsec-structmap.md);
  Q6 `xsi:type`, Q7 DIP `dissemination`; bỏ "(task: …)" ở C10; ADMID = `amdSec`; `AttachmentFile` chỉ khi có đính kèm (GM).

## Bước (sau khi duyệt)

1. (xong) Ghi trả lời Q1–Q5 + X1 vào plan + 10 §4.
2. (xong) Viết Task 65 (kiểm: C1–C14, N1–N13) và Task 66 (dựng: C5–C14, D1–D12) — mỗi case trỏ mã luật của plan.
3. Ánh: mỗi task làm Bước 0 (đọc tài liệu + ghi mục "Thi công Task 6x"), PM duyệt, rồi code. Task 65 (xong): [dong-goi-tt05-thi-cong-65.plan.md](dong-goi-tt05-thi-cong-65.plan.md); Task 66 (xong): [dong-goi-tt05-thi-cong-66.plan.md](dong-goi-tt05-thi-cong-66.plan.md).
