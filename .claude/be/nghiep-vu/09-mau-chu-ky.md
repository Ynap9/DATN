# Mẫu chữ ký (DigitalSignature)

> **Phần 9/21** · trước: [08-cau-hinh-du-an.md](08-cau-hinh-du-an.md) · mục lục: [README.md](README.md)

Mẫu chữ ký (`digitalsignature.Template`) là bộ cấu hình dựng sẵn cho lô ký số: lý do / nơi ký, ảnh dấu đỏ, ảnh chữ ký tay và
toạ độ từng khối trên trang PDF (`digitalsignature.TemplatePosition`), để người ký không phải kéo thả lại mỗi lần ký.
Port từ module template của `kssm.be` (2026-10-17); khác kssm ở chỗ có đăng nhập, kiểm quyền và ghi nhật ký.

## Các chức năng

| Nghiệp vụ | Endpoint | Quyền yêu cầu |
|---|---|---|
| Danh sách (phân trang, `keyword` lọc theo tên, mới tạo trước) | `GET api/digital-signature/template/paging` | `Function.DigitalSignature.Template.View` |
| Xem chi tiết kèm toạ độ (sắp theo `Kind`) | `GET api/digital-signature/template/{id}` | `Function.DigitalSignature.Template.View` |
| Tải file PDF mẫu để thử đặt khối | `GET api/digital-signature/template/sample-file` | `Function.DigitalSignature.Template.View` |
| Tạo (chỉ tên + mô tả), trả chi tiết | `POST api/digital-signature/template` | `Function.DigitalSignature.Template.Create` |
| Đổi tên | `PUT api/digital-signature/template/{id}` | `Function.DigitalSignature.Template.Update` |
| Ghi cấu hình ký (multipart), trả chi tiết | `PUT api/digital-signature/template/{id}/config` | `Function.DigitalSignature.Template.Update` |
| Xoá | `DELETE api/digital-signature/template/{id}` | `Function.DigitalSignature.Template.Delete` |

`sample-file` trả thẳng bytes PDF khi thành công (như `excel-template`), lỗi vẫn là `ApiResponse`.

**Tạo trước, cấu hình sau:** object key của ảnh chứa `templateId`, nên phải có `Id` rồi mới lưu được ảnh. Màn hình gọi
`POST` lấy `id`, rồi `PUT {id}/config`.

## Ghi cấu hình — `PUT {id}/config`

Form fields: `signingReason`, `signingLocation`, `isShowDigitalSignature` (mặc định `true`), `isDigitalSignatureInImage`,
`allowSigningSignedPdf`, `stampIntensity`, `wetSignatureIntensity`, `wetSignatureStrokeWidth`, `digitalSignatureColor`,
`wetSignatureColor`, `digitalSignatureFont`, `digitalSignatureFontSize`, `stampImage`, `wetSignatureImage` (file),
`isRemoveStampImage`, `isRemoveWetSignatureImage`, `positions[i].kind|pageNumber|xRatio|yRatio|widthRatio|heightRatio`.

| Luật | Nằm ở |
|---|---|
| **Ghi đè toàn bộ**: field không gửi về mặc định, `positions` thay hết theo payload (toạ độ cũ **xoá cứng**) | `TemplateService.UpdateConfig`, `TemplateRepository.ReplacePositionsAsync` |
| Độ đậm kẹp `40–250`, độ dày nét `0–200`, cỡ chữ `6–24` — **kẹp**, không báo lỗi | `Template.UpdateConfig` |
| Màu chuẩn hoá `#RRGGBB` viết hoa (thiếu `#` tự thêm); sai khuôn → màu chữ ký số về `#000000`, màu mực chữ ký tay về `null` (giữ mực ảnh gốc) | `Template.UpdateConfig` |
| Font phải thuộc `TemplateConst.DIGITAL_SIGNATURE_FONTS` (không phân biệt hoa thường), khác → `Times New Roman` | `Template.UpdateConfig` |
| `kind` thuộc `0` chữ ký số · `1` dấu đỏ · `2` chữ ký tay, sai → `602`; khối nằm trọn trong trang, kích thước `> 0`, `pageNumber ≥ 1`, sai → `603` | `TemplatePosition.Create` |
| Ảnh `.png/.jpg/.jpeg`, không rỗng, ≤ 5 MB, sai → `612` (kiểm **cả hai** ảnh trước khi đẩy ảnh nào) | `TemplateImagePolicy.EnsureValidImage` |

Toạ độ là **tỉ lệ `0–1` gốc trên-trái**, độc lập khổ giấy. `allowSigningSignedPdf` = cho phép ký lên PDF đã có chữ ký số.

### Ảnh dấu đỏ / chữ ký tay

| Gửi gì | Kết quả |
|---|---|
| Có file | Đẩy lên MinIO tại `digital-signature/template/{id}/stamp.<đuôi>` (hoặc `wet-signature.<đuôi>`), lưu URL công khai + object key |
| Không gửi file, cờ xoá `false` | **Giữ nguyên ảnh cũ** |
| Không gửi file, cờ xoá `true` | Bỏ ảnh |

Cần cờ xoá riêng vì multipart không phân biệt "không gửi" với "gửi rỗng". Thứ tự: đẩy ảnh mới **trước** transaction DB,
commit xong mới xoá object cũ không còn dùng (đổi `.png` sang `.jpg` sinh key khác). Xoá hỏng chỉ ghi log.

## Xoá

Xoá mềm mẫu (bỏ luôn link ảnh) và toàn bộ toạ độ trong cùng `SaveChanges`, ghi nhật ký, rồi xoá mọi object dưới
`digital-signature/template/{id}/` trên MinIO.

## Nhật ký

| Thao tác | `ActionType` | Dữ liệu |
|---|---|---|
| Tạo mẫu chữ ký | Create | `Name`, `Description` |
| Cập nhật mẫu chữ ký (đổi tên) | Update | `Id`, `Name` cũ / mới |
| Cập nhật cấu hình mẫu chữ ký | Update | Toàn bộ cột cấu hình + object key ảnh, cũ / mới (không ghi toạ độ) |
| Xóa mẫu chữ ký | Delete | `Id`, `Name` |

## Giới hạn đã biết

- ⚠️ Không kiểm tên rỗng hay trùng tên (giữ như kssm); `Update` không đổi được `Description`.
- ⚠️ Đẩy ảnh xong mà commit DB lỗi thì object mới nằm lại trên MinIO; key cố định theo `templateId` nên lần lưu sau ghi đè.
- ⚠️ Mẫu đang được lô ký dùng vẫn xoá được (kssm cũng vậy); lô sẽ báo `601` lúc bắt đầu ký.
- 🔬 Xem trước file của dự án trên màn cấu hình (FE muốn chọn dự án để xem submission) **chưa làm**: ĐATN chưa có bảng tài liệu / submission.

> **Tiếp:** [10-lo-ky.md](10-lo-ky.md) — lô ký: tạo lô, tải / xoá file PDF, lô đang dở.
