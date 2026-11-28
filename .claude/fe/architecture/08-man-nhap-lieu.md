# Màn Nhập liệu và dialog Lịch sử

> **Phần 8/13** · trước: [07-man-construction.md](07-man-construction.md) · mục lục: [README.md](README.md)

Task 43, giao diện làm lại ở Task 53 (cập nhật 2026-11-23). Màn làm việc của người Data: PDF bên trái, các trường bên phải, Lưu = Kiểm tra rồi sang văn bản kế.

## Route và file

| Thứ | Chỗ |
|---|---|
| Route | `.../project/:id/metadata/:documentTypeId/check/:submissionId`, quyền như danh sách, `canDeactivate` khai inline trong `construction.routes.ts` |
| Màn | `submission/submission-check.component.*` |
| Khung PDF | `submission/submission-pdf-viewer/`: tải qua `proxy-file` bằng `DownloadService`, hiện bằng blob URL trong iframe, có nút Thử lại. File đã tải do `SubmissionPdfCacheService` (cùng thư mục, khai `providers` của màn) giữ, một mục `{fileUrl, objectUrl}`: đổi Ngang / Dọc (dựng lại Splitter) cùng văn bản không tải lại; sang văn bản khác hoặc rời màn thì tải lại và thu hồi objectUrl cũ; lỗi tải không lưu cache (Task 69) |
| Dialog Lịch sử | `submission/submission-history-dialog/`: hàm `openSubmissionHistory(dialogService, id, fileName)`, dùng chung cho danh sách và màn này |
| Đọc query | `submission/submission-query.ts` (`readSubmissionQuery`), dùng chung với danh sách. Riêng `documentTypeName` (`DOCUMENT_TYPE_NAME_PARAM`) chỉ để hiển thị, không nằm trong `SubmissionQuery` |
| Bố cục | `submission-layout.ts` (dùng chung với màn QA): localStorage `construction.submission-check.layout` = `{ direction, sizes, fontSize }`, sai thì lấy mặc định |

## Quyết định

- Bộ lọc đi theo query string (cùng tên tham số với danh sách, bỏ trang, giữ `view`). nút trước / sau gọi `previous` / `next` rồi đổi
  `:submissionId` bằng `replaceUrl`. Đầu / cuối bộ lọc thì **tắt** nút (không ẩn, không toast) theo `position` / `totalItems`
  (`isFilterEdge`), 317 cũng tắt chiều đó. `position = null` thì hiện tag "Ngoài bộ lọc".
- Chỉ đọc khi: thiếu `Function.Construction.Submission.Check` (Leader), dự án Xong / Hết hạn (trừ SuperAdmin), hoặc Data gặp văn
  bản đã QA Đạt. Câu `readonlyBanner.message` hiện ở dòng tóm tắt kèm icon khoá, không có khung riêng. QA vào màn này thì bị đẩy
  về danh sách.
- Trường có `enumValues` (kiểu danh mục, Task 75) là `p-autocomplete` (gõ lọc gợi ý theo "chứa chuỗi", nút dropdown, `forceSelection` tắt nên gõ được giá trị khác, `appendTo="body"`, `inputId` = id ô cũ để focus tự động vẫn chạy). Giá trị khác rỗng và không thuộc danh mục (trim, không phân biệt hoa thường, `isOutOfEnum`) hiện dòng "Ngoài danh mục" màu `--app-blue` (không amber vì amber là "Đã sửa", không đỏ vì đỏ là QA sai); Lưu không bị chặn.
- Trường hiện = `isShowListView` **hoặc** bị QA đánh sai. Lưu gửi **mọi** trường: BE **giữ nguyên** trường không gửi lên (`BuildCheckPlan` chỉ duyệt trường có trong `inputs`), nhưng FE vẫn gửi đủ để log cũ → mới đầy đủ và form khớp sau lưu.
- Dự án đang nghiệm thu (Task 74.6): sửa văn bản đã QA Đạt giữ kết quả QA (BE `isProjectAccepting`), nên **không** hỏi, lưu thẳng; dòng tóm tắt thêm "· Dự án đang nghiệm thu: sửa
  không làm mất kết quả QA đạt" (getter `keepsQaPassed`). Dự án đang triển khai giữ hành vi dưới.
- Quản lý sửa văn bản đã QA Đạt thì hỏi "Lưu và QA lại" trước. Lưu xong gọi `next`; hết văn bản (317) hoặc `next` lỗi thì ở lại và
  tải lại văn bản để form khớp giá trị vừa lưu.
