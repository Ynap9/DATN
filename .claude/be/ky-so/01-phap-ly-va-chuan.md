# Pháp lý và chuẩn kỹ thuật của chữ ký số

> **Phần 1/6** · mục lục: [README.md](README.md)

## Văn bản pháp luật

| Văn bản | Nội dung liên quan | Mức chắc chắn |
|---|---|---|
| Luật Giao dịch điện tử 20/2023/QH15 (hiệu lực 01/7/2024) | Chữ ký số có giá trị như chữ ký tay / con dấu khi bảo đảm an toàn; chữ ký số chuyên dùng công vụ | Chưa đọc nguyên văn điều khoản; các nguồn lệch nhau Điều 22 / 23 |
| NĐ 23/2025/NĐ-CP (hiệu lực 10/4/2025) | Chữ ký điện tử và dịch vụ tin cậy, thay NĐ 130/2018 | Đọc qua trang tổng hợp |
| **NĐ 68/2024/NĐ-CP** (hiệu lực 15/8/2024) | Chữ ký số chuyên dùng công vụ, do Ban Cơ yếu Chính phủ quản lý | Điều 1–15 đọc nguyên văn, Điều 29–32 chỉ có bản tóm tắt |
| TT 76/2025/TT-BQP (hiệu lực 02/9/2025) | Ban hành QCVN 16:2025/BQP về xác thực và lưu trữ tài liệu số lâu dài; tham chiếu ISO 14533-1 (CAdES dài hạn) và ISO 14533-3 (PAdES dài hạn) | Đọc phần đầu + danh mục tiêu chuẩn |
| **TT05/2025/TT-BNV** Điều 8, 41 | Hình thức chữ ký số trên tài liệu lưu trữ số hoá và bản sao | Đọc nguyên văn (`dong-goi/tai-lieu/TT05-2025-TT-BNV.txt`) |
| NĐ 30/2020/NĐ-CP Phụ lục I | Hình thức chữ ký số trên văn bản hành chính (văn thư) | Chưa đọc được nguyên văn |

Lưu ý: các trang thuvienphapluat / hethongphapluat chặn bot hoặc thu phí, còn vbpl.moj.gov.vn lỗi DNS khi tra.
Nếu đưa Điều 29–32 NĐ 68 vào báo cáo đồ án thì **phải chép từ Công báo**, không dùng lời tóm tắt ở dưới.

### NĐ 68/2024: những gì đọc được

- Điều 3 định nghĩa:
  - CRL là "danh sách chứng thư chữ ký số chuyên dùng công vụ bị thu hồi";
  - OCSP là "dịch vụ kiểm tra, cho phép xác định trạng thái hiện thời của chứng thư".
- Điều 6 liệt kê các dịch vụ, trong đó có kiểm tra chứng thư trực tuyến (khoản 8) và cấp dấu thời gian (khoản 9).
- Điều 8.3: khoá bí mật "được lưu giữ trên thiết bị lưu khóa bí mật". Đây là cơ sở để plugin **chỉ nhận khoá trên token**.
- Điều 9: chứng thư cấp mới có hạn tối đa 5 năm, gia hạn tối đa 3 năm. Chứng thư trong file mẫu có hạn 2024-01-08 → 2029-01-06.
- Điều 29 (tóm tắt): quy trình tạo chữ ký gồm 5 bước:
  1. kiểm hiệu lực chứng thư (Điều 31);
  2. kiểm đường dẫn chứng thực (Điều 32);
  3. ký bằng khoá bí mật;
  4. gắn dấu thời gian khi pháp luật yêu cầu;
  5. hiển thị thông tin chữ ký.
- Điều 30 (tóm tắt): chữ ký hợp lệ khi đủ 5 điều kiện, xem bảng ở [02-kiem-chung.md](02-kiem-chung.md).
- Điều 31 (tóm tắt): chứng thư hợp lệ khi thời điểm ký nằm trong thời hạn, dùng đúng phạm vi, và **trạng thái lúc ký
  còn hoạt động**, kiểm bằng CRL hoặc OCSP.
- Điều 32 (tóm tắt): chứng thư người ký, CA trung gian và Root đều phải còn hiệu lực lúc ký; Root phải đúng Root
  do tổ chức cung cấp dịch vụ công bố.

