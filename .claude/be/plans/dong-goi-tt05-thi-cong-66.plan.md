# Thi công Task 66 — dựng gói TT05 (Bước 0)

> Trạng thái: **đã duyệt 28/11** (viết 2026-11-28, chưa code; chốt ở mục "PM chốt 28/11"). Tách từ [dong-goi-tt05.plan.md](dong-goi-tt05.plan.md). Phần dưới bổ sung
> metsHdr / dmdSec / fileSec cho AIP + DIP mà [11](../dong-goi/11-premis-amdsec-structmap.md) chưa rà. Nguồn: `tr.N` = trang thông tư,
> `GM` = gói mẫu, `C13` = khuôn gói mẫu áp cho 5 loại. Đã đọc lại tr.37–62, 114–133, 296–313 và 2 METS của `goi-mau/`.

## Input

- Task 65 xong: phiên, gói (`MatchReport`, `RowIndex`, `DocKey`), file (`ObjectKey`, `Sha256`, `MimeType`); cột build đã có; thêm 1 migration 2 cột cơ quan lưu trữ (P3).
- Luật C5–C14, D1–D12 của plan gốc; bảng 10 §2; amdSec / structMap / PREMIS theo 11 §1–4 (đối chiếu lại trang gốc: không thấy lệch).

## Thẻ `mets` (gốc + rep1)

| Thuộc tính | Gốc | rep1 | Nguồn |
|---|---|---|---|
| namespace + thứ tự khai | như GM (`xsi`, `sip`, METS, `csip`, `xlink`) | như gốc | GM, C13 |
| `OBJID` | AIP `urn:{fondsCode}:uuid:{UUID HOA}`; SIP, DIP `uuid-{UUID HOA}` | `rep1` | tr.38, 115, 297; rep1: C13 (bảng tr.52, 309 ghi uuid, C13 đã chốt theo GM) |
| `LABEL` | `title` cấp gói (AIP_hoso, AIP_tailieu, SIP); DIP `""` | `""` | tr.38 "trường title", tr.115; GM; DIP: P2 |
| `TYPE` | AIP_hoso / SIP_hoso `Mixed`; AIP_tailieu / SIP_tailieu `Collection`; DIP `MIXED` | = gốc | tr.38, 116, 130, 244, 297, 309 |
| `csip:CONTENTINFORMATIONTYPE` | `MIXED` | `MIXED` | GM; bắt buộc ở rep1 tr.39, 298 |
| `PROFILE` | AIP `http://www.ra.ee/METS/v01/IP.xml`; SIP E-ARK-CSIP; DIP E-ARK-DIP | E-ARK-CSIP mọi loại | tr.39, 116, 298; rep1: ví dụ tr.52, 130, 310 + GM |
| `xsi:schemaLocation` | 4 cặp trỏ `schemas/…` | `../../schemas/…` | GM (ví dụ DIP tr.296 trỏ URL ngoài, C13 theo GM) |

## `metsHdr`

