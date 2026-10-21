# Lô ký (DigitalSignature)

> **Phần 10/12** · trước: [09-mau-chu-ky.md](09-mau-chu-ky.md) · mục lục: [README.md](README.md)

Lô ký (`digitalsignature.SigningBatch`) gom các file PDF người dùng tải lên để ký số hàng loạt bằng USB token qua plugin
theo một mẫu chữ ký. Mỗi file là một dòng `digitalsignature.SigningBatchFile`. Kiến trúc ký và thứ tự thi công:
[../plans/ky-so-local.plan.md](../plans/ky-so-local.plan.md). Cập nhật 2026-10-17: đã thi công bước 2–7.

## Các chức năng

Mọi route có gốc `api/digital-signature/signing-batch`; quyền viết tắt `...SigningBatch.X` = `Function.DigitalSignature.SigningBatch.X`.

| Nghiệp vụ | Endpoint | Quyền |
|---|---|---|
| Lô đang dở của chính mình (`SIGNING` / `PAUSED` mới nhất, không có → `data = null`) | `GET running` | `.View` |
| Danh sách **toàn bộ** file của lô, theo `Order` | `GET {id}/files` | `.View` |
| Tạo lô (`templateId`, `signedFolderName`) / thêm file (multipart `files`) | `POST` / `POST {id}/files` | `.Create` |
| Xoá một file | `DELETE {id}/files/{fileId}` | `.Delete` |
| Mở phiên (`certificateBase64` do plugin đọc từ token) / đóng phiên | `POST {id}/session` / `DELETE {id}/session` | `.Sign` |
| Bắt đầu hoặc ký tiếp / tạm dừng / huỷ, trả lô | `POST {id}/start` / `pause` / `cancel` | `.Sign` |
| Lấy đợt yêu cầu ký (long-poll 25 s, tối đa 8, hết giờ → `[]`) | `GET {id}/requests` | `.Sign` |
| Nộp chữ ký (`[{requestId, signatureBase64, error, isSessionLost}]`), trả luôn đợt kế | `POST {id}/signatures` | `.Sign` |
| Tiến độ `{id, status, totalFiles, signedFiles, failedFiles, stopReason, isRunning}` | `GET {id}/progress` | `.View` |
| Link tải bản ký (ký sẵn, sống 15 phút) | `GET {id}/files/{fileId}/signed-url` | `.View` |

Lô trả về (`FindSigningBatchByIdDto`): `id`, `templateId`, `signedFolderName`, `certificateThumbprint`, `status`
(`SigningBatchConst`: `0` NEW · `1` SIGNING · `2` DONE · `3` CANCELLED · `4` FAILED · `5` PAUSED), `totalFiles`,
`signedFiles`, `failedFiles`, `stopReason`, `startedAt`, `finishedAt`, `createdDate`.

## Vòng ký phía FE

1. `POST session` → `POST start`.
2. Lặp: `GET requests` → plugin ký từng `dataBase64` (RSA-SHA256 trên token) → `POST signatures` nhận luôn đợt kế; mảng
   rỗng là hết giờ chờ, gọi lại. Lỗi `619` là phiên đã đóng: dừng vòng.
3. Song song hỏi `GET progress` mỗi 2 s; lô hết `SIGNING` thì dừng.

`dataBase64` là `SignedAttributes` DER (vài trăm byte), không phải file PDF. Plugin báo mất phiên (rút token, hết 15 phút
rảnh) bằng `isSessionLost = true` → server đóng phiên, lô tự `PAUSED`.

## Luật

