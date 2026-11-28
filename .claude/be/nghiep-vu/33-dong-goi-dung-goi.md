# Đóng gói — dựng gói TT05 thành ZIP (Task 66)

> Cập nhật 2026-11-28. Luật gốc: [../plans/dong-goi-tt05.plan.md](../plans/dong-goi-tt05.plan.md) (mã C / D), thi công + bảng thuộc
> tính METS từng phần tử: [../plans/dong-goi-tt05-thi-cong-66.plan.md](../plans/dong-goi-tt05-thi-cong-66.plan.md). Đầu vào là phiên đã
> kiểm của [32](32-dong-goi-phien-kiem.md); bộ trường + thứ tự thẻ: [16](16-dong-goi-cau-hinh.md).

Phiên đã kiểm xong (`passed` / `warning` / `failed`) mà còn ≥ 1 gói không lỗi thì dựng được: mỗi gói không lỗi ra **1 ZIP** chứa đúng
1 thư mục gói (METS + EAD + schemas + data, AIP / DIP thêm PREMIS), lưu MinIO, trả link tải tạm.

## Dữ liệu — dùng cột sẵn của Task 65 + 2 cột mới

| Bảng | Cột |
|---|---|
| `PackageSession` | `BuildStatus` (`running` / `done` / `failed`), `BuildTotal` = số gói không lỗi, `BuildDone`, `BuildFailReason`, `FondsCode`, `BuiltBy` (UserName người bấm), **mới** `ArchiveCode` / `ArchiveName` nvarchar(100) null (migration `Packaging/Add_PackageSession_Archive`) |
| `PackageDossier` | `PackageObjid`, `PackageObjectKey`, `PackageFileName`, `PackageSize`; gói dựng lỗi ghi nối vào `ErrorSummary` sau tiền tố "Dựng gói lỗi: " |

Lưu ý: Migration 2 cột **chưa** `database update` lên `SO_HOA_DB` (DB chung của nhóm), PM chạy khi merge.

## API — `api/packaging/session`, quyền `FUNCTION_PACKAGE_BUILD`

| API | Việc |
|---|---|
| `POST {id}/build` | `{fondsCode, archiveCode?, archiveName?}` → trả tiến độ. Lỗi: không có phiên `706`; phiên chưa kiểm xong hoặc không còn gói không lỗi `710`; đang dựng `711`; `fondsCode` trống / > 50 ký tự / ngoài `[A-Za-z0-9_-]` `712`; cơ quan lưu trữ chỉ nhập một nửa hoặc > 100 ký tự `713`; không có server Hangfire queue `default` → lỗi như `start` |
| `GET {id}/build-progress` | `buildStatus`, `buildTotal`, `buildDone`, `buildFailReason` (FE hỏi định kỳ như `progress`) |
| `GET {id}/packages` | Gói đã có ZIP: `packageCode`, `packageObjid`, `packageFileName`, `packageSize`, `downloadUrl` (link ký sẵn 1 giờ) |
| `GET {id}/result` (Task 65) | Trả thêm `fondsCode`, `archiveCode`, `archiveName` để FE điền sẵn form dựng lại |

`archiveCode` / `archiveName` được trim, chuỗi trắng coi như không nhập; nhập đủ cả hai thì AIP / DIP phát agent `ARCHIVIST`.

## Luồng

1. `Build` (`PackageSession.StartBuild`): kiểm điều kiện + giá trị, ghi `FondsCode`, `ArchiveCode`, `ArchiveName`, `BuiltBy`, đặt
   `running`, `BuildDone = 0`. Mọi gói `ResetBuild`: xoá 4 cột `Package*` và phần "Dựng gói lỗi" cũ trong `ErrorSummary`.
2. Lưu DB + audit "Dựng gói TT05", **rồi** `DeleteByPrefixAsync("packaging/{id}/packages/")` xoá ZIP lần trước, rồi mới `Enqueue`
   `PackageBuildJob` (xoá trước khi đẩy job để không xoá nhầm ZIP job mới vừa tải lên).
3. `BuildFromJob`: phiên không còn `running` thì thôi. Đọc bộ trường đủ 4 loại metadata (schemas luôn 8 file), đọc Excel của từng gói
   (cache theo file Excel), ghép cột lại bằng `MatchSheetColumns` (cùng hàm Task 65, kết quả trùng `MatchReport` khi cấu hình không đổi).
