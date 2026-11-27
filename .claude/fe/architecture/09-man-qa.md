# Màn QA

> **Phần 9/13** · trước: [08-man-nhap-lieu.md](08-man-nhap-lieu.md) · mục lục: [README.md](README.md)

Task 44 (cập nhật 2026-11-23). Màn chấm của người QA: PDF bên trái, giá trị từng trường bên phải (chỉ đọc), bấm "Sai" ở
trường không khớp rồi chấm Đạt hoặc Trả lại, xong tự sang văn bản kế trong bộ lọc.

## Route và file

| Thứ | Chỗ |
|---|---|
| Route | `.../project/:id/metadata/:documentTypeId/qa/:submissionId`, quyền như danh sách, `canDeactivate` inline trong `construction.routes.ts` |
| Màn | `submission/submission-qa/submission-qa.component.*` |
| API | `GET submission/{id}/qa` (chi tiết + người kiểm), `GET submission/{id}/qa-logs`, `PUT submission/qa` (`result` 1 Đạt / 2 Không đạt) |
| Bố cục | `submission/submission-layout.ts`: cùng khoá localStorage với màn Nhập liệu, hai màn chia chung một bố cục |
| Khung PDF | Dùng chung `submission-pdf-viewer` và `SubmissionPdfCacheService` với màn Nhập liệu ([08-man-nhap-lieu.md](08-man-nhap-lieu.md)): màn khai `providers`, đổi Ngang / Dọc không tải lại file (Task 69) |
| Đầu / cuối bộ lọc | `isFilterEdge` trong `submission-query.ts`, dùng chung với màn Nhập liệu |
| Thanh trên | Cùng khuôn với màn Nhập liệu từ Task 53 (cỡ, cụm nút trước / sau, dòng 2 có tên metadata qua query `documentTypeName`), không có nút Lưu. Xem [08-man-nhap-lieu.md](08-man-nhap-lieu.md#giao-diện-task-53) |
| Hằng | `QA_RESULT`, `QA_REASON_MAX_LENGTH` (1000), `QA_QUICK_REASONS`, mã lỗi 320–345 ở `submission.constants.ts` |

## Quyết định

- Chưa đánh dấu trường nào → thanh (a) "Để sau" + "Đạt". Có trường "Sai" → thanh (b) Trả lại, bắt buộc lý do (đã trim).
  Chip "Thêm nhanh" nối vào cuối lý do rồi focus lại ô. Ctrl + Enter = nút chính của thanh đang hiện.
- Banner chỉ đọc theo thứ tự: Leader → thiếu `Function.Construction.Submission.Qa` → dự án Xong / Hết hạn (trừ SuperAdmin)
  → chưa kiểm → đang bị trả (đỏ) → đã Đạt (xanh). **Leader xét trước quyền** vì DB dev Leader không có quyền Qa, xét quyền
  trước thì Leader thấy nhầm câu "thiếu quyền". Banner chỉ đọc thắng banner vàng "Lần trước sai".
- "Lần trước sai" lấy từ log QA Không đạt gần nhất; trường ẩn (`isShowListView = false`) mà bị đánh sai lần trước vẫn hiện.
  Lượt QA = số log + 1, chưa có log hiện "—".
- Hết văn bản trong bộ lọc: **không** có trạng thái "đã chấm hết". nút trước / sau và "Văn bản sau" chỉ **tắt** (không ẩn) theo
  `position` / `totalItems`; chấm xong mà `next` trả 317 thì ở lại văn bản vừa chấm và tải lại để thấy kết quả.
- Dự án không bật QA / người Nhập liệu vào màn này → toast rồi về danh sách. 820 → trạng thái trống "không được giao".
- Lỗi khi chấm 316 / 320 / 323 / 332 / 334 / 345 → tải lại dự án + văn bản (thường thành chỉ đọc), giữ nguyên đánh dấu nếu
  vẫn cùng văn bản. Đổi văn bản thì xoá đánh dấu.
- Có đánh dấu hoặc lý do mà rời màn → hỏi "Bỏ kết quả đang chấm?". Cờ `_allowLeave` như màn Nhập liệu.

## Bẫy

- Lưu ý: `sh-input-text` tự trim giá trị khi gõ và tự hiện lỗi "bắt buộc" trùng với dòng gợi ý → khai
  `[isShowMessageInvalid]="false"`. Bộ đếm ký tự đọc `reasonControl.value`, không tự đếm lại.
- Lưu ý: Phím tắt nghe ở `document:keydown` (lý do ở [08-man-nhap-lieu.md](08-man-nhap-lieu.md)); focus trong iframe PDF thì
  Ctrl + ↑ / ↓ / Enter không bắt được.
- Lưu ý: Test trên DB dev qua tunnel: mỗi lần mở văn bản mất 3–6 s. Bấm khi màn đang bận thì nút bị tắt, thao tác mất — đừng
  coi là lỗi điều hướng.

## Đã kiểm thực tế (Task 72)

Ngày 2026-11-27, Chrome thật + API thật trên DB dev, dự án tạm ZZTEST72 (có QA, 2 hộp × 6 văn bản, Data A kiểm hết, Q phụ trách cả hai hộp)
và ZZTEST72B (không QA). Văn bản chèn bằng SQL vì MinIO đầy.

- Kết quả 24 / 24 bước của bảng 44.3 đạt. Bước 24 (đóng tab khi chấm dở) kiểm gián tiếp: đang có trường đánh dấu Sai thì điều hướng bị
  chặn bởi hộp "Leave site?".
- Bước 1 lọc theo hộp A để ra "2 / 6" (dự án có 12 văn bản chưa QA). Bước 22 chạy đầu tiên với 12 văn bản, 10 dòng mỗi trang, mở văn bản ở trang 2.
- Đổi ở Task 44: bước 12 chấm văn bản cuối bộ lọc thì ở lại văn bản vừa chấm (tải lại, banner xanh, tag "Ngoài bộ lọc", ▶ và "Văn bản sau"
  tắt), không có trạng thái "Đã chấm hết văn bản trong bộ lọc".
- Dùng API thay màn: bước 9 (Q trả lại trường ẩn F5 rồi Data sửa), bước 20 (Admin chấm Đạt khi Q còn mở màn), chuyển dự án sang ACCEPTING
  / DONE (API nghiệm thu). Bước 8 và 9 Data sửa lại bằng màn Nhập liệu / API `check`.
- Bước 20 thực tế: Q gửi Đạt sau khi Admin đã chấm Đạt thì BE nhận, toast "Đã chấm Đạt …", không báo lỗi (chấm đè Đạt → Đạt, không tính tiền
  theo Task 68). Không phải lỗi màn.
- Bước 21: Dọc + A+ ở màn QA, mở màn Nhập liệu thì cùng bố cục (khoá localStorage chung).
- Đã dọn: dự án, metadata, văn bản, log và audit của ZZTEST72 / 72B / 72G.

> **Tiếp:** [10-man-operation.md](10-man-operation.md) — màn module Operation: Thành viên dự án.
