# Đưa ký số về sh: thứ tự xử lý

> **Phần 4/6** · trước: [03-review-loi.md](03-review-loi.md) · mục lục: [README.md](README.md)

Trạng thái: **nghiên cứu**, chưa sửa code. Danh sách task gốc ở `Downloads\Cường\M3_KySo_KhoLuuTru_DanhSachTask.txt`.

## Gắn lỗi vào task

| Khi nào | Việc | Lỗi xử lý |
|---|---|---|
| Trước KS-06 (vòng ký) | Sửa plugin: AllowedHosts, mã phiên, lọc dữ liệu ký | B3 |
| Trong KS-07 (chạy thử token thật) | Bỏ signing-time; dựng chuỗi bằng CA ghim; kiểm OCSP lúc mở phiên, fail-closed; chặn ECDSA hoặc hỗ trợ đúng | P3, K2, P1, K1 |
| KS-07, bước đo | Kiểm file ra bằng Adobe, pyHanko, openssl theo [02-kiem-chung.md](02-kiem-chung.md); thử phá 3 kiểu | Bằng chứng cho báo cáo |
| KS-04 (cấu hình mẫu) | Thêm kiểu mẫu "Tài liệu lưu trữ TT05": khoá màu đen, Times New Roman 10, góc trên bên phải trang đầu, tắt ảnh dấu đỏ; báo lỗi khi trang của mẫu vượt số trang file | P4, K6 |
| KS-12 / KS-13 (ký theo dự án) | Dự án số hoá dùng mặc định mẫu TT05 | P4 |
| Sau KS-07, trước KHO-18 | Nâng lên B-LT: lưu phản hồi OCSP lấy lúc ký cùng chuỗi CA, thêm 1 bản nối có DSS | P2, K4 |
| KHO-14 / KHO-18 / KHO-23 | Hàm kiểm đầy đủ 5 điều kiện (không dùng lại `EnsureValid`); nhập kho thì đóng document time-stamp (B-LTA); kiểm tra định kỳ thì đóng dấu lại | K3, P2 |
| Không làm | Đưa hàng đợi ký ra Redis | K5: chấp nhận chạy 1 máy, đã ghi trong tài liệu 10 |

B1, B2 không cần làm vì sh.be đã có quyền và mã 616. Khi chép thêm mã từ kssm hoặc soHoaSaoMai, phải giữ hai lớp
này, đừng chép kiểu route chỉ kiểm đăng nhập.

## Điểm cần chú ý

- Bỏ signing-time làm đổi chuỗi byte đem ký, nhưng không đổi API hay schema. Làm cùng lúc với KS-07 để chỉ phải đo lại một lần.
- Kiểm OCSP lúc mở phiên thêm khoảng 1 lời gọi mạng mỗi lô, không phải mỗi file. Phản hồi OCSP có hạn 3 giờ, đủ cho
  một lô; lô chạy quá 3 giờ thì lấy lại.
- Nâng B-LT bằng một bản nối thứ hai **sau** chữ ký, không sửa bản ký. Bộ ghi xref tự viết (`PdfPreparer.WriteRevision`)
  đã nối bản được, chỉ cần thêm dictionary `/DSS` vào Catalog.
- Mẫu TT05 chỉ áp cho tài liệu lưu trữ số hoá. Lô ký văn bản thường (giấy báo, quyết định) vẫn cho ảnh chữ ký tươi
  và dấu đỏ, nhưng phải tách thành kiểu mẫu riêng để không lẫn.
- Các điều khoản NĐ 68 Điều 29–32 mới có bản tóm tắt. Trước khi viết chương pháp lý của báo cáo, cần chép nguyên
  văn từ Công báo.

> **Tiếp:** [05-plugin.md](05-plugin.md) giải thích luồng ký tại máy người ký và review plugin.
