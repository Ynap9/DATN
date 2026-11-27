# Đề xuất cải tiến: mô hình ký số hoàn chỉnh

> **Phần 6/6** · trước: [05-plugin.md](05-plugin.md) · mục lục: [README.md](README.md)
>
> Trạng thái: **đã chốt phạm vi** (2026-11-28), chưa lên plan. Mô hình đích rút ra từ phần review 03 và 05. Được duyệt thì mới
> lên plan thi công. Mã cải tiến (`PL-`, `KY-`, `KT-`, `NV-`) dùng để dẫn chiếu trong task.

## Mục tiêu của mô hình

1. **Khoá chỉ ở chip, phép ký chỉ ở máy người ký.** Giữ nguyên cách đang làm.
2. **Plugin không bị mượn khoá:** chỉ ký cho đúng trang web, đúng phiên, đúng lô người dùng vừa đồng ý.
3. **File ra đạt PAdES đúng chuẩn và tự chứng minh được về lâu dài:** mức B-LT ở web thi công, B-LTA ở kho.
4. **Đúng quy định:**
   - NĐ 68/2024 Điều 29: kiểm hiệu lực và thu hồi trước khi ký;
   - Điều 30: đủ 5 điều kiện khi kiểm;
   - TT05 Điều 8: hình thức chữ ký trên tài liệu lưu trữ.
5. **Có hồ sơ bằng chứng cho từng file**, đủ để trình hội đồng hoặc dùng khi tranh chấp.

## Mô hình đích

```text
Máy người ký                         Máy chủ sh.be                               Bên ngoài
-----------------------------        ------------------------------------------  -----------------
Plugin (127.0.0.1:17739)             1. Mở phiên: kiểm chứng thư                 OCSP ca.gov.vn
 - chỉ nhận Host localhost             (hạn, KeyUsage, chuỗi về Root ghim,  ---> 
 - hộp đồng ý: trang, số file,          OCSP "good")  [KY-02]
   chứng thư   [PL-02]               2. Dựng PDF theo mẫu (TT05 / thường) [KY-05]
 - mã phiên + giới hạn lượt [PL-03]  3. signedAttributes KHÔNG có signing-time
 - chỉ ký signedAttributes hợp lệ,      [KY-01]
   đúng chứng thư phiên  [PL-04]  <- 4. (qua trình duyệt) xin ký
 - chip token ký                  -> 5. Gắn TSA, ghép CMS + chuỗi CA ghim  ----> TSA ca.gov.vn
                                        [KY-03]
                                     6. Nối bản DSS: CA + OCSP lúc ký
                                        => PAdES-B-LT  [KY-04]
                                     7. Kiểm lại đủ 5 điều kiện [KT-01],
                                        ghi hồ sơ bằng chứng [KT-02]
                                     Kho (sau): document time-stamp
                                        => B-LTA [KY-06]
```

## Danh sách cải tiến

### Plugin (repo `Ký số Plugin`, chỉ bản Windows)

| Mã | Cải tiến | Chặn được | Ước |
|---|---|---|---|
| PL-01 | `AllowedHosts=localhost;127.0.0.1` | DNS rebinding (W1) | 0,5 giờ |
| PL-02 | Hộp đồng ý native khi mở phiên: "Trang <origin> xin ký <N> file bằng chứng thư <CN>". Bấm Đồng ý thì mới bật hộp PIN | Trang khác hoặc mã độc âm thầm mở phiên | 3 giờ |
| PL-03 | `mo-phien` nhận origin và số lượt tối đa, trả mã phiên ngẫu nhiên. `ky` / `dong-phien` phải gửi kèm mã. Hết lượt hoặc sai origin thì từ chối | Gọi `ky` bằng phiên của người khác; ký vượt số file | 3 giờ |
| PL-04 | Chỉ ký dữ liệu là SET OF signedAttributes, có `messageDigest` 32 byte, có `signingCertificateV2` khớp chứng thư của phiên | Dùng token ký challenge đăng nhập, giao dịch khác, hoặc chuỗi tuỳ ý | 2 giờ |
| PL-05 | Chặn ECDSA ở `mo-phien` (token Ban Cơ yếu là RSA) | Chữ ký sai định dạng (W3) | 0,5 giờ |
| PL-06 | Bộ cài: kiểm chữ ký Authenticode của bộ cài hãng lúc đóng gói, ẩn trang chọn môi trường | Đóng gói nhầm bộ cài giả; người dùng chọn nhầm Development | 2 giờ |
| PL-07 | (Tuỳ chọn) Job ticket do máy chủ ký: plugin ghim khoá công khai theo từng origin, chỉ ký các `messageDigest` có trong ticket | Mã độc trên máy và máy chủ giả mạo (T2 / T3 một phần) | 2–3 ngày |

Không đề xuất làm WYSIWYS (plugin nhận cả PDF để tự kiểm nội dung). Lý do: mỗi lô phải đẩy vài GB qua trình duyệt.
Thay vào đó dùng NV-01 (xem trước trên web) cộng với PL-04.

### Máy chủ ký (sh.be)

