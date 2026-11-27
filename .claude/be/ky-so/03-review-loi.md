# Review lỗi: kssm, plugin, soHoaSaoMai, sh.be

> **Phần 3/6** · trước: [02-kiem-chung.md](02-kiem-chung.md) · mục lục: [README.md](README.md)

Cột "sh.be" cho biết bản đã chép vào `sh.be/.../DigitalSignature` có mang lỗi đó không.

## Mức 1: bảo mật (sửa trước khi chạy thật)

| # | Lỗi | Ở đâu | sh.be |
|---|---|---|---|
| B1 | **API kssm không có xác thực.** Không có `AddAuthentication` / `[Authorize]`, compose mở cổng `${HOST_PORT:-8200}:8080`. Ai biết địa chỉ đều tạo lô, mở phiên, kéo yêu cầu ký, xem danh sách file được | `kssm.be.api/Program.cs`, `kssm.deploy/docker-compose.yml` | Không: có `PermissionAttribute` |
| B2 | **Không kiểm người tạo lô.** Route `lo-ky/:id/*` chỉ có `auth('all','admin')`. Người đăng nhập bất kỳ có thể `cho-ky` (lấy hash cần ký), `chu-ky` (nộp chữ ký), `huy`, `file/:fileId` (xoá) trên lô của người khác | `sao_mai_be/src/routes/nhp/kySo.route.js` | Không: mã 616 `DigitalSignatureErrorSigningBatchNotOwned` |
| B3 | **Plugin ký mù cho bất kỳ ai gọi tới, khi phiên đang mở.** Ba nguyên nhân: không kiểm Host (`ListenLocalhost`, không đặt `AllowedHosts`), không có mã phiên, `/api/plugin/ky-so/ky` ký mọi chuỗi base64. Sau khi người dùng nhập PIN, phiên sống tới 15 phút rảnh. Trong thời gian đó, mã độc trên máy hoặc trang web dùng DNS rebinding gọi được `ky` để ký dữ liệu tuỳ ý | `ks.plugin.api/Program.cs`, `KySoController`, `SigningSession` | Thuộc repo plugin |

Cách sửa B3, làm ở repo plugin:
- `AllowedHosts=localhost;127.0.0.1`.
- `mo-phien` trả về một mã ngẫu nhiên; `ky` và `dong-phien` bắt buộc gửi kèm mã này trong header.
- Chỉ ký dữ liệu là SET OF signedAttributes có đúng `messageDigest` 32 byte. Chặn được việc dùng token ký challenge
  đăng nhập hay giao dịch khác.
- Khay hệ thống hiện số lượt đã ký trong phiên.

## Mức 2: sai chuẩn / pháp lý

| # | Lỗi | Hậu quả | sh.be |
|---|---|---|---|
| P1 | **Không kiểm thu hồi lúc ký.** `RevocationMode = NoCheck` khi mở phiên (`CertificateTrustValidator`) | Chứng thư đã bị thu hồi vẫn ký được. Trái Điều 29 / 31 NĐ 68 (kiểm hiệu lực, gồm trạng thái, trước khi ký) | Có (`CertificateValidatorExternal`) |
| P2 | **Chỉ đạt B-T, không có DSS** | Sau khi chứng thư hết hạn không chứng minh được trạng thái lúc ký. pyHanko hard-fail đã báo đúng việc này ([02](02-kiem-chung.md)) | Có |
| P3 | **Có thuộc tính signing-time** trong signedAttributes | ETSI EN 319 142-1 Bảng 1 ghi "shall not be present". Adobe vẫn nhận, nhưng không đạt PAdES baseline. Giờ ký đã có ở `/M` và TSA | Có (`CmsBuilder.BuildSignedAttributes`) |
| P4 | **Hình thức hiển thị lệch TT05 Điều 8.1.c** với tài liệu lưu trữ số hoá. Template cho chọn màu / font / cỡ, và luồng dự án **vẽ ảnh dấu đỏ lên bản quét** (chú thích ở `PdfPreparer.PlanAnnotations`) | TT05 bắt buộc: chữ đen, Times New Roman 10, chữ thường đứng, không hiện hình dấu, góc trên bên phải trang đầu | Có (cùng `PdfPreparer`) |

