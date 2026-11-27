# Thi công Task 65 — phiên kiểm tra đóng gói

> Trạng thái: đã thi công 2026-11-28 (code + build sạch, chưa commit). Tách từ [dong-goi-tt05.plan.md](dong-goi-tt05.plan.md)
> vì plan gốc chạm trần 100 dòng. Nghiệp vụ chạy thật: [../nghiep-vu/32-dong-goi-phien-kiem.md](../nghiep-vu/32-dong-goi-phien-kiem.md).

## Input

- Luật C1–C14, N1–N13 của plan gốc; bộ trường + mã 20 schema (Task 36, [16](../nghiep-vu/16-dong-goi-cau-hinh.md)); Excel mẫu Task 37.
- Hạ tầng có sẵn: `IFileStorageExternal` (private, xoá theo tiền tố), `IExcelReaderExternal`, `PdfSignatureReader`, Hangfire.
- Được duyệt trong task: 3 bảng + 1 migration, 2 key quyền, mã lỗi `704`–`719`.

## Đã làm theo tầng

| Tầng | Thêm |
|---|---|
| shared | `PackageSessionConst` (trạng thái, vai trò file, giới hạn, đuôi theo loại metadata, MimeType); `MENU_PACKAGE_BUILD`, `FUNCTION_PACKAGE_BUILD`; lỗi `704`–`709`; 4 action audit; cron `PackageSessionCleanup` |
| domain | Entity `PackageSession`, `PackageDossier`, `PackageDocument`; `IPackageSessionRepository`; `PackageSessionDomainService` (kiểm metadataTypes, chuẩn đường dẫn, nhận file, ghép cột, khoá tài liệu, kiểm ô, `BuildVerifyPlan`) |
| applications | `IPackageSessionService` + `PackageSessionService`; `IPackageSessionQuery`; `IPackageFileInspectExternal` + `PackageFileInspectDto`; `PackageVerifyJob`, `PackageSessionCleanupJob`; DTO thư mục `Dtos/PackageSession/` |
| infrastructure.data | Repository, `PackageSessionQuery` (`ProjectTo`), `PackageConfigQuery.GetVerifyConfig`; migration `Packaging/Add_PackageSession` (3 bảng + 2 index `SessionId`) |
| infrastructure.external | `DataReader/PackageFileInspectExternal` (SHA-256, số trang, chữ ký, PDF/A từ XMP) |
| api | `PackageSessionController` 7 API `packaging/session`; DI; đăng ký recurring job |

## Khoá tài liệu theo schema (đếm từ `PackagingSeedData`)

| Loại gói | Văn bản | Phim, ảnh | Ghi âm, ghi hình |
|---|---|---|---|
| `AIP_hoso` | `docCode` | `arcFileCode` + `docOrdinal` | `arcFileCode` + `docOrdinal` |
| `AIP_tailieu` | `docCode` | `arcDocCode` | `arcDocCode` |
| `SIP_hoso` | `docId` | `docCode` | `docCode` |
| `SIP_tailieu` | `docId` | `docCode` | `docCode` |
| `DIP` | `docId` | `arcDocCode` | `arcDocCode` |

Thứ tự ưu tiên `docId` → `docCode` → `arcDocCode` → `arcFileCode` + "." + `docOrdinal` (đệm 7 số), lấy cái đầu tiên schema có.

## Lệch so với task / tự chọn

- Hàm ghép cột ở `PackageSessionDomainService` (domain), không ở applications như task ghi — quy ước 07: phép tính thuần ở DomainService.
- Mã lỗi chỉ dùng `704`–`709`: `706` không có phiên, `707` phiên không còn chờ, `708` số file / đợt, `709` thiếu đường dẫn thư mục.
- Phiên `failed` cả khi lỗi nằm ngoài mọi gói (Excel lỗi, file không có dòng) — không để lỗi bị giấu sau các gói `passed`.
- Trùng tài liệu trong gói xét theo khoá (tên file bỏ đuôi), không theo SHA-256.
- `.wav` → `audio/wav` (03 §8 không có); Excel không giới hạn cỡ riêng (chỉ trần 2 GB / request).
- Cron dọn là setting riêng `PackageSessionCleanup` (`0 2 * * *`), không gộp vào cron xuất Excel.
- Không viết comment trong code theo yêu cầu người dùng — lệch mục Comment của 07-conventions.

## Kiểm

- `dotnet build sh.be/sh.be.api/sh.be.api.sln` sạch; `has-pending-model-changes` → "No changes".
- API thật: các mã `701`, `702`, `704`–`709`, nhận / bỏ / từ chối file. Luồng kiểm ca 1–18 bằng harness (gọi thẳng domain + external,
  đọc file local) — chi tiết ở [32](../nghiep-vu/32-dong-goi-phien-kiem.md#đã-kiểm-thực-tế--2026-11-28).
- BE thật trên LocalDB riêng: ca 19, 20, 21 và job kiểm lỗi Excel đạt. Chưa kiểm: tải lên MinIO thật (MinIO dev đầy ổ).

## Điểm cần chú ý

- Migration chưa áp lên DB chung (đã gỡ sau khi thử): `SO_HOA_DB` là DB cả nhóm, server Hangfire máy khác chưa có code mới sẽ
  Failed job dọn. Áp khi merge xong và cả nhóm đã pull.
- Task 66 dùng lại: `PackageDossier.Status`, `DocKey`, `ObjectKey`, `Sha256`, `PdfAPart`, cột `Build*` / `Package*` đã có sẵn.

> **Tiếp:** [dong-goi-tt05.plan.md](dong-goi-tt05.plan.md) — luật gốc, Task 66 dựng ZIP.
