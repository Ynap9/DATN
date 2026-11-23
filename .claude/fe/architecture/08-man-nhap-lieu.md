# Màn Nhập liệu và dialog Lịch sử

> **Phần 8/9** · trước: [07-man-construction.md](07-man-construction.md) · mục lục: [README.md](README.md)

Task 43 (cập nhật 2026-11-22). Màn làm việc của người Data: PDF bên trái, các trường bên phải, Lưu = Kiểm tra rồi sang văn bản kế.

## Route và file

| Thứ | Chỗ |
|---|---|
| Route | `.../project/:id/metadata/:documentTypeId/check/:submissionId`, quyền như danh sách, `canDeactivate` khai inline trong `construction.routes.ts` |
| Màn | `submission/submission-check.component.*` |
| Khung PDF | `submission/submission-pdf-viewer/`: tải qua `proxy-file` bằng `DownloadService`, hiện bằng blob URL trong iframe, có nút Thử lại |
| Dialog Lịch sử | `submission/submission-history-dialog/`: hàm `openSubmissionHistory(dialogService, id, fileName)`, dùng chung cho danh sách và màn này |
| Đọc query | `submission/submission-query.ts` (`readSubmissionQuery`), dùng chung với danh sách |
| Bố cục | `submission-layout.ts` (dùng chung với màn QA): localStorage `construction.submission-check.layout` = `{ direction, sizes, fontSize }`, sai thì lấy mặc định |

## Quyết định

- Bộ lọc đi theo query string (cùng tên tham số với danh sách, bỏ trang, giữ `view`). ◀ ▶ gọi `previous` / `next` rồi đổi
  `:submissionId` bằng `replaceUrl`. Đầu / cuối bộ lọc thì **tắt** nút (không ẩn, không toast) theo `position` / `totalItems`
  (`isFilterEdge`), 317 cũng tắt chiều đó. `position = null` thì hiện tag "Ngoài bộ lọc".
- Chỉ đọc khi: thiếu `Function.Construction.Submission.Check` (Leader), dự án Xong / Hết hạn (trừ SuperAdmin), hoặc Data gặp văn
  bản đã QA Đạt (banner xanh). QA vào màn này thì bị đẩy về danh sách.
- Trường hiện = `isShowListView` **hoặc** bị QA đánh sai. Lưu gửi **mọi** trường: trường ẩn gửi giá trị gốc, không thì BE coi là xoá trắng.
- Quản lý sửa văn bản đã QA Đạt thì hỏi "Lưu và QA lại" trước. Lưu xong gọi `next`; hết văn bản (317) hoặc `next` lỗi thì ở lại và
  tải lại văn bản để form khớp giá trị vừa lưu.
- Lỗi khi Lưu: luôn giữ giá trị đang gõ. 826 / 316 / 323 thì tải lại dự án + văn bản (thành chỉ đọc). 820 hiện trạng thái trống
  "không được giao".
- Cờ `_allowLeave` cho lần điều hướng sau "Rời đi" hoặc sau Lưu đi qua guard, tránh hỏi hai lần. Guard chạy cả khi chỉ đổi `:submissionId`.
- Dialog Lịch sử: BE trả cũ → mới, FE đảo lại. `changedFields = null` mà `changedDataCount > 0` là log trước Task 43A, hiện
  "Bản ghi cũ…". Lỗi thì toast rồi đóng. Danh sách có mục "Lịch sử" cho mọi vai trò.

## Bẫy

- ⚠️ Phím tắt (Ctrl+Enter, Ctrl+↑/↓) phải nghe ở `document:keydown`, đừng nghe ở host component. Lý do: đóng hộp hỏi xong focus
  rơi về `body`, sự kiện không đi qua host nên phím tắt chết đến khi người dùng bấm vào ô nhập. Phím đến từ trong `.p-dialog` thì
  bỏ qua, kẻo Ctrl+Enter lưu ngầm sau dialog. Focus nằm trong iframe PDF thì không bắt được, giới hạn của trình duyệt.
- ⚠️ Kéo thanh chia mà iframe nuốt sự kiện chuột thì thanh "dính". Lúc kéo phải gắn `pointer-events-none` lên khung PDF.
- ⚠️ `SplitterResizeEndEvent.sizes` có kiểu `(string | number)[]`, phải `Number()` trước khi lưu.
- ⚠️ Đổi hướng chia phải dựng lại Splitter (`@for` trên `[layout.direction]`). Chỉ đổi `[layout]` thì kích thước cũ còn kẹt.
- ⚠️ `proxy-file` lỗi vẫn trả HTTP 200 kèm JSON (315 / 305), nên luôn đọc qua `readErrorResponse`. BE chỉ kiểm URL có thuộc một
  văn bản nào không, **không** kiểm người gọi có được giao văn bản đó.

> **Tiếp:** [09-man-qa.md](09-man-qa.md) — màn QA chấm Đạt / Trả lại.
