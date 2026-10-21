# Đóng gói tài liệu lưu trữ số — quy định TT05/2025

> 🔬 **Chưa thi công ở sh.be.** Bộ này chỉ chép **quy định** của Thông tư 05/2025/TT-BNV về gói SIP / AIP / DIP
> (cấu trúc gói, METS, EAD, PREMIS, bảng mã, PDF/A). Nghiệp vụ, cấu hình và thiết kế module đóng gói của sh.be
> **chốt riêng ở ĐATN**, không bê theo sao_mai / kssm.

Nguồn: bê từ `C:\Users\Admin\workspace\ksts\.claude\docs\tt05-*.md` ngày 2026-10-21 (bản tổng hợp từ PDF chính thức
364 trang trên `moha.gov.vn`, cập nhật 2026-09-08). Nội dung quy định giữ nguyên; chỉ tách file cho vừa 180 dòng,
sửa link, và đổi các ghi chú gắn với kssm / KSTS sang ngữ cảnh sh.be.

> 🎯 **Luật trọng tài:** khi thông tư và gói mẫu `SIP_hoso` lệch nhau thì **theo gói mẫu** — trừ ba lỗi của chính gói
> mẫu ở [04-sip-goi-mau.md](04-sip-goi-mau.md).

## Mục lục

| File | Nội dung |
|---|---|
| [01-thong-tu.md](01-thong-tu.md) | Nhận dạng văn bản, 3 loại gói, bảng chọn gói (Điều 17), thể thức số hóa (Điều 8), các chương, bản cho người dùng, bản đồ phụ lục, đối chiếu nhanh SIP / AIP / DIP |
| [02-sip-cau-truc-mets.md](02-sip-cau-truc-mets.md) | `SIP_hoso` vs `SIP_tailieu`, cây thư mục gói, METS (`mets`, `metsHdr`, `dmdSec`, `fileSec`, `structMap`) |
| [03-sip-metadata.md](03-sip-metadata.md) | `EAD.xml` cấp gói (18 / 5 trường), `EAD_doc` / `EAD_pic` / `EAD_media`, tài liệu liên kết, bảng mã, mimetype, tên ZIP |
| [04-sip-goi-mau.md](04-sip-goi-mau.md) | Gói mẫu chuẩn, 15 điểm chốt theo gói mẫu, 3 lỗi của gói mẫu |
| [05-aip.md](05-aip.md) | Khi nào ra AIP, cấu trúc (thêm PREMIS), METS khác SIP, `EAD.xml` (18 / 4 trường), metadata tài liệu của AIP |
| [06-aip-premis.md](06-aip-premis.md) | PREMIS v3.0 (`object`, `event`, `agent`), tài liệu liên kết của `AIP_hoso`, điều AIP không quy định |
| [07-dip.md](07-dip.md) | DIP (Điều 42), tệp văn bản xác thực 11 thông tin, cấu trúc, METS, `EAD.xml` yêu cầu khai thác, metadata tài liệu |
| [08-pdfa.md](08-pdfa.md) | PDF/A hai lớp: là gì, thông tư đòi tới đâu, vì sao tuỳ chọn, số đo gói mẫu (PDF/A-2b), bẫy ký số |

## Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|
| SIP / AIP / DIP | Gói hồ sơ, tài liệu **nộp** / **lưu trữ** (bảo quản) / tài liệu lưu trữ **sử dụng** |
| `_hoso` / `_tailieu` | Gói mô tả một **hồ sơ** / một **lô tài liệu rời lẻ** (Điều 17) |
| METS | XML mô tả cấu trúc gói: file nào, ở đâu, băm gì, nối metadata ↔ tài liệu ra sao |
| EAD (`simpledc`) | XML metadata mô tả: `EAD.xml` cấp gói, `EAD_doc` / `EAD_pic` / `EAD_media` từng tài liệu |
| PREMIS | XML siêu dữ liệu bảo quản — bắt buộc ở AIP, tuỳ chọn ở DIP, không có ở SIP |
| `rep1` | Bản đại diện duy nhất của gói: `representations/rep1/{METS.xml, metadata, data}` |
| `OBJID` | Định danh gói, cũng là tên thư mục gói (`uuid-…` ở SIP, `urn:…` ở AIP) |
| Holey file | Tài liệu liên kết `.fetch.txt` — trỏ tới tài liệu đã nộp / đã lưu ở gói khác |

> **Tiếp:** [01-thong-tu.md](01-thong-tu.md) — thông tư, ba loại gói và cách chọn gói.