| Phần tử | Giá trị | Nguồn |
|---|---|---|
| `CREATEDATE` = `LASTMODDATE`, `RECORDSTATUS` | giờ dựng (mili giây, `+07:00`), `NEW` | tr.39–40, 53, 299, 310; GM |
| `csip:OAISPACKAGETYPE` (gốc + rep1) | `AIP` / `SIP` / `DIP` theo loại | tr.40, 53, 299, 311 (ví dụ tr.311 ghi `SIP`, theo bảng) |
| agent gốc | `ROLE="CREATOR" TYPE="OTHER" OTHERTYPE="SOFTWARE"`, `name` = `SoftwareName`, note `IDENTIFICATIONCODE` = `fondsCode` + note `SOFTWARE VERSION` = `SoftwareVersion` | tr.40–41, 117–118, 299–300 (`SOFTWARE VERSION` mặc định); GM (`IDENTIFICATIONCODE`); C13 |
| agent rep1 AIP | `ROLE="CREATOR" TYPE="INDIVIDUAL" OTHERTYPE=""`, `name` = `BuiltBy`, note `IDENTIFICATIONCODE` = `fondsCode` | tr.54, 131–132: CREATOR + IDENTIFICATIONCODE = mã phông, `TYPE` cho phép `INDIVIDUAL`; khuôn GM. Không cần tên cơ quan |
| agent rep1 DIP | `ROLE="DISSEMINATOR" TYPE="INDIVIDUAL" OTHERTYPE=""`, `name` = `BuiltBy`, note `IDENTIFICATIONCODE` = `requestID` | tr.311: note chỉ định nghĩa cho `ARCHIVIST` (mã cơ quan lưu trữ) và `DISSEMINATOR` (mã yêu cầu khai thác); P1 |
| agent rep1 `ARCHIVIST` (AIP + DIP) | chỉ khi `POST build` có `archiveCode` + `archiveName`: `ROLE="ARCHIVIST" TYPE="ORGANIZATION" OTHERTYPE=""`, `name` = `archiveName`, note `IDENTIFICATIONCODE` = `archiveCode`, đứng sau agent đầu; không nhập thì không phát. SIP không phát (GM) | tr.54–55, 132, 311; P3 |
| `altRecordID` | SIP: `TYPE="SUBMISSIONAGREEMENT"` = `fileCode`; AIP, DIP: không phát | GM; tr.247; 10 §2 |

## `dmdSec`

| Phần tử | Giá trị | Nguồn |
|---|---|---|
| gốc: 1 khối → `metadata/descriptive/EAD.xml`; rep1: mỗi file data 1 khối → `metadata/descriptive/EAD_{DOC|PIC|MEDIA}_{DocKey}.xml` | `ID` uuid, `CREATED`, `STATUS="CURRENT"` | tr.41, 55; GM |
| `mdRef` | `ID`, `LOCTYPE="URL"`, `MDTYPE="DC"`, `MDTYPEVERSION="1.0.0"`, `xlink:type="simple"`, `xlink:href`, `MIMETYPE="application/xml"`, `SIZE`, `CREATED`, `CHECKSUM` HOA, `CHECKSUMTYPE="SHA-256"` | tr.41–42 (ví dụ `OTHER` / `1.0` / `text/xml`), C13 theo GM |

## `fileSec`

| Phần tử | Giá trị | Nguồn |
|---|---|---|
| gốc `fileGrp USE="Schemas"` | 8 file theo thứ tự GM, `application/octet-stream` | GM, tr.45–47 |
| gốc `fileGrp USE="Documentation"` | **chỉ DIP**: 1 file `documentation/{requestID}.pdf`, `application/pdf`, đặt giữa Schemas và rep1 | tr.304 (USE `Documentation`), C12; thứ tự nhóm: PM chốt kỹ thuật |
| gốc `fileGrp USE="Representations/rep1"` | 1 file `representations/rep1/METS.xml`, `application/xml` | GM |
| rep1 `fileGrp USE="Data"` | mỗi file data 1 `file`, `MIMETYPE` = `PackageDocument.MimeType`, `FLocat` → `data/{DocKey}{đuôi}` | GM, tr.59–61 |
| `file` | `ID="ID-{UUID HOA}"`, `SIZE`, `CREATED`, `CHECKSUM`, `CHECKSUMTYPE`; `FLocat xlink:type="simple" LOCTYPE="URL"` | tr.46, 60; GM |
| không liệt | `EAD*.xml`, `PREMIS*.xml` (chỉ qua `dmdSec` / `amdSec`) | GM (EAD_* không trong fileSec) |

## PM chốt 28/11