## Mức 3: lỗi kỹ thuật

| # | Lỗi | Hậu quả | sh.be |
|---|---|---|---|
| K1 | Plugin ký được ECDSA, nhưng máy chủ ghi cứng OID `rsaEncryption`. Thêm nữa, `ECDsa.SignData` của .NET trả dạng P1363 chứ không phải DER | Token ECDSA ra file hỏng. Token Ban Cơ yếu hiện là RSA nên chưa gặp | Có |
| K2 | Chuỗi nhúng vào CMS dựng bằng kho chứng thư **của máy chủ** (`BuildChain` / `LayChuoiChungThu` không thêm CA ghim) | Container Linux không có CA Ban Cơ yếu nên chỉ nhúng chứng thư lá; Adobe phải tự tìm CA. File mẫu có đủ 3 cert vì ký trên máy có CA | Có (`PdfSignerExternal.BuildChain`) |
| K3 | Tự kiểm sau khi ký chỉ gồm `CheckSignature(verifySignatureOnly: true)` và `TryDecode` token. Không đối chiếu imprint với SHA-256(chữ ký), không kiểm chữ ký / chuỗi của TSA, không kiểm chuỗi người ký | Cờ `ChuKyHopLe` chỉ nghĩa là "nguyên vẹn". Lúc ký vẫn an toàn vì `ProcessResponse` đã khớp hash + nonce, nhưng **không được dùng lại hàm này** để kiểm file từ ngoài vào (KHO-14) | Có (`EnsureValid`) |
| K4 | URL CRL trong chứng thư (`http://ca.gov.vn/...cpcag2.crl`) trả 404 | Bộ kiểm chỉ dùng CRL sẽ báo "không kiểm được". Phải dùng OCSP trước; khi lấy CRL thì đi HTTPS và theo chuyển hướng | Áp dụng khi làm P1 / P2 |
| K5 | Hàng đợi ký và phiên nằm trong RAM (`HangDoiKy`, `SigningQueueExternal`) | Chỉ chạy được 1 máy; khởi động lại API là mất phiên, lô chuyển tạm dừng | Có, đã ghi ở `nghiep-vu/10-lo-ky.md` |
| K6 | `PlanAnnotations` dùng `Math.Clamp` cho số trang | Mẫu đặt ở trang 5 mà file chỉ có 2 trang thì chữ ký **lặng lẽ** rơi sang trang 2, không báo | Kiểm lại khi làm KS-04 |

## Phần đã làm đúng (giữ nguyên khi chép)

- Ký nối bản (incremental update): giữ chữ ký cũ, `SigFlags 3`, tên field không trùng.
- ByteRange vá sau với độ rộng cố định, chừa 32 KB cho `/Contents`.
- Chuỗi signedAttributes đem ký là **cùng chuỗi byte** được nhét vào SignerInfo, chỉ đổi tag thành `[0]`.
- `signing-certificate-v2`; RSA có tham số NULL, SHA-256 không có.
- TSA có nonce, thử lại 3 lần, giới hạn số lời gọi song song.
- Root ghim bằng SHA-256; máy chủ **tự** dựng chuỗi, không tin cờ do máy người dùng gửi lên.
- Plugin từ chối khoá phần mềm, ký thử để bật hộp PIN sớm, đóng phiên khi rút token hoặc rảnh quá lâu, không ghi dữ liệu ký vào log.
- PIN và khoá bí mật không rời máy người dùng (đúng Điều 8.3 NĐ 68).

> **Tiếp:** [04-dua-ve-sh.md](04-dua-ve-sh.md) xếp các lỗi trên vào lộ trình KS-01…KS-16.
