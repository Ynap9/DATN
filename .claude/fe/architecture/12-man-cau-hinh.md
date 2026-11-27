# Màn Cấu hình

> **Phần 12/13** · trước: [11-man-nhap-xuat.md](11-man-nhap-xuat.md) · mục lục: [README.md](README.md)

Task 50 (cập nhật 2026-11-24). Sửa các dòng của bảng `ProjectConfig` (dùng cho mọi dự án): kết nối Google, ứng dụng
Google, hạn dự án. Nghiệp vụ BE ở [../../be/nghiep-vu/08-cau-hinh-du-an.md](../../be/nghiep-vu/08-cau-hinh-du-an.md).

## Route và file

| Thứ | Chỗ |
|---|---|
| Route | `/admin/construction/project-config`, `permissionGuard` = `Menu…ProjectConfig` **hoặc** `Function…ProjectConfig.View`, `canDeactivate` inline trong `construction.routes.ts` |
| Menu | "Cấu hình" (`pi pi-cog`) cuối nhóm Quản trị, key `MENU_PROJECT_CONFIG` |
| Màn cha | `project-config/project-config.component.*`: tải danh sách, `canDeactivate`, Kết nối Google, đọc `?google=` |
| Khối | `google-connection-section/` (chỉ hiển thị, phát `connect`), `google-app-section/` (4 ô), `deadline-section/` (báo trước + mail) |
| Lưu chung | `project-config-save.ts`: `toProjectConfigMap`, `saveProjectConfigs` (tuần tự, dừng ở lỗi đầu), `getSaveErrorMessage` |
| Hằng | `project-config.constants.ts`: `PROJECT_CONFIG_NAME`, `PROJECT_CONFIG_MASKED`, `GOOGLE_CALLBACK_PATH`, `GOOGLE_CALLBACK_RESULT`, `PROJECT_CONFIG_ERROR_CODE`, `DEADLINE_WARNING_DAYS_DEFAULT` |
| API | `ProjectConfigService`: `getList`, `create`, `update`, `getGoogleAuthorizationUrl` |

## Quyết định

- Mỗi khối chỉ gửi dòng **đã đổi**: có dòng → `PUT` (giữ `description`), chưa có → `POST`. Lưu xong (kể cả lỗi giữa
  chừng) khối phát `saved`, cha tải lại; ô đang sửa giữ nguyên, ô chưa sửa nhận giá trị mới.
- Client secret: BE che `******` **chỉ khi có giá trị** (bước 0 BE), nên rỗng = chưa lưu → ô nhập thường. Đã lưu → ô
  `••••••` chỉ đọc + "Đổi"; "Huỷ đổi" bỏ phần đã gõ. Không bấm Đổi thì không gửi secret.
- "Đã kết nối" = `GoogleRefreshToken` có giá trị (BE trả `******`). Lưu đổi Client ID / secret / Redirect URI khi đang kết
  nối → thêm toast vàng "Nên bấm Kết nối lại…".
- "Sửa lần cuối" = mốc lớn nhất `modifiedDate ?? createdDate` của 4 dòng khối Ứng dụng (BE thêm `createdDate`).
- Redirect URI thiếu `GOOGLE_CALLBACK_PATH` → chữ vàng cảnh báo, **vẫn cho lưu**.
- Báo trước: trống hợp lệ (BE dùng 3), chỉ nhận số nguyên ≥ 0. Mail bật trừ khi giá trị là `false` (không phân biệt hoa thường).
- Quyền: thiếu `Update` → mọi ô chỉ đọc, ẩn Lưu / Huỷ / Đổi / Kết nối; ô secret che cũng xám và bỏ vế "Bấm Đổi…" của
  gợi ý (không còn nút Đổi). Dòng chưa có mà thiếu `Create` → ô đó chỉ đọc.
- Kết nối: khối Ứng dụng **hoặc** Hạn dự án có sửa → hỏi "Có thay đổi chưa lưu. Rời trang để kết nối Google?" (spec
  chỉ nói khối Ứng dụng, người dùng chốt hỏi cả hai); lấy link rồi `window.location.href` (cùng tab). Thiếu cấu hình →
  BE `304`, toast câu BE.
- Quay về: `?google=success` → khung xanh; `?google=error&message=` → khung đỏ câu BE. Đọc xong xoá query bằng
  `router.navigate([], { replaceUrl: true })` để F5 không hiện lại.
- Lỗi lưu (50.10): `409` → "Cấu hình vừa được người khác tạo, kiểm tra lại rồi lưu"; lỗi khác (kể cả `304` dòng đã bị
  xoá) toast câu BE. Cả hai đều tải lại, lần Lưu sau tự đổi `POST` ↔ `PUT`.
- Rời màn trong app khi một trong hai khối có sửa → hỏi "Có thay đổi chưa lưu. Rời trang?". Không gắn `beforeunload`
  (người dùng chốt).

Bẫy: style toàn cục đặt `h2 { font-size: 2rem; margin: 1.5rem 0 1rem }` và `.pi { font-size: var(--p-icon-size) }` thắng
class Tailwind thường → tiêu đề khối dùng `m-0! text-[15px]! font-semibold!`, icon dùng `text-xl!`.

## Đã kiểm (2026-11-24, trình duyệt + DB dev, đã trả bảng về như cũ)

Ca 50.3 số 1–6, 9–12: hiện 3 khối + menu; sửa Thư mục Drive → đúng 1 `PUT`, không gửi secret, "Sửa lần cuối" đổi;
Đổi secret → DB nhận giá trị mới, màn hiện lại `••••••` + toast "Nên bấm Kết nối lại…"; Redirect URI thiếu `/v1` → cảnh
báo vàng vẫn lưu được; Client ID trống → lỗi tại ô, không gửi; Báo trước 5 + tắt mail → DB `"5"` / `"false"`; Báo trước
trống → DB `""`; chỉ View (giả lập bằng `ng.getComponent(...)._sharedService.setRoles([]) / setPermissions([...])` rồi
điều hướng lại) → chỉ đọc, không nút. Thêm: `409` (chèn ngầm dòng rồi `POST`) → toast + lần sau `PUT`; `304` (xoá mềm
ngầm rồi `PUT`) → toast câu BE; hỏi khi rời màn và khi Kết nối (cả khi chỉ khối Hạn dự án có sửa);
`?google=success|error` hiện khung + URL sạch; trạng thái Chưa kết nối.

## Chưa kiểm

- Ca 7, 8, 13: bấm Kết nối thật sang Google, Huỷ trên Google, tạo dự án sau khi kết nối (cần Gmail sở hữu thư mục;
  luồng 302 về màn đã kiểm ở Task 50A). Ca 9 mới kiểm bằng URL `?google=success` dựng tay.
- Thiếu `Create` nhưng có `Update` (chỉ đọc code).
- Ô secret trống chỉ viền đỏ, dòng "Không được để trống" hiện khi rời ô hoặc bấm Lưu.

> **Tiếp:** [13-man-chia-phieu.md](13-man-chia-phieu.md) — màn Chia phiếu: giao phiếu, phiếu của một người, chuyển / thu hồi.