- P1. Agent rep1 DIP = `DISSEMINATOR` + `requestID` (nguồn tr.311), không theo khuôn GM `CREATOR`.
- P2. `LABEL` gốc DIP = `""` (tr.297 không bắt buộc, cấp gói DIP không có `title`; giữ khuôn luôn có `LABEL` như GM).
- P3 (đổi so với bản Ánh viết: **lưu DB**). `POST build {fondsCode, archiveCode?, archiveName?}`; nhập đủ cả hai thì phát agent
  `ARCHIVIST`, chỉ một trong hai → `713`; mỗi cái ≤ 100 ký tự, trim, chuỗi trắng coi như không nhập.
  - Thêm 2 cột null vào `PackageSession`: `ArchiveCode` nvarchar(100), `ArchiveName` nvarchar(100). Migration riêng
    `Packaging/Add_PackageSession_Archive` chỉ thêm 2 cột (được phép, ngoại lệ của luật "không migration" trong task).
  - `PackageSession.StartBuild(fondsCode, archiveCode, archiveName, builtBy, now)` ghi cả 4 giá trị cùng `FondsCode` / `BuiltBy` có sẵn.
    Job **đọc từ phiên**, không nhận qua tham số job.
  - Dựng lại: body vẫn gửi đủ, ghi đè giá trị cũ (gửi trống thì xoá, không phát `ARCHIVIST`). `GET .../result` trả thêm `fondsCode`,
    `archiveCode`, `archiveName` để FE điền sẵn form, người dùng không phải gõ lại.
- PREMIS (M2-G20): thông tư không bắt buộc kèm hay kiểm theo `premis.xsd` (tr.34–35: `schemas/` của AIP chỉ có `METS.xsd` + `EAD*.xsd`;
  tr.90–91: `schemaLocation` trỏ URL LOC). **Không nhúng `premis.xsd`**, không đóng vào gói; PREMIS kiểm theo khuôn tài liệu 11.
  Có thể tải bản LOC về kiểm tay một lần lúc test, không đưa vào repo.
- Sửa luôn trong task này: M2-G18 (khối catch của `VerifyFromJob` đọc lại trạng thái, phiên đã `cancelled` thì không `Fail`),
  M2-G19 (`AddFiles` hai đợt song song không được trùng `ObjectKey`: đặt khoá theo Guid thay số thứ tự đếm).
- P4. Được sửa `sh.be.infrastructure.external.csproj`: thêm `<EmbeddedResource>` cho 4 `.xsd` của `goi-mau/`, không đổi tham chiếu.

## Class sẽ tạo

| Tầng | Class |
|---|---|
| shared | `PackageBuildConst` (trạng thái dựng, PROFILE, namespace, hằng METS); `PackagingSettings` (`SoftwareName`, `SoftwareVersion`); lỗi `710`–`713` (dựng sai điều kiện, đang dựng, `fondsCode` sai, cơ quan lưu trữ thiếu một nửa) |
| domain | `PackageSession.StartBuild` / `AddBuildProgress` / `FinishBuild`; `PackageDossier.CompleteBuild` / `FailBuild`; `PackageSessionDomainService.EnsureValidFondsCode`, `BuildArchivePlan` (OBJID, đường dẫn, thứ tự thẻ EAD, ID) |
| applications | `IPackageBuildService` + `PackageBuildService`, `PackageBuildJob`, DTO `Dtos/PackageBuild/`; `IPackageArchiveExternal` (`Storage`) |
| infrastructure.external | `Storage/Implements/PackageArchiveExternal`: ghi cây ra thư mục tạm, sinh METS / EAD / `ead*.xsd` / PREMIS bằng `XmlWriter` (UTF-8 không BOM, LF, thụt 4), băm, tự kiểm checksum, nén ZIP |
| api | 3 action trong `PackageSessionController`; `Packaging` trong `appsettings.json`; DI |

## Expected output

Mỗi gói không lỗi ra 1 ZIP `packaging/{id}/packages/{guid}.zip`; SIP_hoso dựng lại từ dữ liệu gói mẫu trùng cây, bộ thẻ, thứ tự thẻ
(66.10); 2 METS qua `mets1_12.xsd`; PREMIS đúng khuôn 11; build sạch, `has-pending-model-changes` sạch sau migration 2 cột,
comment theo 07.

> **Tiếp:** [dong-goi-tt05.plan.md](dong-goi-tt05.plan.md) — luật gốc C / N / D.
