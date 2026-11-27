# Màn Báo cáo tiến độ (Construction)

> **Phần 15/16** · trước: [14-man-tien-cong.md](14-man-tien-cong.md) · mục lục: [README.md](README.md)

Task 46 (cập nhật Task 71, 2026-11-27): `/admin/construction/project/:id` (`project/project-report/`), chỉ để xem — không có lối sang danh sách văn bản.
Tách khỏi [07-man-construction.md](07-man-construction.md) từ Task 71.

## Lọc và quyền

- Chỉ Admin / Leader: không có metadata nào mình quản lý → toast "Chỉ quản lý dự án xem được báo cáo tiến độ" rồi về danh sách dự án.
- Query string `?documentTypeId=&range=7|30|all` là nguồn trạng thái; thiếu / sai → metadata quản lý đầu tiên + 30 ngày, ghi lên URL
  (`replaceUrl`) rồi mới tải để không gọi số hai lần. Không có "Tất cả" metadata; quản lý một metadata thì hiện tên, không dropdown.
- `buildRange`: 7 / 30 ngày tính lùi từ hôm nay; Toàn bộ = từ `startedAt` nhưng không quá 366 ngày (trần BE). Nhãn khoảng lấy
  `fromDate` / `toDate` trong response.

## Các khối

| Khối | Nguồn | Theo |
|---|---|---|
| KPI | `acceptance` + `daily` (+N 7 ngày) + `qa-stats/summary` (đạt lần đầu) | metadata đang chọn |
| Tiến độ theo ngày | `progress-stats/daily` | metadata + khoảng |
| Nghiệm thu | `project/{id}/acceptance` | toàn dự án |
| Trạng thái theo metadata | `acceptance` (mọi metadata, kể cả "(đã gỡ)") | toàn dự án, bỏ qua khoảng |
| Chất lượng QA | `qa-stats/summary` | metadata + khoảng |
| Trường hay sai nhất | `qa-stats/fields` (chỉ dự án có QA) | metadata + khoảng, `@defer (on viewport)` |
| Năng suất (Task 71) | `progress-stats/users` + `qa-stats/entry-users` (ghép cột chất lượng) | metadata + khoảng, `@defer (on viewport)`, hàng riêng dưới cùng |

- Mỗi khối một luồng `switchMap` riêng qua `bindReportBlock` (`project-report.constants.ts`): đổi lọc huỷ request cũ, lỗi chỉ đánh dấu
  khối đó ("Chưa tải được …" + Thử lại), có số rồi thì làm mờ chứ không nháy skeleton. Hai khối `@defer` nhận `filter`; trang tạo object
  `statsFilter` mới mỗi lần đổi lọc / Làm mới nên khối tự tải lại, không cần `reloadToken`.
- Dự án không QA: KPI 3 cột, biểu đồ chỉ đường Đã kiểm, ẩn 3 khối QA và không gọi API của chúng; khối Năng suất vẫn có, không còn chữ "QA" trên màn.
- Thiếu `ProgressStats` / `QaStats` → khối giữ khung, ghi "Bạn chưa được cấp quyền xem mục này", không dựng component nên không gọi API.
- Nghiệm thu: luật nút / chặn / dialog lý do / PDF chuyển nguyên từ màn chi tiết cũ (`a` = tổng − đã kiểm, `b` = đã kiểm − QA đạt, khớp
  `340`); chuyển xong khối phát `changed`, trang tải lại dự án + acceptance. Câu "Tự vào nghiệm thu khi …" chỉ hiện ở bước Đang triển khai.
- Màu dữ liệu khai một lần ở `REPORT_COLORS`; số định dạng `Intl` vi-VN (`formatNumber`, `formatPercent`), không đăng ký locale `vi`.
- Lưu ý: chart.js vẽ canvas nên đọc token bằng `readCssVar(element, name)` trên phần tử của khối; đừng đọc `document.documentElement`
  — token `--app-*` không ở `:root`, ra chuỗi rỗng và chart tô đen.

## Khối Năng suất (Task 71)

`report-productivity/` (`report-productivity.component.*`, `productivity-columns.ts`) thay `report-entry-users` cũ (chỉ đếm văn bản đã QA chấm, ẩn ở dự án không QA).
BE: [31-nang-suat-theo-nguoi.md](../../be/nghiep-vu/31-nang-suat-theo-nguoi.md). Admin / Leader kiểm hộ, chấm hộ được tính cho người đang giữ văn bản, không có dòng riêng.