- Lỗi khi Lưu: luôn giữ giá trị đang gõ. 826 / 316 / 323 thì tải lại dự án + văn bản (thành chỉ đọc). 820 hiện trạng thái trống
  "không được giao".
- Cờ `_allowLeave` cho lần điều hướng sau "Rời đi" hoặc sau Lưu đi qua guard, tránh hỏi hai lần. Guard chạy cả khi chỉ đổi `:submissionId`.
- Dialog Lịch sử: BE trả cũ → mới, FE đảo lại. `changedFields = null` mà `changedDataCount > 0` là log trước Task 43A, hiện
  "Bản ghi cũ…". Lỗi thì toast rồi đóng. Danh sách có mục "Lịch sử" cho mọi vai trò.

## Giao diện (Task 53)

- Thanh trên cao 56px. Trái: nút ←, khối 2 dòng gồm tên file (cắt "…", tooltip tên đủ) + tag trạng thái, rồi `locationText`
  = Hồ sơ · Hộp · tên metadata. Tên metadata đi theo query `documentTypeName` từ danh sách, nút trước / sau giữ lại tham số này. Mở link
  trực tiếp không có tham số thì dòng 2 bỏ phần đó.
- Lưu ý: Hồ sơ / Hộp import có khi đã kèm tiền tố ("HS 114", "Hộp 12") → ghép thêm "Hồ sơ " / "Hộp " ra "Hộp Hộp 12". Dùng
  `buildLocationText` (`submission-location.ts`, chung màn Nhập liệu và QA): giá trị đã bắt đầu bằng hồ sơ / hs / hộp thì giữ nguyên.
- Phải: cụm [trước | x / y | sau], các nút text Chia / A− / A+ / Lịch sử, nút Lưu. Văn bản đang bị trả thì nhãn nút là
  "Lưu · gửi QA lại". Chỉ đọc thì ẩn nút Lưu, chân form bỏ vế "Ctrl + Enter: Lưu".
- Tiêu đề form có dòng tóm tắt (`formSummary`). Chỉ đọc → xám + khoá. Trả lại còn trường sai → đỏ "Còn X / Y trường QA đánh sai".
  Trả lại và đã sửa hết → cam. Các trường hợp khác → "N trường".
- Không còn khối đỏ "QA trả lại" và chip cạnh nhãn. Trường sai có **một dòng** chú thích dưới ô (`wrongFieldNote`), cắt "…",
  `title` = toàn câu. Chưa sửa: viền + chữ đỏ "QA đánh sai — lý do", lý do rỗng thì chỉ "QA đánh sai". Đã sửa: cam "Đã sửa · sẽ
  gửi QA lại khi Lưu". Văn bản bị trả mà đang chỉ đọc (dự án xong…) vẫn hiện chú thích đỏ.
- Mở văn bản bị trả thì focus trường sai đầu tiên. Các trường hợp khác focus trường đầu.
- Cỡ dùng lớp Tailwind chuẩn theo [05-conventions.md](05-conventions.md): nhãn `text-xs`, ô `h-9`. A− / A+ chỉ đổi chữ trong ô.
  Khi nào phải thêm `!` lên nút PrimeNG: xem bẫy `cssLayer` ở 05.

## Bẫy

- Lưu ý: Phím tắt (Ctrl+Enter, Ctrl+↑/↓) phải nghe ở `document:keydown`, đừng nghe ở host component. Lý do: đóng hộp hỏi xong focus
  rơi về `body`, sự kiện không đi qua host nên phím tắt chết đến khi người dùng bấm vào ô nhập. Phím đến từ trong `.p-dialog` thì
  bỏ qua, kẻo Ctrl+Enter lưu ngầm sau dialog. Focus nằm trong iframe PDF thì không bắt được, giới hạn của trình duyệt.