4. Mỗi gói: dựng `PackageArchiveDto` → `IPackageArchiveExternal.BuildAsync` → `CompleteBuild`; lỗi thì `PackageDossier.FailBuild`, gói
   khác chạy tiếp. Mỗi gói xong `AddBuildProgress` + `SaveChanges`.
5. Hết: `done` nếu ≥ 1 gói có ZIP, không thì `failed` "Không dựng được gói nào, xem lỗi từng gói". Lỗi bất ngờ → `FailBuild` cả phiên.

`PackageArchiveExternal.BuildAsync` (thư mục tạm `%TEMP%/sh-package-{guid}/`, xoá trong `finally`):

- Tải PDF / ảnh / âm thanh từ MinIO bằng `DownloadToAsync` (ghi thẳng ra file, không nạp RAM), so lại SHA-256 với `Sha256` đã lưu
  lúc kiểm; lệch → gói lỗi.
- Ghi file theo thứ tự data → `EAD_*` → `EAD.xml` → schemas → `PREMIS_rep1` → rep1 `METS` → `PREMIS` → `METS` gốc (METS chứa checksum
  của file ghi trước nó), băm lại 100% file trên đĩa, nén `ZipFile.CreateFromDirectory`, `UploadPrivateAsync`
  `packaging/{id}/packages/{guid}.zip` (`application/zip`).

## Cây gói

| Đường dẫn | Loại gói |
|---|---|
| `METS.xml`, `metadata/descriptive/EAD.xml`, `schemas/` (8 file) | mọi loại |
| `representations/rep1/METS.xml`, `rep1/metadata/descriptive/EAD_{DOC\|PIC\|MEDIA}_{DocKey}.xml`, `rep1/data/{DocKey}{đuôi}` | mọi loại |
| `metadata/preservation/PREMIS.xml`, `rep1/metadata/preservation/PREMIS_rep1.xml` | AIP_hoso, AIP_tailieu, DIP |
| `documentation/{requestID}.pdf` (file xác thực người dùng đưa) | DIP |

Thư mục gói = `OBJID` đổi `:` thành `_`. `OBJID`: AIP `urn:{fondsCode}:uuid:{UUID HOA}`, SIP / DIP `uuid-{UUID HOA}`. Tên ZIP = GUID
chữ thường.

## Khuôn byte và XML

- UTF-8 không BOM, LF, thụt 4 cách, file XML kết thúc bằng `\n`. METS `standalone="yes"`, EAD `standalone="no"`, PREMIS không có
  `standalone`. Mốc giờ `yyyy-MM-ddTHH:mm:ss.fff+07:00`. `CHECKSUM` SHA-256 HOA; ID `uuid-{UUID HOA}`, file `ID-{UUID HOA}`.
- Ghi bằng `Storage/Base/PackageXmlBuilder` (StringBuilder), **không** `XmlWriter`: `XmlWriter` ghi `encoding="utf-8"` và `<x />` có
  cách, lệch byte gói mẫu. Thẻ trống ra `<x/>`; thuộc tính `null` thì bỏ, chuỗi rỗng vẫn ghi (`OTHERTYPE=""`).
- METS theo loại:

| | AIP_hoso | AIP_tailieu | SIP_hoso | SIP_tailieu | DIP |
|---|---|---|---|---|---|
| `TYPE` | Mixed | Collection | Mixed | Collection | MIXED |
| `PROFILE` gốc | ra.ee IP | ra.ee IP | E-ARK-CSIP | E-ARK-CSIP | E-ARK-DIP |
| div rep1 | NORMALIZED | NORMALIZED | ORIGINAL | ORIGINAL | ORIGINAL |
| `amdSec` | digiprovMD → PREMIS | như trái | rỗng có ID | rỗng có ID | digiprovMD → PREMIS |
| agent rep1 | CREATOR + fondsCode (+ ARCHIVIST) | như trái | CREATOR + fondsCode | như trái | DISSEMINATOR + requestID (+ ARCHIVIST) |

  Agent gốc mọi loại: `CREATOR / OTHER / SOFTWARE`, `name` = `Packaging:SoftwareName`, note `IDENTIFICATIONCODE` = fondsCode + note
  `SOFTWARE VERSION`. `altRecordID SUBMISSIONAGREEMENT` = mã gói chỉ có ở SIP. `LABEL` gốc = ô `title` cấp gói, DIP để `""`.