- Hàng riêng ngay dưới "Trường hay sai nhất" (hàng này còn một khối, `report-row--single`); chỉ cần `ProgressStats`.
- **Tab:** dự án có QA (theo `isQaEnabled` của response) có 2 tab "Nhập liệu" / "QA", số trong tab = số dòng mảng, mặc định Nhập liệu, đổi tab không gọi lại
  API; không QA thì không tab. Câu phụ nói rõ Admin / Leader kiểm hộ tính cho người giữ văn bản.
- **Cột:** Nhập liệu = Người nhập · Văn bản đã kiểm · Ô có dữ liệu · Ngày làm · TB / ngày (`documentCount ÷ workingDays`, 1 số lẻ, ngày làm 0 → "—"),
  thêm Đạt lần đầu · Độ chính xác ô khi có QA và `QaStats`. QA = Người chấm · Lượt chấm · Văn bản · Đạt · Không đạt · Ngày làm · TB lượt / ngày.
- **Ô tên:** avatar chữ cái; `isRemoved` → chip "Đã gỡ"; `userId = null` → "Không gắn người" avatar "?"; user xoá cứng (tên null) → "—". Dòng toàn 0 chữ xám.
  Giữ thứ tự BE trả.
- **Ghép cột chất lượng:** một lần tải gọi song song `progress-stats/users` và `qa-stats/entry-users` (`pageSize` 200, `orderBy` review, cùng filter) bằng `forkJoin` trong
  một hàm request của `bindReportBlock`; ghép `firstPassRate` / `fieldAccuracyRate` theo `userId`, không có → "—". API chất lượng lỗi → 2 cột "—", bảng vẫn hiện,
  không toast (`qaStatsEntryUsers(query, skipErrorToast = true)` gắn `SKIP_ERROR_TOAST`). Không `QaStats` hoặc không QA → ẩn 2 cột và không gọi.
- **Top 5 + Xem tất cả:** bảng hiện 5 dòng đầu của tab; "Xem tất cả (n)" (n = số dòng tab, hiện cả khi ≤ 5 theo thiết kế) mở dialog "Năng suất · Nhập liệu" / "· QA"
  phân trang ở FE 10 / 25 / 50, không gọi lại API.
- Lưu ý: dòng xám phải chọn đủ cấp `:host ::ng-deep .p-datatable .p-datatable-tbody > tr.productivity-row--zero > td`. `DataTable` đặt màu ô bằng selector ưu tiên cao,
  nên kiểu `.row-class > td` như màn Tiền công không ăn (chữ vẫn đen).

## Đã kiểm thực tế (Task 71.7)

2026-11-27, trình duyệt + API thật trên DB dev chung, dự án tạm `ZZTEST71` (có QA, 2 metadata) và `ZZTEST71B` (không QA), user Admin / Leader / Data / QA / Leader thiếu `QaStats`
tạm; đã dọn sạch:

- Admin, Leader vào được; Data, QA bị toast "Chỉ quản lý dự án xem được báo cáo tiến độ" rồi về danh sách. Đổi metadata / 7 / 30 / Toàn bộ ghi lên URL, F5 giữ.
- KPI khớp `acceptance` (9 văn bản, 8 đã kiểm, 2 QA đạt, 5 chờ QA); biểu đồ theo ngày khớp `progress-stats/daily`; thẻ nghiệm thu đúng [22](../../be/nghiep-vu/22-nghiem-thu-du-an.md) (không bấm Hoàn tất).
- Năng suất: tab "Nhập liệu 18" / "QA 1", đổi tab không có request mới; Đạt lần đầu / Độ chính xác khớp `qa-stats/entry-users`; Leader kiểm hộ văn bản của d1 → Làm mới d1 tăng 1,
  Leader không có dòng; "Không gắn người" cuối bảng; thành viên đã gỡ có số hiện chip "Đã gỡ"; thành viên chưa làm dòng 0 xám, TB / ngày "—"; dialog đủ 18 dòng, 10 / 25 / 50 không gọi API.
- Tải lại làm mờ bảng, không skeleton; chặn riêng API năng suất → "Chưa tải được năng suất" + Thử lại chỉ trong khối, các khối khác nguyên. Dự án không QA: không tab, 5 cột, không gọi `qa-stats`.
  Thiếu `QaStats`: 5 cột, không gọi `qa-stats/entry-users`, hai khối kia hiện "chưa được cấp quyền".
- Chưa kiểm: tắt mạng thật bằng DevTools Offline (đã giả lập bằng chặn đúng API đó).

> **Tiếp:** [16-man-han-du-an.md](16-man-han-du-an.md) — màn Hạn dự án.
