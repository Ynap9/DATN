# Văn bản đang làm gần nhất và lọc trường trống

> **Phần 28/28** · trước: [27-ai-prompt.md](27-ai-prompt.md) · mục lục: [README.md](README.md)

Task 42A (2026-11-21) thêm hai thứ cho màn danh sách văn bản FE (Task 42). Thứ nhất là dòng "Tiếp tục văn bản đang làm" để người
dùng quay lại đúng văn bản vừa lưu / vừa chấm. Thứ hai là lọc "(Trống)" để tìm văn bản còn thiếu dữ liệu ở một trường. Bộ lọc chung
và phạm vi xem nằm ở [12-ho-so-kiem-tra.md](12-ho-so-kiem-tra.md).

## Văn bản đang làm gần nhất

| Endpoint | Quyền | Việc |
|---|---|---|
| `GET api/construction/submission/latest-working?projectId=&documentTypeId=&type=` | `Function.Construction.Submission.View` | Văn bản người gọi làm gần nhất trong cặp dự án + loại văn bản; chưa có thì `data = null` |

- `type`: `CHECK` (lần lưu kiểm tra gần nhất, đọc `SubmissionLog`) hoặc `QA` (lần chấm QA gần nhất, đọc `SubmissionQaLog`). Trim, không
  phân biệt hoa thường. Giá trị khác / rỗng / thiếu → `400` "type chỉ nhận CHECK hoặc QA" trong envelope (`SubmissionConst.LATEST_WORKING_TYPES`).
- Chỉ tính log **của chính người gọi** (`UserId` = token), kể cả Admin / Leader: đây là "việc của tôi", không phải "việc mới nhất trong cặp".
- Lấy văn bản **còn mở được**: văn bản đã xoá mềm bị bỏ qua. Người bị giới hạn (`ResolveRestrictedUserIdAsync` khác `null`) chỉ nhận
  văn bản mình còn giữ **đúng vai**: `Data` với `CHECK`, `QA` với `QA`. Văn bản gần nhất không còn hợp lệ thì lấy văn bản kế tiếp,
  vì bộ lọc chạy **trước** khi sắp xếp (một câu SQL, `ORDER BY CreatedDate DESC, Id DESC`).
- Kiểm cặp như mọi API đọc (`303` / `301` / `312`); người ngoài cặp → `820`.
- Kết quả: `submissionId`, `name`, `fileName`, `profileRange`, `box`, `statusCheck`, `statusQa`, `workedDate` (giờ của log).
- Dự án tắt QA mà gửi `type=QA`: không chặn, trả theo log như thường. FE không hiện lựa chọn QA cho dự án này (chốt 2026-11-21).

## Lọc trường trống

Bộ lọc chung thêm `isEmpty1` / `isEmpty2` (`bool?`) đi cặp với `documentFieldId1` / `documentFieldId2`.

- `isEmpty = true`: văn bản **không có** dòng `SubmissionData` của trường đó mà dữ liệu khác rỗng sau `Trim()`. Không có dòng nào, hoặc
  dữ liệu chỉ toàn dấu cách, đều tính là trống.
- `false` hoặc không gửi: như cũ, lọc theo `fieldValue` nếu có.
- Kiểm trước khi lọc (`EnsureFieldFilterValidAsync`, gọi ngay sau kiểm cặp ở cả 11 API dùng bộ lọc): thiếu `documentFieldIdN` → `400`; gửi kèm
  `fieldValueN` khác rỗng → `400`; trường không thuộc cấu hình dự án + loại văn bản → `311`. Hai luật `400` xét hết rồi mới tới `311`.
- 11 API: `paging`, `{id}`, `{id}/next`, `{id}/previous`, `count`, `qa/paging`, `{id}/qa`, `field-values`, `tree`, `export-excel`,
  `export-excel/job`. Job xuất nhận nguyên `ExportSubmissionDto` (kế thừa `SubmissionFilterDto`) nên lọc trống đi theo vào Hangfire.
- `field-values` vẫn bỏ cặp lọc của chính trường đang lấy gợi ý (lọc liên hoàn như [12](12-ho-so-kiem-tra.md#tra-cứu-và-thống-kê)).

⚠️ `311` cho trường lạ được thêm lúc tự review: bản đầu bỏ qua bước này nên `isEmpty=true` với trường ngoài cấu hình (id sai, trường của loại khác,
hoặc trường thêm sau trước khi sửa #15 ở [06](06-truong-van-ban.md#luật-nghiệp-vụ)) trả **mọi** văn bản, vì không văn bản nào có dữ liệu của trường đó. Lọc theo giá trị với trường lạ
thì ra 0 dòng nên không cần chặn. Đừng bỏ lời gọi `EnsureDocumentFieldInProjectAsync` để tiết kiệm một lượt DB.

⚠️ `Trim()` dịch ra `LTRIM(RTRIM())` của SQL Server, chỉ cắt dấu cách: ô chỉ có tab / xuống dòng vẫn tính là **có** dữ liệu (chốt giữ đúng spec).

## Đã kiểm thực tế

2026-11-21, API thật trên DB dev chung (cổng 5342, `BackgroundJob__Enabled=false`), dữ liệu gắn `ZZTEST42A` (1 dự án, 7 văn bản, 2 trường,
Data A / B, QA Q, người ngoài Z), kiểm xong đã dọn về 0:

- 27/27 lần đầu: null khi chưa làm; Check rồi tái kiểm đổi văn bản; đổi người giữ / Admin xoá văn bản thì lùi về văn bản trước hoặc null; QA chấm
  Không đạt; QA gọi `CHECK` → null; Admin Check thì nhận văn bản của mình; `type` sai / rỗng / thiếu → `400`, `' check '` chạy như `CHECK`;
  `820`, `312`; dự án đang nghiệm thu vẫn trả. Lọc trống: `paging`, `count`, `{id}` + `next` / `previous`, `qa/paging`, `export-excel`,
  `export-excel/job` (dừng ở `211`), `field-values`, `tree`; kết hợp với cặp giá trị; `isEmpty=false`. Chưa thử `{id}/qa` với lọc trống.
- 30/30 sau tự review: thêm trường ngoài cấu hình → `311` (paging, count) và `400` xét trước `311`.

Lệch kịch bản spec: API chuyển phiếu chỉ chuyển văn bản chưa làm (`817`) nên bước đổi người giữ làm bằng SQL; QA Không đạt bắt buộc có trường sai;
job xuất chỉ chạy tới bước kiểm (không có Hangfire server).

## Giới hạn

⚠️ `SubmissionLog` chỉ có index `(ProjectId, DocumentTypeId, CreatedDate)`, `SubmissionQaLog` chỉ có `(SubmissionId, CreatedDate)`: `latest-working`
lọc thêm `UserId` nên quét nhiều khi log lớn (phát hiện #13). DB dev nhỏ chưa thấy chậm; thêm index `(ProjectId, DocumentTypeId, UserId, CreatedDate)` cần migration, chưa chốt.

> **Tiếp:** hết — quay lại [README.md](README.md).
