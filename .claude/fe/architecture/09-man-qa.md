# Màn QA

> **Phần 9/10** · trước: [08-man-nhap-lieu.md](08-man-nhap-lieu.md) · mục lục: [README.md](README.md)

Task 44 (cập nhật 2026-11-23). Màn chấm của người QA: PDF bên trái, giá trị từng trường bên phải (chỉ đọc), bấm "Sai" ở
trường không khớp rồi chấm Đạt hoặc Trả lại, xong tự sang văn bản kế trong bộ lọc.

## Route và file

| Thứ | Chỗ |
|---|---|
| Route | `.../project/:id/metadata/:documentTypeId/qa/:submissionId`, quyền như danh sách, `canDeactivate` inline trong `construction.routes.ts` |
| Màn | `submission/submission-qa/submission-qa.component.*` |
| API | `GET submission/{id}/qa` (chi tiết + người kiểm), `GET submission/{id}/qa-logs`, `PUT submission/qa` (`result` 1 Đạt / 2 Không đạt) |
| Bố cục | `submission/submission-layout.ts`: cùng khoá localStorage với màn Nhập liệu, hai màn chia chung một bố cục |
| Đầu / cuối bộ lọc | `isFilterEdge` trong `submission-query.ts`, dùng chung với màn Nhập liệu |
| Thanh trên | Cùng khuôn với màn Nhập liệu từ Task 53 (cỡ, cụm ◀ ▶, dòng 2 có tên metadata qua query `documentTypeName`), không có nút Lưu. Xem [08-man-nhap-lieu.md](08-man-nhap-lieu.md#giao-diện-task-53) |
| Hằng | `QA_RESULT`, `QA_REASON_MAX_LENGTH` (1000), `QA_QUICK_REASONS`, mã lỗi 320–345 ở `submission.constants.ts` |

## Quyết định

- Chưa đánh dấu trường nào → thanh (a) "Để sau" + "Đạt". Có trường "Sai" → thanh (b) Trả lại, bắt buộc lý do (đã trim).
  Chip "Thêm nhanh" nối vào cuối lý do rồi focus lại ô. Ctrl + Enter = nút chính của thanh đang hiện.
- Banner chỉ đọc theo thứ tự: Leader → thiếu `Function.Construction.Submission.Qa` → dự án Xong / Hết hạn (trừ SuperAdmin)
  → chưa kiểm → đang bị trả (đỏ) → đã Đạt (xanh). **Leader xét trước quyền** vì DB dev Leader không có quyền Qa, xét quyền
  trước thì Leader thấy nhầm câu "thiếu quyền". Banner chỉ đọc thắng banner vàng "Lần trước sai".
- "Lần trước sai" lấy từ log QA Không đạt gần nhất; trường ẩn (`isShowListView = false`) mà bị đánh sai lần trước vẫn hiện.
  Lượt QA = số log + 1, chưa có log hiện "—".
- Hết văn bản trong bộ lọc: **không** có trạng thái "đã chấm hết". ◀ ▶ và "Văn bản sau" chỉ **tắt** (không ẩn) theo
  `position` / `totalItems`; chấm xong mà `next` trả 317 thì ở lại văn bản vừa chấm và tải lại để thấy kết quả.
- Dự án không bật QA / người Nhập liệu vào màn này → toast rồi về danh sách. 820 → trạng thái trống "không được giao".
- Lỗi khi chấm 316 / 320 / 323 / 332 / 334 / 345 → tải lại dự án + văn bản (thường thành chỉ đọc), giữ nguyên đánh dấu nếu
  vẫn cùng văn bản. Đổi văn bản thì xoá đánh dấu.
- Có đánh dấu hoặc lý do mà rời màn → hỏi "Bỏ kết quả đang chấm?". Cờ `_allowLeave` như màn Nhập liệu.

## Bẫy

- ⚠️ `sh-input-text` tự trim giá trị khi gõ và tự hiện lỗi "bắt buộc" trùng với dòng gợi ý → khai
  `[isShowMessageInvalid]="false"`. Bộ đếm ký tự đọc `reasonControl.value`, không tự đếm lại.
- ⚠️ Phím tắt nghe ở `document:keydown` (lý do ở [08-man-nhap-lieu.md](08-man-nhap-lieu.md)); focus trong iframe PDF thì
  Ctrl + ↑ / ↓ / Enter không bắt được.
- ⚠️ Test trên DB dev qua tunnel: mỗi lần mở văn bản mất 3–6 s. Bấm khi màn đang bận thì nút bị tắt, thao tác mất — đừng
  coi là lỗi điều hướng.

> **Tiếp:** [10-man-operation.md](10-man-operation.md) — màn module Operation: Thành viên dự án.
