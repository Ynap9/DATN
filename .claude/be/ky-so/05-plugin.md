# Plugin ký số: luồng ký tại máy người ký và vì sao khoá không lộ

> **Phần 5/6** · trước: [04-dua-ve-sh.md](04-dua-ve-sh.md) · mục lục: [README.md](README.md)
>
> Review repo `workspace/Ký số Plugin` ngày 28/11/2026, gồm bản Windows `Window/ks.plugin` (.NET 9, đang chạy thật)
> và bản macOS `MacOS/ks-plugin` (Rust, mới có lõi đọc thẻ). Tài liệu riêng của repo đó: `Window/docs/*`.

## Luồng ký: phép ký diễn ra trên máy người ký

| Bước | Ở đâu | Thứ đi qua mạng |
|---|---|---|
| 1. Chọn chứng thư, mở phiên, nhập PIN | Máy người ký (plugin + middleware token) | Không có gì. PIN không rời máy |
| 2. Gửi chứng thư **phần công khai** lên máy chủ | Trình duyệt → máy chủ | Chứng thư công khai (ai cũng xem được) |
| 3. Dựng PDF, tính SHA-256 hai đoạn ByteRange, dựng signedAttributes | Máy chủ | Không |
| 4. Trang web lấy signedAttributes đem xuống plugin | Máy chủ → trình duyệt → `127.0.0.1:17739` | Khoảng 200 byte, chứa giá trị băm, không chứa nội dung file |
| 5. **Chip trong token ký** | Máy người ký, bên trong token | Không |
| 6. Trả chữ ký thô lên máy chủ | Plugin → trình duyệt → máy chủ | 256 byte (RSA-2048) |
| 7. Gọi TSA, ghép CMS, ghi vào PDF | Máy chủ | Giá trị băm gửi tới TSA |

Máy chủ **không bao giờ có khoá bí mật**, nên cũng không thể tự ký thay người dùng. Chữ ký thô ở bước 6 chỉ dùng
được cho đúng signedAttributes đó, không dùng lại cho file khác.

## Vì sao khoá bí mật không lộ ra ngoài: ba lớp

1. **Phần cứng.** Khoá được sinh và nằm trong chip token, mang thuộc tính không xuất được. Chip chỉ nhận một loại
   lệnh: "ký giá trị băm này", rồi trả về chữ ký.
   - Bản Windows đi qua CNG / minidriver của bit4id.
   - Bản macOS gửi thẳng APDU `MANAGE SECURITY ENVIRONMENT` + `PSO: COMPUTE DIGITAL SIGNATURE`
     (`card/driver.rs::sign_sha256`): máy chỉ gửi băm, chip trả chữ ký.
2. **Mã plugin.** Đã quét cả hai bản: **không có lời gọi xuất khoá nào** (`Export*`, `ExportParameters`, `Pfx`,
   `Exportable`). Plugin chỉ cầm *handle* (`GetRSAPrivateKey`) để ra lệnh `SignData`. Handle không chứa khoá.
3. **PIN.**
   - Windows: hộp PIN do middleware của hãng hiện, PIN đi thẳng từ bàn phím vào provider, tiến trình plugin không
     thấy byte nào.
   - macOS: plugin phải tự gửi `VERIFY` nên PIN có đi qua tiến trình. PIN được bọc `Zeroizing` (xoá khỏi RAM sau
     khi dùng). Plugin cũng tự từ chối thử PIN khi thẻ còn dưới 2 lần thử, để người dùng không tự khoá thẻ.

Kết luận: **khoá không thể bị lấy ra**, kể cả khi máy chủ hoặc máy người ký bị chiếm. Rủi ro thật nằm ở chỗ khác:
khoá có thể **bị mượn để ký** trong lúc phiên đang mở. Nghĩa là có người nhờ chip ký một giá trị băm mà người dùng
không biết. Tài liệu `Window/docs/bao-mat-agent-ky-so.md` của bạn đã gọi đúng tên rủi ro này ("signing oracle",
T1–T3). Bảng dưới bổ sung những gì tài liệu đó chưa có.

## Review bản Windows

