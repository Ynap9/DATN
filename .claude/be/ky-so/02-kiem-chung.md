# Kiểm chứng: chứng minh một file đã được ký số

> **Phần 2/6** · trước: [01-phap-ly-va-chuan.md](01-phap-ly-va-chuan.md) · mục lục: [README.md](README.md)

## "Đã ký số" nghĩa là chứng minh được 5 điều

Bảng dưới lấy theo Điều 30–32 NĐ 68/2024 (bản tóm tắt, xem phần 1). Mỗi điều kiện cần một bằng chứng đọc được từ
chính file, không tin cờ nào do máy chủ tự ghi.

| Điều kiện | Bằng chứng trong file | Kiểm bằng gì |
|---|---|---|
| 1. Nội dung không bị sửa sau khi ký | SHA-256 của 2 đoạn `/ByteRange` bằng `messageDigest`; RSA mở được chữ ký trên signedAttributes; ByteRange phủ tới byte cuối | pyHanko `intact`, `valid`, `coverage=ENTIRE_FILE` |
| 2. Người ký đúng là chủ chứng thư | Chữ ký chỉ mở được bằng khoá công khai của chứng thư; `signing-certificate-v2` ghim đúng chứng thư đó | Như trên |
| 3. Thời điểm ký | Token TSA của Ban Cơ yếu: `genTime`, imprint = SHA-256(giá trị chữ ký) | Kiểm imprint và chữ ký của TSA |
| 4. Chứng thư còn hiệu lực lúc ký | Thời điểm TSA nằm trong NotBefore–NotAfter; OCSP / CRL **tại thời điểm ký** chưa thu hồi | OCSP `ocsp.ca.gov.vn`; về lâu dài phải có DSS |
| 5. Chuỗi chứng thực đúng | Lá → "CA phục vụ các cơ quan Nhà nước G2" → Root G2 đã ghim | Dựng chuỗi với Root ghim |

## Kết quả kiểm 2 file thật (28/11/2026)

Hai file là `1781486247715-864715_daky_20261013_170915.pdf` và `..._172026.pdf` trong `Downloads`. Cả hai được ký
bằng chứng thư cá nhân của một cán bộ HUCE do CA G2 cấp, dùng cùng bộ mã với kssm.

| Mục | Kết quả |
|---|---|
| SubFilter, `/M` | `ETSI.CAdES.detached`, `D:20261013170915+07'00'` |
| Số lần nối bản / DSS / DocTimeStamp | 2 `%%EOF` (gốc + bản ký) / không / không, tức mức B-T |
| ByteRange phủ trọn, messageDigest khớp | Có / có |
| Thuật toán | SHA-256 + RSA PKCS#1 v1.5 |
| Signed attributes | content-type, **signing-time**, message-digest, signing-certificate-v2 |
| Chứng thư nhúng trong CMS | 3 (Root G2, CA G2, lá), nên đủ để dựng chuỗi offline |
| TSA | genTime 2026-10-13 10:09:16 UTC; imprint = SHA-256(chữ ký): **đúng**; TSA tin được về Root G2 |
| pyHanko, không mạng, soft-fail | `intact=True valid=True trusted=True`, coverage ENTIRE_FILE, modification NONE, timestamp valid + trusted |
| OCSP hỏi trực tiếp (openssl) | `Cert Status: good`, This Update 27/11 17:29 GMT, Next Update +3 giờ |
| pyHanko có mạng, hard-fail | `trusted=False`: "OCSP response is too recent; Failure to fetch CRL" |

Dòng cuối là phát hiện quan trọng nhất. Bộ kiểm tra nghiêm ngặt muốn chứng cứ thu hồi **gần thời điểm ký** (10/2026).
OCSP hôm nay chỉ nói chứng thư *bây giờ* chưa bị thu hồi, còn CRL ở URL ghi trong chứng thư thì 404. File chỉ ở mức
B-T nên không mang sẵn chứng cứ đó. Hôm nay vẫn kết luận được là hợp lệ, vì chứng thư chưa từng bị thu hồi. Sau 2029,
khi chứng thư hết hạn và Ban Cơ yếu thôi trả OCSP cho nó, sẽ **không còn cách chứng minh** trạng thái lúc ký. Đây là
lý do kho cần B-LT / B-LTA.

## Kịch bản thử với USB token của bạn (khi bắt đầu KS-07)

1. Cài middleware token và plugin. Plugin chỉ nhận khoá trên token (`TokenVerifier.OnUsbToken`).
2. Ký 1 file PDF/A-2b mẫu (bản quét có lớp chữ) qua lô ký.
3. Kiểm bằng 3 công cụ độc lập:
   - **Adobe Acrobat Reader**: cần thêm Root G1 / G2 vào Trusted Certificates. Kỳ vọng "Signature is valid", có dấu
     thời gian, không có thay đổi sau ký.
   - **pyHanko** qua script `Downloads\Cường\ky-so\kiem-chu-ky.py`: đặt PDF vào thư mục `in/` cạnh script rồi chạy
     `python -I kiem-chu-ky.py`, hoặc `... online` để kiểm OCSP. Cần `pip install pyhanko pyhanko-certvalidator`.
   - **openssl ocsp** với chứng thư lá và CA G2 (xem lệnh dưới).
4. **Thử phá:**
   - sửa 1 byte trong nội dung → phải báo "modified" / không hợp lệ;
   - nối thêm 1 trang bằng công cụ khác → Adobe báo có thay đổi sau ký;
   - ký lại file đã ký → chữ ký cũ vẫn hợp lệ (nối bản).
5. Ghi lại vào báo cáo đồ án: ảnh Adobe, đầu ra pyHanko, phản hồi OCSP. Đó là "bằng chứng đã ký số" nộp được cho hội đồng.

```bash
# trạng thái thu hồi của chứng thư người ký (leaf.pem, issuer.pem tách từ CMS)
openssl ocsp -issuer issuer.pem -cert leaf.pem -url http://ocsp.ca.gov.vn -resp_text -noverify
```

Lưu ý: kiểm ở trên chưa chứng minh **người cầm token** là ai. Việc đó dựa vào quy trình cấp chứng thư của Ban Cơ yếu
và việc thuê bao giữ PIN. Hệ thống chỉ cần chứng minh khoá trong token đã ký đúng nội dung này vào đúng thời điểm này.

> **Tiếp:** [03-review-loi.md](03-review-loi.md) liệt kê lỗi và sai lầm tìm được trong mã nguồn.
