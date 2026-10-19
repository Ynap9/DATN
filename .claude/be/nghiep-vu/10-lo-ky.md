# Lô ký (DigitalSignature)

> **Phần 10/12** · trước: [09-mau-chu-ky.md](09-mau-chu-ky.md) · mục lục: [README.md](README.md)

Lô ký (`digitalsignature.SigningBatch`) gom các file PDF người dùng tải lên để ký số hàng loạt bằng USB token qua plugin
theo một mẫu chữ ký. Mỗi file là một dòng `digitalsignature.SigningBatchFile`. Kiến trúc ký và thứ tự thi công:
[../plans/ky-so-local.plan.md](../plans/ky-so-local.plan.md). Trang này mô tả phần **đã chạy** (bước 2: dữ liệu lô,
2026-10-17); phiên ký, runner, đưa thư, tạm dừng / huỷ nằm ở mục 🔬 cuối file.

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Lô đang dở của chính mình (`SIGNING` / `PAUSED` mới nhất, không có → `data = null`) | `GET api/digital-signature/signing-batch/running` | `Function.DigitalSignature.SigningBatch.View` |
| Danh sách **toàn bộ** file của lô, theo `Order` | `GET api/digital-signature/signing-batch/{id}/files` | `Function.DigitalSignature.SigningBatch.View` |
| Tạo lô (`templateId`, `signedFolderName`), trả lô | `POST api/digital-signature/signing-batch` | `Function.DigitalSignature.SigningBatch.Create` |
| Thêm file (multipart `files`), trả lô | `POST api/digital-signature/signing-batch/{id}/files` | `Function.DigitalSignature.SigningBatch.Create` |
| Xoá một file, trả lô | `DELETE api/digital-signature/signing-batch/{id}/files/{fileId}` | `Function.DigitalSignature.SigningBatch.Delete` |

Lô trả về (`FindSigningBatchByIdDto`): `id`, `templateId`, `signedFolderName`, `certificateThumbprint`, `status`
(`SigningBatchConst`: `0` NEW · `1` SIGNING · `2` DONE · `3` CANCELLED · `4` FAILED · `5` PAUSED), `totalFiles`,
`signedFiles`, `failedFiles`, `stopReason`, `startedAt`, `finishedAt`, `createdDate`. Danh sách file **lấy một lần** khi mở
màn hình; lô 10.000 file vẫn trả hết.

## Luật

| Luật | Nằm ở |
|---|---|
| **Chỉ người tạo lô** xem / sửa được lô, người khác → `616` (kssm không kiểm) | `SigningBatch.EnsureOwnedBy` |
| Mẫu chữ ký phải còn, sai → `601` | `SigningBatchService.Create` |
| Tên đợt ký bỏ `/`, `\`, ký tự điều khiển rồi trim; rỗng → `605` (`"Đợt/ký 01"` thành `"Đợtký 01"`) | `SigningBatch.NameSignedFolder` |
| Chỉ thêm file khi lô `NEW` → `607`; lô dự án → `606` | `SigningBatch.CreateFile`, `EnsureAcceptUpload` |
| Tối đa **200 file / lượt**, vượt → `617`; thân request tối đa 512 MB | `SigningBatchFilePolicy.EnsureUploadCount`, `[RequestSizeLimit]` |
| File rỗng, không đuôi `.pdf`, **trùng tên trong lô** (không phân biệt hoa thường) bị **bỏ qua lặng lẽ** — FE gửi lại đợt hỏng không nhân đôi file | `SigningBatchFilePolicy.IsAcceptedFile`, `SigningBatchService.AddFiles` |
| Xoá file: lô đang `SIGNING` → `608`; file không thuộc lô → `610`; chỉ file `PENDING` / `FAILED` → `611`. Lô `NEW` **được** xoá (kssm cấm, không gỡ được file tải nhầm) | `SigningBatch.RemoveFile`, `SigningBatchFile.Delete` |
| Xoá file cuối **không** xoá lô (kssm xoá mềm luôn cả lô) | — |

## Lưu file nguồn

Key MinIO `digital-signature/signing-batch/{id}/source/{order:D6}.pdf` — theo số thứ tự, không theo tên gốc (tên gốc ở
`FileName`). Đẩy **song song 4 file** (`SigningBatchConst.UPLOAD_PARALLELISM`) **trước** `SaveChanges`; xoá file thì xoá
mềm dòng rồi xoá object sau khi lưu.

⚠️ Bẫy đã sập: lấy `Order = TotalFiles + 1` thì xoá một file xong thêm file mới **trùng số thứ tự và ghi đè object
nguồn** của file khác. `Order` mới = `Order` lớn nhất của lô **kể cả file đã xoá mềm**
(`SigningBatchRepository.GetLastFileOrderAsync`, `IgnoreQueryFilters`) — đừng quay lại đếm theo `TotalFiles`.

## Nhật ký

| Thao tác | `ActionType` | Dữ liệu |
|---|---|---|
| Tạo lô ký | Create | `TemplateId`, `SignedFolderName` |
| Thêm file vào lô ký | Create | `Id`, `AddedFiles`, `TotalFiles` |
| Xóa file khỏi lô ký | Delete | `Id`, `FileId`, `FileName` |

## Giới hạn đã biết

- ⚠️ Hai lượt thêm file **đồng thời** vào cùng lô có thể lấy trùng `Order` (không khoá); FE phải gửi tuần tự từng đợt.
- ⚠️ Đẩy MinIO xong mà `SaveChanges` lỗi thì object nằm lại; lượt sau ghi đè cùng key.
- ⚠️ `IFileStorage.UploadAsync` đặt `PublicRead` như ảnh mẫu, nhưng file nguồn / bản ký là tài liệu **không nên công khai**.
  Quyền đọc trên MinIO do bucket policy quyết định: chỉ mở đọc công khai cho tiền tố `digital-signature/template/`.
- Lô `NEW` bỏ dở không bị dọn; chưa có API xoá lô, danh sách lô, chi tiết lô.

## 🔬 Chưa thi công

Mở / đóng phiên ký (`ICertificateValidator`, sai → `614` / `615`), runner, vòng đưa thư, bắt đầu / tạm dừng / huỷ, tiến độ,
link bản đã ký, khôi phục sau restart — theo các bước 3–7 của [../plans/ky-so-local.plan.md](../plans/ky-so-local.plan.md).

> **Tiếp:** [11-ho-so-import.md](11-ho-so-import.md) — import hồ sơ từ Google Sheet và xem file.
