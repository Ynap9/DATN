# Nghiên cứu ký số: pháp lý, kiểm chứng, review kssm / soHoaSaoMai

> Viết 2026-11-28, trước khi đưa ký số vào sh. Tài liệu chỉ để review và nghiên cứu, chưa sửa dòng code nào.
> Nguồn gồm: mã kssm (`ksts/kssm.be`), plugin Windows (`Ký số Plugin/Window/ks.plugin`), soHoaSaoMai
> (`sao_mai_be` + `sao_mai_fe` mục ky-so), bản đã chép vào `sh.be` (`DigitalSignature/*`), văn bản pháp luật,
> chuẩn ETSI, và kết quả kiểm trên 2 file PDF đã ký thật.

## Kết luận ngắn

1. **Cơ chế ký là đúng và chứng minh được.** File do bộ mã này ký (2 file mẫu ngày 13/10/2026) qua được kiểm tra
   độc lập bằng pyHanko:
   - nội dung còn nguyên (ByteRange phủ trọn file, messageDigest khớp);
   - dựng được chuỗi chứng thư về **RootCA chuyên dùng Chính phủ G2** của Ban Cơ yếu;
   - dấu thời gian TSA của Ban Cơ yếu hợp lệ, đóng đúng lên giá trị chữ ký;
   - OCSP của Ban Cơ yếu trả trạng thái chứng thư "good".

   Xem [02-kiem-chung.md](02-kiem-chung.md).
2. **Có lỗi cần sửa trước khi dùng thật**, nặng nhất là bảo mật:
   - API kssm không xác thực;
   - soHoaSaoMai không kiểm người tạo lô;
   - plugin ký mù cho bất kỳ ai gọi tới 127.0.0.1 khi đang có phiên.
   Xem [03-review-loi.md](03-review-loi.md).
3. **Chưa đạt yêu cầu lưu trữ dài hạn.** Chữ ký mới ở mức PAdES B-T:
   - không kiểm thu hồi lúc ký;
   - không nhúng dữ liệu kiểm tra (DSS);
   - còn thuộc tính signing-time, trong khi ETSI cấm.

   Ký trong web thi công thì đủ dùng. Kho lưu trữ lịch sử thì cần B-LT / B-LTA. Xem
   [01-phap-ly-va-chuan.md](01-phap-ly-va-chuan.md).
4. **Ký tài liệu lưu trữ số hoá phải theo TT05 Điều 8.1.c:** chữ đen, Times New Roman 10, không hiện hình dấu,
   góc trên bên phải trang đầu. Luồng soHoaSaoMai đang vẽ dấu đỏ lên bản quét, sai với quy định này.
5. Bản trong `sh.be` mang theo gần đủ các lỗi kỹ thuật trên, trừ phần quyền (sh.be đã có `PermissionAttribute`
   và mã 616 chặn người không phải chủ lô). Cách xử lý khi đưa về sh: [04-dua-ve-sh.md](04-dua-ve-sh.md).

## Mục lục

| Phần | Nội dung |
|---|---|
| [01-phap-ly-va-chuan.md](01-phap-ly-va-chuan.md) | Luật, nghị định, thông tư, QCVN, chuẩn PAdES / CMS / TSA, hạ tầng Ban Cơ yếu |
| [02-kiem-chung.md](02-kiem-chung.md) | Thế nào là "đã ký số" chứng minh được, kết quả kiểm 2 file thật, kịch bản thử với USB token |
| [03-review-loi.md](03-review-loi.md) | Lỗi và sai lầm trong kssm, plugin, soHoaSaoMai, sh.be, xếp theo mức nặng |
| [04-dua-ve-sh.md](04-dua-ve-sh.md) | Thứ tự sửa khi đưa ký số về sh, gắn với KS-01…KS-16 |
| [05-plugin.md](05-plugin.md) | Plugin ký số (Windows + macOS): luồng ký tại máy người ký, 3 lớp giữ khoá không lộ, review |
| [06-de-xuat-mo-hinh.md](06-de-xuat-mo-hinh.md) | Đề xuất cải tiến, mô hình đích (chờ duyệt) trước khi lên plan |

Công cụ kiểm: `Downloads\Cường\ky-so\kiem-chu-ky.py`. Script dùng pyHanko và CA ghim trong `sh.be/sh.be.api/Cert`.
Cách chạy ở [02-kiem-chung.md](02-kiem-chung.md).
