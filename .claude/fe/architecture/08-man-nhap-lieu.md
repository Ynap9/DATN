# Màn Nhập liệu và dialog Lịch sử

> **Phần 8/9** · trước: [07-man-construction.md](07-man-construction.md) · mục lục: [README.md](README.md)

Task 43, giao diện làm lại ở Task 53 (cập nhật 2026-11-23). Màn làm việc của người Data: PDF bên trái, các trường bên phải, Lưu = Kiểm tra rồi sang văn bản kế.

## Route và file

| Thứ | Chỗ |
|---|---|
| Route | `.../project/:id/metadata/:documentTypeId/check/:submissionId`, quyền như danh sách, `canDeactivate` khai inline trong `construction.routes.ts` |
| Màn | `submission/submission-check.component.*` |
| Khung PDF | `submission/submission-pdf-viewer/`: tải qua `proxy-file` bằng `DownloadService`, hiện bằng blob URL trong iframe, có nút Thử lại |
| Dialog Lịch sử | `submission/submission-history-dialog/`: hàm `openSubmissionHistory(dialogService, id, fileName)`, dùng chung cho danh sách và màn này |
| Đọc query | `submission/submission-query.ts` (`readSubmissionQuery`), dùng chung với danh sách. Riêng `documentTypeName` (`DOCUMENT_TYPE_NAME_PARAM`) chỉ để hiển thị, không nằm trong `SubmissionQuery` |
| Bố cục | `submission-layout.ts` (dùng chung với màn QA): localStorage `construction.submission-check.layout` = `{ direction, sizes, fontSize }`, sai thì lấy mặc định |

## Quyết định

- Bộ lọc đi theo query string (cùng tên tham số với danh sách, bỏ trang, giữ `view`). ◀ ▶ gọi `previous` / `next` rồi đổi
  `:submissionId` bằng `replaceUrl`. Đầu / cuối bộ lọc thì **tắt** nút (không ẩn, không toast) theo `position` / `totalItems`
  (`isFilterEdge`), 317 cũng tắt chiều đó. `position = null` thì hiện tag "Ngoài bộ lọc".
- Chỉ đọc khi: thiếu `Function.Construction.Submission.Check` (Leader), dự án Xong / Hết hạn (trừ SuperAdmin), hoặc Data gặp văn
  bản đã QA Đạt. Câu `readonlyBanner.message` hiện ở dòng tóm tắt kèm icon khoá, không có khung riêng. QA vào màn này thì bị đẩy
  về danh sách.
- Trường hiện = `isShowListView` **hoặc** bị QA đánh sai. Lưu gửi **mọi** trường: trường ẩn gửi giá trị gốc, không thì BE coi là xoá trắng.
- Quản lý sửa văn bản đã QA Đạt thì hỏi "Lưu và QA lại" trước. Lưu xong gọi `next`; hết văn bản (317) hoặc `next` lỗi thì ở lại và
  tải lại văn bản để form khớp giá trị vừa lưu.
- Lỗi khi Lưu: luôn giữ giá trị đang gõ. 826 / 316 / 323 thì tải lại dự án + văn bản (thành chỉ đọc). 820 hiện trạng thái trống
  "không được giao".
- Cờ `_allowLeave` cho lần điều hướng sau "Rời đi" hoặc sau Lưu đi qua guard, tránh hỏi hai lần. Guard chạy cả khi chỉ đổi `:submissionId`.
- Dialog Lịch sử: BE trả cũ → mới, FE đảo lại. `changedFields = null` mà `changedDataCount > 0` là log trước Task 43A, hiện
  "Bản ghi cũ…". Lỗi thì toast rồi đóng. Danh sách có mục "Lịch sử" cho mọi vai trò.

## Giao diện (Task 53)

- Thanh trên cao 56px. Trái: nút ←, khối 2 dòng gồm tên file (cắt "…", tooltip tên đủ) + tag trạng thái, rồi `locationText`
  = Hồ sơ · Hộp · tên metadata. Tên metadata đi theo query `documentTypeName` từ danh sách, ◀ ▶ giữ lại tham số này. Mở link
  trực tiếp không có tham số thì dòng 2 bỏ phần đó.
- Phải: cụm [◀ | x / y | ▶], các nút text Chia / A− / A+ / Lịch sử, nút Lưu. Văn bản đang bị trả thì nhãn nút là
  "Lưu · gửi QA lại". Chỉ đọc thì ẩn nút Lưu, chân form bỏ vế "Ctrl + Enter: Lưu".
- Tiêu đề form có dòng tóm tắt (`formSummary`). Chỉ đọc → xám + khoá. Bị trả còn trường sai → đỏ "Còn X / Y trường QA đánh sai".
  Bị trả và đã sửa hết → cam. Các trường hợp khác → "N trường".
- Không còn khối đỏ "QA trả lại" và chip cạnh nhãn. Trường sai có **một dòng** chú thích dưới ô (`wrongFieldNote`), cắt "…",
  `title` = toàn câu. Chưa sửa: viền + chữ đỏ "QA đánh sai — lý do", lý do rỗng thì chỉ "QA đánh sai". Đã sửa: cam "Đã sửa · sẽ
  gửi QA lại khi Lưu". Văn bản bị trả mà đang chỉ đọc (dự án xong…) vẫn hiện chú thích đỏ.
- Mở văn bản bị trả thì focus trường sai đầu tiên. Các trường hợp khác focus trường đầu.
- Cỡ dùng lớp Tailwind chuẩn theo [05-conventions.md](05-conventions.md): nhãn `text-xs`, ô `h-9`. A− / A+ chỉ đổi chữ trong ô.
  Khi nào phải thêm `!` lên nút PrimeNG: xem bẫy `cssLayer` ở 05.

## Bẫy

- ⚠️ Phím tắt (Ctrl+Enter, Ctrl+↑/↓) phải nghe ở `document:keydown`, đừng nghe ở host component. Lý do: đóng hộp hỏi xong focus
  rơi về `body`, sự kiện không đi qua host nên phím tắt chết đến khi người dùng bấm vào ô nhập. Phím đến từ trong `.p-dialog` thì
  bỏ qua, kẻo Ctrl+Enter lưu ngầm sau dialog. Focus nằm trong iframe PDF thì không bắt được, giới hạn của trình duyệt.
- ⚠️ Kéo thanh chia mà iframe nuốt sự kiện chuột thì thanh "dính". Lúc kéo phải gắn `pointer-events-none` lên khung PDF.
- ⚠️ Nút ← cạnh khối tên bị co còn 21–29px khi tên file dài. Nguyên nhân: nút trong flex mặc định `shrink: 1`. Cách tránh: thêm
  `shrink-0` cho nút (đã thêm ở cả màn Nhập liệu và màn QA).
- ⚠️ `SplitterResizeEndEvent.sizes` có kiểu `(string | number)[]`, phải `Number()` trước khi lưu.
- ⚠️ Đổi hướng chia phải dựng lại Splitter (`@for` trên `[layout.direction]`). Chỉ đổi `[layout]` thì kích thước cũ còn kẹt.
- ⚠️ `proxy-file` lỗi vẫn trả HTTP 200 kèm JSON (315 / 305), nên luôn đọc qua `readErrorResponse`. BE chỉ kiểm URL có thuộc một
  văn bản nào không, **không** kiểm người gọi có được giao văn bản đó.

> **Tiếp:** [09-man-qa.md](09-man-qa.md) — màn QA chấm Đạt / Trả lại.