| Mã | Cải tiến | Căn cứ | Ước |
|---|---|---|---|
| KY-01 | Bỏ thuộc tính signing-time. Giờ ký lấy từ `/M` và TSA | ETSI EN 319 142-1 Bảng 1 | 0,5 giờ |
| KY-02 | Kiểm chứng thư khi mở phiên, fail-closed: hạn, KeyUsage, chuỗi về Root ghim, **OCSP "good"**; giữ phản hồi OCSP cho KY-04. Lô dài quá hạn OCSP (3 giờ) thì lấy lại | NĐ 68 Điều 29, 31 | 4 giờ |
| KY-03 | Nhúng chuỗi dựng bằng CA ghim, không dùng kho chứng thư của máy chủ | K2 | 1 giờ |
| KY-04 | Nối thêm 1 bản `/DSS` gồm chuỗi CA + OCSP người ký + OCSP / chuỗi của TSA → **PAdES-B-LT**. CRL lấy qua HTTPS, theo chuyển hướng | ETSI 5.4; QCVN 16:2025 (ISO 14533-3) | 8 giờ |
| KY-05 | Hai kiểu mẫu: **"Tài liệu lưu trữ (TT05)"** khoá cứng chữ đen, Times New Roman 10, chữ thường đứng, góc trên phải trang đầu, không ảnh dấu; **"Văn bản thường"** cho ảnh chữ ký tươi và dấu. Báo lỗi khi trang của mẫu vượt số trang file | TT05 Điều 8.1.c, 8.2.a; K6 | 5 giờ |
| KY-06 | Kho: document time-stamp khi nhập kho và đóng lại định kỳ → **B-LTA** | ETSI B-LTA; TT05 bảo quản | Thuộc KHO-18 / KHO-23 |
| KY-07 | Giữ PDF/A-2b sau khi ký (font nhúng, annotation có cờ in), kiểm bằng veraPDF ở bước KS-07 | TT05 Điều 8.2.a | 2 giờ |

### Kiểm tra và bằng chứng

| Mã | Cải tiến | Ước |
|---|---|---|
| KT-01 | Một bộ kiểm dùng chung `ISignatureValidatorExternal` trả báo cáo theo đủ 5 điều kiện Điều 30 (toàn vẹn, người ký, thời điểm TSA + imprint, hiệu lực / thu hồi lúc ký từ DSS hoặc OCSP, chuỗi về Root ghim). Dùng cho: tự kiểm sau ký (thay `EnsureValid`), màn NV-02, kho KHO-14 | 8 giờ |
| KT-02 | Hồ sơ bằng chứng cho từng file đã ký: SHA-256 bản gốc và bản ký, số serial + CN chứng thư, giờ TSA, giờ và trạng thái OCSP, mức PAdES đạt được, người thao tác. **Đổi schema** `SigningBatchFile` (hiện chỉ có `SignedAt`, `TimestampedAt`, `IsSignatureValid`) | 4 giờ |

### Nghiệp vụ và giao diện

| Mã | Cải tiến | Ước |
|---|---|---|
| NV-01 | Màn vòng ký: xem trước file đầu đã đặt mẫu chữ ký và tóm tắt "N file, chứng thư X, mẫu Y" trước khi mở phiên | 3 giờ |
| NV-02 | Màn **"Kiểm tra chữ ký số"**: tải lên PDF bất kỳ → báo cáo 5 điều kiện (dùng KT-01). Phục vụ demo với hội đồng và bên nhận file | 5 giờ |
| NV-03 | Phân biệt trên FE "plugin chưa cài" với "trình duyệt chặn quyền mạng cục bộ" (Chrome 142+) | 1 giờ |
| NV-04 | Cảnh báo khi ký mẫu TT05 bằng chứng thư cá nhân. TT05 ghi chữ ký số "của cơ quan, tổ chức, cá nhân có thẩm quyền" nên chỉ cảnh báo, không chặn | 1 giờ |

## Không đổi

- Kiến trúc chia việc: máy chủ dựng PDF và ghép CMS, plugin chỉ ký.
- Hàng đợi ký trong RAM (chạy 1 máy).
- Trình duyệt làm người đưa thư (topology A).
- Bản macOS: tạm hoãn ([05-plugin.md](05-plugin.md)).

## Tổng ước lượng

- Bản đề xuất mặc định (không có PL-07): plugin ~11 giờ + máy chủ ~21 giờ + kiểm tra ~12 giờ + giao diện ~10 giờ,
  tức **khoảng 54 giờ**.
- Cộng thêm vào KS-01…KS-16 (~88 giờ). Một phần trùng việc đã tính trong KS-07 và KS-15.

## Đã chốt 28/11/2026

1. Plugin làm đủ PL-01…PL-07, **có job ticket**. Plan phải chốt thêm: thuật toán khoá ticket (đề xuất Ed25519), cách
   ghim khoá công khai theo từng origin, ticket mang danh sách `messageDigest` của lô.
2. Web thi công xuất **PAdES-B-LT** (KY-04). Kho nâng lên B-LTA (KY-06).
3. **Đổi schema** `SigningBatchFile` để lưu hồ sơ bằng chứng (KT-02). Tên cột chốt khi viết plan.
4. **Làm** màn "Kiểm tra chữ ký số" (NV-02).

Tổng ước sau khi chốt: khoảng 54 giờ + PL-07 (2–3 ngày) ≈ **75 giờ**.