| # | Vấn đề | Chỗ | Đề xuất |
|---|---|---|---|
| W1 | **DNS rebinding chưa nằm trong mô hình mối đe doạ.** Không kiểm header Host. Trang độc trỏ tên miền của nó về 127.0.0.1 thì thành cùng origin, CORS không chặn. Chrome 142+ có hộp xin quyền `loopback-network` nên chặn được phần lớn; Firefox / Edge cũ chưa kiểm | `ks.plugin.api/Program.cs` | Thêm `AllowedHosts = localhost;127.0.0.1` (HostFiltering). Chỉ 1 dòng cấu hình |
| W2 | **`ky-so/ky` ký mọi chuỗi base64**, không có mã phiên | `KySoController`, `KySoService` | Bản nhẹ trước khi làm job ticket: `mo-phien` trả mã ngẫu nhiên, `ky` bắt buộc gửi kèm; chỉ ký dữ liệu là SET OF có `messageDigest` 32 byte; khay hiện số lượt đã ký |
| W3 | Ký được ECDSA, nhưng `ECDsa.SignData` trả dạng P1363, CMS cần DER; máy chủ lại ghi cứng RSA | `SigningSession.SignWithHandle` | Token Ban Cơ yếu là RSA. Hoặc chặn ECDSA ở `mo-phien`, hoặc đổi sang `DSASignatureFormat.Rfc3279DerSequence` và để máy chủ đọc đúng thuật toán |
| W4 | Bộ cài không ký Authenticode; bộ cài bit4id / TokenManager nhúng kèm, chạy bằng `runas`, không kiểm hash hay chữ ký | `dong-goi.ps1`, `bo-cai.nsi` | Trong `dong-goi.ps1`, kiểm `Get-AuthenticodeSignature` của file hãng trước khi đóng gói. Ký mã (OV) trước khi phát hành rộng (tài liệu §8 đã nêu) |
| W5 | Cài theo người dùng ở `%LOCALAPPDATA%`: mã độc cùng quyền có thể thay file exe | `bo-cai.nsi` | Chấp nhận, đã ghi ở §9.4. Không phải lỗi cần sửa |
| W6 | Trang cài cho chọn Development / Staging, ghi vào HKCU | `bo-cai.nsi` trang Environment | Ẩn trang này ở bản phát hành, chỉ bật bằng tham số dòng lệnh |
| W7 | Chrome 142+ hỏi quyền khi trang https gọi 127.0.0.1. Người dùng bấm Chặn thì FE sẽ báo "chưa cài plugin" | FE màn Cài plugin | Phân biệt "không gọi được" với "bị chặn quyền", có hướng dẫn mở lại quyền. Thử trên Chrome bản hiện tại |

Phần làm tốt, giữ khi sửa:
- chỉ nghe loopback;
- không ghi log nội dung đem ký, không dùng Event Log;
- ký thử để bật hộp PIN ngay lúc mở phiên;
- giữ handle một phiên (không cache PIN);
- tự đóng phiên khi rút token hoặc để rảnh 15 phút;
- từ chối khoá phần mềm;
- chỉ chạy một bản;
- cài theo người dùng, không cần quyền quản trị.

## Review bản macOS (tạm hoãn)

Người dùng chốt 28/11/2026: bản macOS khó phát triển nên **để sau**. Đồ án chỉ hỗ trợ Windows. Bảng dưới giữ lại để
khi quay lại không phải dò từ đầu.

| # | Vấn đề | Ghi chú |
|---|---|---|
| M1 | README `MacOS/README.md` vẫn ghi "chưa có mã nguồn" | Thực tế đã có lõi thẻ: `apdu`, `pkcs15`, `driver`, `tlv`, `x509` và công cụ `ks-probe` (`readers`, `pin-status`, `sign-test`). `ks-plugin-api`, `ks-plugin-applications`, `app` còn rỗng, tức **chưa có máy chủ HTTP**. Cần sửa README |
| M2 | PIN đi qua tiến trình plugin, trái nguyên tắc §4 của bản Windows | Đúng như README macOS đã cảnh báo. Đã giảm rủi ro bằng `Zeroizing` và chặn khi còn dưới 2 lần thử. Còn phải chốt: hộp PIN native, không ghi log, không giữ PIN qua nhiều lô |
| M3 | APDU qua PC/SC không mã hoá kênh USB | Đặc tính của loại token này, không phải lỗi; ghi vào phần rủi ro tồn dư |

## Cần thử khi bạn cắm USB token (gộp vào KS-07)

1. Đo `T`, thời gian một lượt ký: lấy thời gian một đợt `ky-so/ky` chia cho số yêu cầu. Tài liệu ghi `T` chưa đo được.
2. Rút token giữa lô → phiên đóng, file dở quay về trạng thái chờ; cắm lại, mở phiên mới thì ký tiếp.
3. Nhập sai PIN 1 lần → xem số lần thử còn lại; **không thử tới khi khoá thẻ**.
4. Kiểm cấu hình middleware bit4id có tự nhớ PIN không (§4a). Nếu có thì đóng phiên trong plugin chưa đủ.
5. Sau khi sửa W1, gọi `curl -H "Host: evil.test" http://127.0.0.1:17739/api/plugin/trang-thai` → phải bị từ chối.

> **Tiếp:** [06-de-xuat-mo-hinh.md](06-de-xuat-mo-hinh.md) đề xuất cải tiến để hoàn thiện mô hình.
