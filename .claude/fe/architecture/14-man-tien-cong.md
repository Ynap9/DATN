# Màn Tiền công (Operation)

> **Phần 14/14** · trước: [13-man-chia-phieu.md](13-man-chia-phieu.md) · mục lục: [README.md](README.md)

Task 59, 2026-11-26. Chỉ đọc: khối lượng, đơn giá, thành tiền theo người. Công thức và phạm vi xem do BE tính, xem
[24-don-gia-tien-cong.md](../../be/nghiep-vu/24-don-gia-tien-cong.md). FE **không tự tính tiền**; chỉ cộng dòng cho dải tổng của nhân viên.

| Thứ | Chỗ |
|---|---|
| Route | `/admin/operation/wage?projectId=&documentTypeId=&role=&keyword=&pageNumber=&pageSize=`, `permissionGuard` [`FUNCTION_WAGE_VIEW`]; menu "Tiền công" sau "Chia phiếu" |
| Màn | `features/operation/wage/wage-list.component.*`; dải tổng `wage-summary/`; ô bảng `cells/` (người, khối lượng, Đạt / Không đạt, đơn giá, thành tiền, cách tính) |
| Service / model | `services/wage.service.ts` (`findPaging`, `exportExcelUrl`), `models/wage.model.ts` |
| Hằng | `shared/constants/wage.constants.ts` (chế độ xem, tab vai trò, đơn vị, trang 25 / [10, 25, 50, 100]) |

## Chế độ xem

| Chế độ | Khi nào | Khác biệt |
|---|---|---|
| `admin` | Admin / SuperAdmin | Chip "Toàn bộ thành viên", nút Xuất Excel, nhãn "Chưa đặt giá" là link sang Thành viên dự án đúng cặp |
| `leader` | Metadata đang chọn (hoặc một cặp bất kỳ khi "Tất cả") có `viewerRole` Leader | Chip "Người bạn quản lý" |
| `staff` | Còn lại | Tiêu đề "Tiền công của bạn", 3 ô tổng, không tab / tìm kiếm, cột Cách tính thay cột Người |

- Chế độ theo `viewerRole` của cặp (`project/paging`), **không** theo dữ liệu trả về. Nhân viên lấy vai trò Data / QA theo cặp; CASE
  chốt không có người vừa Data vừa QA trong một dự án nên "Tất cả" dùng QA chỉ khi mọi cặp là QA.
- Dự án = dự án có ít nhất một cặp mình có vai trò, sắp theo tên `vi`. Không có → "Bạn chưa tham gia dự án nào", ẩn bộ lọc.

## Bộ lọc, bảng, tổng

- Lọc nằm trên URL (`replaceUrl`, giá trị mặc định không ghi). Thiếu / sai `projectId` → dự án đầu; đổi dự án → metadata "Tất cả",
  trang 1. Tìm kiếm debounce 400ms. Tải bằng `Subject` + `switchMap`: đổi lọc nhanh thì request cũ bị huỷ.
- Cột dựng lại mỗi lần đổi lọc / tải. Admin / Leader: nhóm `Thực nhận` và `Giá công ty` (bỏ cả nhóm khi `summary.companyAmount`
  null); tab Nhập liệu ẩn cột Đạt / Không đạt. Dòng `isPriceMissing` có vạch vàng (`rowClass`).
- Ô đơn giá: `undefined` → "—" (người xem không thấy giá công ty, FE gán khi `companyAmount` null), `null` → nhãn vàng "Chưa đặt giá".
- Ô tổng Admin / Leader lấy `summary` (cả bộ lọc). Nhân viên: tiền / khối lượng lấy `summary`; văn bản, Đạt / Không đạt, số metadata
  chưa giá cộng từ dòng, nên khi `totalItems` > số dòng của trang thì hiện "—" + tooltip "Xem đủ các trang để có tổng".
- Xuất Excel: `DownloadService.get(exportExcelUrl(lọc hiện tại))`, lỗi BE nằm trong blob → `readErrorResponse` → toast.
  ⚠️ BE trả lỗi nghiệp vụ bằng `OkException` = **HTTP 200** + JSON; giả lập lỗi khi test phải dùng 200, không phải 400.
- Người đã gỡ / đang tắt: nhãn viền nhỏ cạnh tên + cả dòng chữ màu phụ (`rowClass` thêm `wage-row-inactive`).
- Đang tải: skeleton chỉ ở dải tổng, bảng giữ vòng xoay của PrimeNG.

## Lệch so với spec / thiết kế

- Thêm input `rowClass` và cột `group` cho `DataTable` ([05-conventions.md](05-conventions.md)); không khai thì bảng như cũ.
- Giữ chân bảng "Tất cả N dòng" của `DataTable` (thiết kế "Hiển thị 1–25 / 58 dòng"); bảng giữ vòng xoay, không skeleton.
- Nhãn vai trò và "Chưa đặt giá" dùng `StatusBadge` (viên thuốc có chấm) thay ô vuông của thiết kế; màu vai trò theo CASE
  (Nhập liệu xanh lục, QA xanh dương). Không dùng font Space Grotesk của bản thiết kế.
- Bỏ dòng "Đặt giá sau thì tiền được tính lại…" ở trạng thái 4 của thiết kế: không có trong CASE 59.14.

## Đã kiểm

2026-11-26, Chrome headless, dự án test `ZZTEST59` (2 metadata, 2 Leader, 3 Data, 2 QA, một người đã gỡ, hai dòng chưa giá; đã xoá):
24/24 ca bằng Admin — tổng 20.600 / 38.000 / 45 trường / 4 lượt khớp API, hàng nhóm, vạch vàng, link đặt giá, tab, tìm kiếm + trống,
metadata, URL giữ sau reload, Xuất Excel tải file, màn Nhóm nhân viên vẫn một hàng tiêu đề. 17/17 ca theo vai: Leader (hai Leader
khác phạm vi), Data, QA, người không có dự án; ô "—" + tooltip giả lập `totalItems` lớn bằng chặn response. Bổ sung 15/15 cho đủ
14 ca bảng 59.2: tab QA có thanh, QA chưa giá (Admin và chính QA xem, 0 đ đúng màu `--app-faint`), file Excel đọc lại chỉ có dòng
đúng bộ lọc + Tổng, đổi dự án bằng ô chọn, F5 ở trang 2 và khi có từ khoá, header 8 màn khác vẫn một hàng, Xuất Excel lỗi → toast.
Sau khi chỉnh giao diện theo thiết kế chạy lại cả ba bộ: 24/24, 17/17, 16/16. Unit test `DataTable` 20/20.

⚠️ DB dev lúc test (2026-11-26) chưa có `Function.Operation.Wage.View` cho Leader / Data / QA nên họ không thấy menu. Cùng ngày đã bổ sung
cho ba vai trò (Task 57A, [BE 03](../../be/nghiep-vu/03-vai-tro-mac-dinh.md)); DB mới do seeder cấp.

> **Tiếp:** hết bộ tài liệu — quay lại [README.md](README.md).