| Luật | Nằm ở |
|---|---|
| **Chỉ người tạo lô** xem / sửa được lô, người khác → `616` (kssm không kiểm) | `SigningBatch.EnsureOwnedBy` |
| Mẫu chữ ký phải còn → `601`; tên đợt ký bỏ `/`, `\`, ký tự điều khiển rồi trim, rỗng → `605` | `SigningBatchService.Create`, `NameSignedFolder` |
| Chỉ thêm file khi lô `NEW` → `607`; lô dự án → `606`; tối đa **200 file / lượt** → `617`, thân request ≤ 512 MB | `CreateFile`, `EnsureAcceptUpload`, `SigningBatchFilePolicy` |
| File rỗng, không đuôi `.pdf`, **trùng tên trong lô** (không phân biệt hoa thường) bị **bỏ qua lặng lẽ** | `SigningBatchFilePolicy.IsAcceptedFile` |
| Xoá file: lô `SIGNING` → `608`; không thuộc lô → `610`; chỉ file `PENDING` / `FAILED` → `611`. Xoá file cuối **không** xoá lô | `RemoveFile`, `SigningBatchFile.Delete` |
| Mở phiên: chứng thư hỏng → `614`, không chuỗi về CA Ban Cơ yếu đã ghim hoặc hết hạn → `615`; lô phải `NEW` / `PAUSED` / `FAILED` → `618`; lưu thumbprint | `ICertificateValidator.LoadTrusted`, `ChangeCertificate` |
| Bắt đầu: đúng trạng thái trên → `618`; chưa chọn chứng thư → `609`; không có phiên → `619`; phiên khác chứng thư đã lưu → `620`; không còn file chờ → `621`; runner còn đang chạy lô → `608`. File `FAILED` được đưa lại `PENDING` | `SigningBatch.Start`, `ISigningRunner.EnsureNotRunning` |
| Tạm dừng chỉ khi `SIGNING` → `622`; huỷ khi chưa `DONE` / `CANCELLED` → `623` (huỷ đóng luôn phiên) | `Pause`, `Cancel` |
| Hết file: còn file lỗi → `FAILED` kèm lý do (ký tiếp để ký lại), không thì `DONE`; đóng phiên | `SigningBatch.Complete` |
| File đã có chữ ký mà mẫu tắt "cho phép ký đè" → `628` | `SigningBatchFilePolicy.EnsureSignable` |
| Link bản ký chỉ cho file `DONE` → `624` | `SigningBatchFile.GetSignedObjectKey` |

## Runner — một file đi qua 3 chặng

`SigningRunner` (singleton) chạy mỗi lô bằng các task `async` nối qua `Channel` có trần; trần dùng chung **toàn server**
(`SigningBatchConst`): 3 lô chạy cùng lúc (lô sau chờ), chuẩn bị = số CPU, TSA = 4 kết nối, hoàn tất = 4.

| Chặng | Việc | Lỗi của file |
|---|---|---|
| Nhận việc | Một `UPDATE` chuyển 8 file `PENDING` → `SIGNING` | — |
| Chuẩn bị (4 worker / lô) | Tải nguồn MinIO, kiểm đã ký, `IPdfSigner.Prepare` (ảnh dấu / chữ ký tay đã vẽ cache theo lô) | PDF hỏng `625`, mã hoá `626`, `/DocMDP /P 1` `627`, `628`, ảnh mẫu `633`, vẽ chữ `635` |
| Ký (8 yêu cầu đang bay) | `ISigningQueue.RequestSignatureAsync`, chờ plugin tối đa 120 s | Plugin báo lỗi `632`, quá giờ `634`; mất phiên → lô `PAUSED` |
| Hoàn tất (4 worker / lô) | TSA (3 lần thử) → ghép CMS → tự kiểm → đẩy bản ký riêng tư → ghi DB → xoá nguồn | TSA `630`, chữ ký sai / không phủ trọn file `631`, chữ ký quá chỗ trống `629` |

- **Cầu dao TSA**: 5 file lỗi TSA **liên tiếp** → lô `PAUSED` lý do TSA; file đó đã `FAILED`, ký tiếp sẽ ký lại.
- Kết quả và bộ đếm ghi bằng `ExecuteUpdate`; DB ghi trước rồi mới cộng tiến độ trong RAM. `progress` đọc RAM khi lô chạy.
- Tạm dừng / huỷ / mất phiên: file `SIGNING` trả về `PENDING`. Lỗi hệ thống (DB, MinIO sập) → lô `FAILED` lý do chung.
- **Khởi động server**: lô `SIGNING` → `PAUSED` ("máy chủ khởi động lại"), file `SIGNING` → `PENDING` (`RecoverAsync`).

## Lưu file

| Object MinIO | Khi nào |
|---|---|
| `digital-signature/signing-batch/{id}/source/{order:D6}.pdf` | Tải lên (riêng tư, đẩy song song 4 trước `SaveChanges`); **xoá ngay khi file `DONE`** |
| `digital-signature/signing-batch/{id}/signed/{order:D6}.pdf` | Bản ký, riêng tư; tải qua link ký sẵn có `Content-Disposition` mang tên gốc |

⚠️ Bẫy đã sập: lấy `Order = TotalFiles + 1` thì xoá một file xong thêm file mới **trùng số thứ tự và ghi đè object
nguồn** của file khác. `Order` mới = `Order` lớn nhất của lô **kể cả file đã xoá mềm**
(`SigningBatchRepository.GetLastFileOrderAsync`, `IgnoreQueryFilters`) — đừng quay lại đếm theo `TotalFiles`.

## Nhật ký

| Thao tác | `ActionType` | Dữ liệu |
|---|---|---|
| Tạo lô ký / thêm file / xóa file khỏi lô | Create / Create / Delete | `TemplateId`, `SignedFolderName` / `Id`, `AddedFiles`, `TotalFiles` / `Id`, `FileId`, `FileName` |
| Mở phiên ký | Update | thumbprint cũ → mới, `Subject` |
| Bắt đầu ký lô / tạm dừng / hủy | Update | trạng thái, số file đưa lại hàng chờ, bộ đếm |

Runner **không** ghi nhật ký từng file (lô 1000 file sẽ là 1000 dòng); kết quả nằm ở chính `SigningBatchFile`.

## Giới hạn đã biết

- ⚠️ Hàng đợi phiên và runner nằm trong RAM: chỉ chạy **một instance**; restart thì lô phải ký tiếp.
- ⚠️ Revocation đang `NoCheck`: chứng thư đã thu hồi vẫn mở được phiên. Chưa có WYSIWYS: plugin ký mọi yêu cầu nhận được.
- ⚠️ Hai lượt thêm file **đồng thời** vào cùng lô có thể lấy trùng `Order`; FE phải gửi tuần tự từng đợt.
- Huỷ lô chưa dọn bản nguồn còn lại; lô `NEW` bỏ dở không bị dọn; chưa có API xoá lô, danh sách lô.
- Luồng đầy đủ qua API với USB token thật **chưa chạy thử**; phần mật mã đã ký thật qua TSA và OpenSSL xác nhận
  (chương trình thử gọi thẳng `PdfSigner`, chứng thư tự ký).

> **Tiếp:** [11-ho-so-import.md](11-ho-so-import.md) — import hồ sơ từ Google Sheet và xem file.