- Lưu ý: Ô danh mục (`p-autocomplete`) vẫn để Ctrl + Enter / Ctrl + ↑ / ↓ lọt tới `document:keydown` khi đang mở gợi ý (kiểm Karma + trình duyệt 2026-11-28). Ctrl + ↑ / ↓ đồng thời di chuyển mục sáng của gợi ý; nếu đang có mục được sáng bằng phím mũi tên thì Ctrl + Enter có thể chọn mục đó rồi mới lưu (chưa kiểm riêng ca này). Đừng chặn phím trong ô để "sửa" — mất phím tắt Lưu khi con trỏ ở ô danh mục.
- Lưu ý: `SubmissionPdfViewerComponent` inject `SubmissionPdfCacheService` (không `providedIn`), nên màn nào dùng `app-submission-pdf-viewer` phải khai `providers: [SubmissionPdfCacheService]`, thiếu thì lỗi NullInjector. Viewer bị huỷ không thu hồi objectUrl vì cache giữ; đừng thêm `revokeObjectURL` vào viewer.
- Lưu ý: Kéo thanh chia mà iframe nuốt sự kiện chuột thì thanh "dính". Lúc kéo phải gắn `pointer-events-none` lên khung PDF.
- Lưu ý: Nút ← cạnh khối tên bị co còn 21–29px khi tên file dài. Nguyên nhân: nút trong flex mặc định `shrink: 1`. Cách tránh: thêm
  `shrink-0` cho nút (đã thêm ở cả màn Nhập liệu và màn QA).
- Lưu ý: `SplitterResizeEndEvent.sizes` có kiểu `(string | number)[]`, phải `Number()` trước khi lưu.
- Lưu ý: Đổi hướng chia phải dựng lại Splitter (`@for` trên `[layout.direction]`). Chỉ đổi `[layout]` thì kích thước cũ còn kẹt.
- Lưu ý: `proxy-file` lỗi vẫn trả HTTP 200 kèm JSON (315 / 305), nên luôn đọc qua `readErrorResponse`. BE chỉ kiểm URL có thuộc một
  văn bản nào không, **không** kiểm người gọi có được giao văn bản đó.

## Đã kiểm thực tế (Task 72)

Ngày 2026-11-27, Chrome thật + API thật trên DB dev qua tunnel, dự án tạm ZZTEST72 (có QA) và ZZTEST72B (không QA), tài khoản có sẵn
(SuperAdmin, Admin, Leader, 2 Data, 2 QA). Metadata, trường, dự án, thành viên và chia phiếu đi qua API; riêng văn bản chèn bằng SQL vì
MinIO đầy (import Excel báo `209`) và không có Google Sheet.

- Kết quả 21 / 21 bước của bảng 43.3 đạt. Bước 21 (đóng tab khi có sửa chưa lưu) kiểm gián tiếp: điều hướng bị chặn bởi hộp "Leave site?".
- Bước giao diện đã đổi ở Task 53, kiểm theo tài liệu này: 6 và 8 (hết văn bản thì nút ▶ tắt, không có toast "Đã hết văn bản…"; từ văn bản
  "Ngoài bộ lọc" bấm ▶ không đi tiếp được, ◀ đi được), 9 (không còn banner đỏ và chip, thay bằng dòng "Còn X / Y trường QA đánh sai" và
  chú thích dưới ô), 12 (khoá hiện ở dòng tóm tắt, không có khung xanh riêng).
- Dùng API thay màn: bước 11 (màn QA không đánh dấu được trường ẩn), chuyển dự án sang ACCEPTING / DONE (API nghiệm thu), Q chấm Đạt lại
  văn bản để dựng bước 14. Bước 19 sửa `fileUrlS3Object` bằng SQL rồi trả về như cũ.
- Chưa kiểm: dòng "Bản ghi cũ…" của log trước Task 43A (dữ liệu tạm không có log cũ).
- Lệch báo PM, chưa sửa (thuộc BE): Admin sửa hộ văn bản của Data thì dòng QA trong Lịch sử ghi "Người nhập: admin02", không phải người giữ văn bản.
- Lưu ý: mỗi lần mở văn bản mất 3–10 s do tunnel. Sau F5 màn trắng tới khi tải xong; đừng coi là lỗi khi chụp màn quá sớm.
- Đã dọn: dự án, metadata, văn bản, log và audit của ZZTEST72 / 72B.
- Task 74 (2026-11-28, `ZZTEST74*` đã dọn): dự án `ACCEPTING` + văn bản QA đạt, Admin sửa một trường rồi Lưu → không hộp hỏi, toast thường, `StatusQa` vẫn đạt, log "… giữ kết quả QA đạt.",
  không có dòng giữ QA / đợt; dòng tóm tắt hiện câu nghiệm thu. Dự án `DOING` cùng thao tác vẫn hiện hộp "Văn bản đã QA đạt" và không có câu nghiệm thu.

> **Tiếp:** [09-man-qa.md](09-man-qa.md) — màn QA chấm Đạt / Trả lại.