- EAD: bộ thẻ = trường seed của schema, ghi **nguyên ô** Excel. Thứ tự: SIP_hoso cấp gói + văn bản theo gói mẫu
  (`PackageBuildConst.SIP_HOSO_ELEMENT_ORDERS`), còn lại theo `SortOrder`.
- Schemas: 4 file `mets1_12.xsd`, `xlink.xsd`, `DILCISExtension*.xsd` là EmbeddedResource `Storage/Files/*.xsd` chép nguyên byte gói
  mẫu; 4 file `ead*.xsd` sinh theo bộ trường của đúng loại gói, khuôn byte gói mẫu, **không** xuống dòng cuối, thẻ theo `SortOrder`
  (không theo thứ tự EAD: `ead.xsd` gói mẫu khác thứ tự `EAD.xml`).
- PREMIS v3 ([11 §4](../dong-goi/11-premis-amdsec-structmap.md)): thẻ không tiền tố, khai cả `xmlns` mặc định lẫn `xmlns:premis`.
  `PREMIS.xml` 1 `intellectualEntity` (OBJID); `PREMIS_rep1.xml` mỗi file data 1 `file` (Doc / Pic / Media = ID file trong rep1 METS,
  digest chữ thường, size, mimetype); 1 event (`transfer` AIP, `dissemination` DIP) + 1 agent phần mềm. Không nhúng `premis.xsd`.
- `appsettings` `Packaging`: `SoftwareName` "Thi công số hoá", `SoftwareVersion` "1.0.0" (`IOptions<PackagingSettings>`).

## Sửa kèm của Task 65

- M2-G18: khối catch của `VerifyFromJob` đọc lại trạng thái; phiên đã `cancelled` (huỷ xoá file làm job đọc Excel lỗi) thì không `Fail`.
- M2-G19: `ObjectKey` file nguồn = `packaging/{id}/source/{Guid N}{đuôi}` thay số thứ tự, 2 đợt `files` song song không còn trùng khoá.

## Đã kiểm thực tế — 2026-11-28

Chạy BE thật trên LocalDB riêng + `s3rver` giả MinIO (MinIO dev đầy ổ), công cụ C# ngoài repo giải nén + kiểm:

- SIP_hoso dựng lại từ dữ liệu gói mẫu (66.10): cây, tên file, bộ thẻ + thứ tự thẻ mọi XML, thuộc tính METS trùng gói mẫu; 4 xsd chép +
  `ead.xsd` trùng byte; chỉ khác UUID, giờ, checksum, agent (giá trị hệ thống) và 3 lỗi gói mẫu đã sửa.
- Đủ 5 loại gói + phiên văn bản + ảnh: checksum trong 2 METS khớp 100%, METS qua `mets1_12.xsd`, EAD qua `ead*.xsd`, PREMIS qua
  `premis.xsd` v3 LOC (tải về chỉ để test), TYPE / PROFILE / OAIS / div rep1 / amdSec / ARCHIVIST / Documentation đúng bảng trên.
- `710`–`713`, `706`; dựng lại xoá ZIP cũ, bỏ trống cơ quan lưu trữ thì DB về null và gói mới không còn ARCHIVIST; Leader `403` cả
  3 API; xoá tay PDF trên kho → gói đó lỗi, gói kia vẫn có ZIP, `BuildDone` đủ; cả 2 gói lỗi → `failed`, dựng lại không nhân đôi lỗi;
  huỷ đúng lúc job kiểm đang đọc Excel → giữ `cancelled`; 2 đợt tải song song 191 file → 191 khoá khác nhau; không sót thư mục tạm.

Lưu ý: Link tải chỉ kiểm tham số `X-Amz-Expires` ≈ 3600, chưa chờ hết hạn thật. Chưa kiểm trên MinIO thật.

## Giới hạn đã biết

- Dựng tuần tự từng gói trong 1 job, mỗi gói tải hết file về đĩa tạm: gói ghi âm / ghi hình lớn cần đủ chỗ trống ở `%TEMP%`.
- Đổi cấu hình trường giữa lúc kiểm và lúc dựng thì job ghép cột theo cấu hình mới, có thể khác `MatchReport`: nên kiểm lại phiên.
- Ngoài phạm vi: `.fetch.txt`, sinh file xác thực DIP, chuyển PDF/A, ký số gói, chuyển SIP → AIP, màn FE, nộp kho M3.

> **Tiếp:** [README.md](README.md) — mục lục nghiệp vụ.