### TT05/2025: hình thức chữ ký trên tài liệu lưu trữ

- Điều 8.1.c (nguyên văn): thông tin hiển thị gồm
  - "tên cơ quan, tổ chức, cá nhân";
  - "thời gian ký (ngày, tháng, năm; giờ, phút, giây; múi giờ Việt Nam theo Tiêu chuẩn ISO 8601)";
  - trình bày bằng "phông chữ Times New Roman, chữ in thường, kiểu chữ đứng, cỡ chữ 10, màu đen";
  - "**không hiển thị hình ảnh dấu** của cơ quan, tổ chức".
- Điều 8.2.a: với PDF/A số hoá từ giấy, thông tin chữ ký số hiển thị "tại góc trên, bên phải, trang đầu".
  Mức PDF/A đã chốt là PDF/A-2b ([../dong-goi/08-pdfa.md](../dong-goi/08-pdfa.md)).
- Điều 41: bản sao có xác thực mang chữ ký số của cơ quan / lưu trữ lịch sử, thể hiện bằng chữ "BẢN SAO".
  Đây là việc của KHO-32.

## Chuẩn kỹ thuật

| Chuẩn | Điểm dùng tới |
|---|---|
| ETSI EN 319 142-1 V1.1.1 (PAdES baseline) | Xem các dòng dưới bảng |
| RFC 5652 (CMS), RFC 5035 (ESS signing-certificate-v2) | Cấu trúc khối chữ ký nằm trong `/Contents` |
| RFC 3161 (TSA) | Token dấu thời gian đóng lên SHA-256 của giá trị chữ ký (thuộc tính signature-time-stamp) |

Các điểm của ETSI EN 319 142-1:
- `/SubFilter ETSI.CAdES.detached`.
- `/ByteRange` "shall cover the entire file" trừ `/Contents`.
- `/M` "shall be present".
- Bảng 1: "signing-time attribute in CMS signature **shall not be present**" ở cả 4 mức.
- Bốn mức chữ ký:
  - **B-B**: chữ ký cơ bản.
  - **B-T**: thêm dấu thời gian chứng minh chữ ký có từ thời điểm đó.
  - **B-LT**: thêm DSS (mục 5.4) chứa chứng thư, CRL, OCSP để kiểm lại về sau mà không cần mạng.
  - **B-LTA**: thêm document time-stamp (`/SubFilter ETSI.RFC3161`) để "validate the signature long time after its
    generation", kể cả khi thuật toán yếu đi hoặc dữ liệu kiểm tra hết hạn.

Chữ ký kssm hiện ở **mức B-T**. Với kho lưu trữ lịch sử (QCVN 16:2025 tham chiếu ISO 14533-3, hồ sơ PAdES dài hạn),
mức hợp lý là **B-LT khi ký, B-LTA khi nhập kho và đóng dấu lại định kỳ** (KHO-18, KHO-23).

## Hạ tầng Ban Cơ yếu (đo ngày 28/11/2026)

| Thành phần | Giá trị | Ghi chú |
|---|---|---|
| Root | RootCA chuyên dùng Chính phủ G1 / G2 | Đã ghim SHA-256 trong `CertificateConst`, file ở `sh.be.api/Cert` |
| CA cấp | "CA phục vụ các cơ quan Nhà nước G2" | Policy chứng thư `2.16.704.1.1.1.1.1` |
| OCSP | `http://ocsp.ca.gov.vn` | Trả lời được, trạng thái "good", hiệu lực 3 giờ mỗi phản hồi |
| CRL | `http://ca.gov.vn/pki/pub/crl/cpcag2.crl` | **HTTP trả 404**; HTTPS chuyển hướng sang `dichvucong.ca.gov.vn`, 1,7 KB |
| TSA | `http://tsa.ca.gov.vn/` | Chỉ nhận POST RFC 3161; chứng thư "Máy chủ cấp dấu thời gian 02", policy `2.16.704.1.1.1.2` |

TSA chạy HTTP không phải lỗ hổng: token có chữ ký của TSA, request có nonce, nên sửa trên đường truyền là bị phát hiện.

> **Tiếp:** [02-kiem-chung.md](02-kiem-chung.md) giải thích cách chứng minh một file đã được ký số và kết quả đo thật.
