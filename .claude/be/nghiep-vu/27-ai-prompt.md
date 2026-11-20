# Prompt OCR theo metadata (module Ai)

> **Phần 27/27** · trước: [26-thu-hoi-chuyen-phieu.md](26-thu-hoi-chuyen-phieu.md) · mục lục: [README.md](README.md)

OCR bằng AI cần biết "trích cái gì, trả về dạng gì". Mỗi metadata (`DocumentType`) có **một chuỗi phiên bản prompt**: prompt chung + prompt
từng trường OCR. Có phiên bản để sửa prompt mà không ảnh hưởng lúc đang OCR và quay lại được bản cũ khi bản mới cho kết quả kém. Task 21,
2026-11-20: chỉ **lưu, quản lý phiên bản và ghép chữ** prompt — không gọi AI, không chuẩn hoá kết quả, không đụng `ProjectAiConfig` (Task 20, 22).
Hướng dẫn riêng của dự án là `ProjectAiConfig.ExtraInstruction` (Task 22). Bảng nền có từ Task 19 ([20](20-ai-nha-cung-cap.md)).

## Luật đã chốt

| # | Luật | Sai → |
|---|---|---|
| 21.1 | Prompt gắn với metadata, **không** theo dự án | — |
| 21.2 | Một bản = `SystemPrompt` (vai trò, luật chung) + `InstructionPrompt` (hướng dẫn trích xuất) + `Note` + cấu hình từng trường | — |
| 21.3 | Vòng đời `DRAFT` → `PUBLISHED` → `ARCHIVED`. Chỉ sửa / xuất bản / bỏ được bản nháp; xuất bản thì bản đang dùng cũ → `ARCHIVED` | `909` |
| 21.4 | Tối đa 1 nháp / metadata. Lưu nháp khi chưa có → tạo nháp mới: `Version` = lớn nhất **kể cả bản đã xoá** + 1, `SourceTemplateId` = bản đang dùng | — |
| 21.5 | Trường trong prompt: chưa xoá, `IsFieldOcr = true`, đúng metadata. Mỗi trường: `Description`, `Example`, `NormalizeRule` (`NONE` / `TRIM` / `DIGITS` / `NUMBER` / `DATE_DMY` / `UPPER`), `IsRequired`, `IsActive` (tạm bỏ khỏi prompt, giữ nội dung). `Order` = `OrderListView` lúc lưu | `911` / `400` |
| 21.6 | `OutputKey` = `FieldNameOcr` (trim) **lúc lưu nháp**, không nhận từ body. Rỗng, dài quá 450 hoặc trùng trường khác (không phân biệt hoa thường) | `912` |
| 21.7 | Kết quả AI ghép về trường theo `DocumentFieldId`, **không** theo `OutputKey` — đổi `FieldNameOcr` sau xuất bản không làm sai ghép (cho Task 22) | — |
| 21.8 | Màn sửa trả `missingFields` (trường OCR hiện có mà bản đang xem chưa có) và `invalidFields` (cấu hình trỏ trường đã xoá / tắt OCR / khác metadata) | — |
| 21.9 | Xuất bản cần: bản nháp, ≥ 1 trường `IsActive`, không còn `invalidFields` (lưu lại nháp để dọn). Prompt chung rỗng vẫn xuất bản được | `913` / `911` |
| 21.10 | Khôi phục bản `ARCHIVED` / `PUBLISHED` → **nháp mới** chép prompt + note + cấu hình trường còn hợp lệ, `SourceTemplateId` = bản nguồn | `910` / `909` |
| 21.11 | Ghép prompt trả 2 chuỗi tách riêng `systemPrompt` + `prompt` (khuôn ở dưới), bỏ `invalidFields` và trường `IsActive = false` | — |
| 21.12 | Bỏ nháp: chỉ bản nháp; xoá mềm bản + cấu hình trường của nó | `909` |
| 21.13 | 4 key `Menu.Ai.PromptTemplate`, `Function.Ai.PromptTemplate.View` / `.Update` / `.Publish`; mặc định chỉ Admin / SuperAdmin ([02](02-phan-quyen.md)) | `403` |
| 21.14 | Lưu nháp, xuất bản, khôi phục, bỏ nháp chạy trong transaction + khoá dòng `DocumentType` (`UPDLOCK, HOLDLOCK`), sau khoá đọc lại nháp | — |
| 21.15 | `SystemPrompt`, `InstructionPrompt` ≤ 20000; `Description` ≤ 2000, `Example` ≤ 500, `Note` ≤ 1000 (trim, rỗng → `null`) | `400` |
| 21.16 | Mọi thao tác ghi có audit: lưu nháp, xuất bản, khôi phục thành nháp, bỏ nháp (`ACTION_PROMPT_TEMPLATE_*`) | — |

**Khác spec (đã chốt 2026-11-20):**

- Spec ghi "không migration", nhưng cột `OutputKey` từ Task 19 chỉ `nvarchar(100)` trong khi `FieldNameOcr` là `nvarchar(max)` — tên OCR dài
  làm lưu nháp lỗi 500. Người dùng chọn **nới cột** lên `nvarchar(450)` (migration `Update_PromptFieldConfig_OutputKey`, `Migrations/Ai/`);
  `912` vì thế bắt thêm "dài quá 450" và trùng **không phân biệt hoa thường** (collation DB không phân biệt, index unique sẽ đụng).
- Metadata đã xoá mềm: **chặn ghi, cho đọc** — màn sửa, danh sách phiên bản, lưu nháp, xuất bản, khôi phục → `301`; xem trước và bỏ nháp vẫn được.
- Khôi phục lấy `Order` theo `OrderListView` **hiện tại** và `OutputKey` theo `FieldNameOcr` hiện tại; trường có key rỗng / quá dài / trùng
  bị **bỏ khỏi nháp, không báo lỗi** (hai trường trùng key thì bỏ cả hai — không có căn cứ chọn).
- `911` / `912` nêu tên trường bằng `UserFriendlyException(code, message)` vì shared không có hàm ghép mẫu thông điệp.
- Xuất bản: gửi `note` thì ghi đè ghi chú của nháp, không gửi thì giữ.

## API — `api/v1/ai/prompt-template`

| # | Method | Route | Quyền | Vào / ra |
|---|---|---|---|---|
| 1 | GET | `document-type/{documentTypeId:int}` | `View` | `ViewPromptEditorDto`: `published`, `draft`, `editingId` (= nháp ?? bản đang dùng; `null` = chưa có bản nào), prompt của bản đó, `fields` = **mọi trường OCR hiện có** theo `OrderListView` kèm cấu hình nếu có (`inTemplate`), `missingFields`, `invalidFields` (tên; trường đã xoá lấy tên lúc còn) |
| 2 | GET | `document-type/{documentTypeId:int}/versions` | `View` | Mọi bản chưa xoá, `Version` giảm dần, có `sourceVersion`, người tạo / xuất bản |
| 3 | PUT | `document-type/{documentTypeId:int}/draft` | `Update` | `SavePromptDraftDto` → id nháp. `fields` rỗng được (xuất bản sẽ `913`); trường bỏ khỏi body bị xoá mềm |
| 4 | POST | `{id:int}/publish` | `Publish` | `{ note? }` |
| 5 | POST | `{id:int}/restore` | `Update` | → id nháp mới |
| 6 | DELETE | `{id:int}` | `Update` | Bỏ nháp |
| 7 | GET | `{id:int}/preview` | `View` | `{ systemPrompt, prompt }` — xem trước được mọi bản, kể cả `ARCHIVED` |

Không tìm thấy bản (kể cả đã xoá) → `908`; metadata không có / đã xoá → `301`. Lỗi kiểm body trả `400` kèm câu tiếng Việt nêu tên trường.

## Khuôn ghép prompt

```text
<InstructionPrompt>                         ← rỗng thì bỏ cả dòng này lẫn dòng trống sau

Trích xuất các trường sau và trả về DUY NHẤT một JSON object, không giải thích thêm:
- "<OutputKey>" (<Tên trường>)[, bắt buộc][: <Description>][ Ví dụ: <Example>][ Định dạng: <mô tả rule>]
Trường không tìm thấy trong văn bản thì để chuỗi rỗng "".
```

Mô tả rule: `TRIM` "bỏ khoảng trắng thừa", `DIGITS` "chỉ gồm chữ số", `NUMBER` "số, không dấu phân cách", `DATE_DMY` "ngày dạng dd/MM/yyyy",
`UPPER` "chữ in hoa"; `NONE` không ghi. Xuống dòng `\n`, trường theo `Order`. `PromptTemplateDomainService.Compose` là hàm static thuần,
không truy cập DB — Task 22B gọi lại khi chạy OCR thật.

## Cách lưu và bẫy

- Luật trạng thái nằm ở entity `PromptTemplate` (`EnsureDraft`, `UpdateDraft`, `Publish`, `Archive`); kiểm trường, phân loại, điều kiện
  xuất bản, ghép prompt, tạo nháp / nháp khôi phục ở `PromptTemplateDomainService`. Service chỉ điều phối + transaction + audit.
- Đọc trường của metadata chỉ qua `IDocumentTypeRepository.FindByIdAsync`, `IDocumentFieldRepository.GetByDocumentTypeIdAsync` có sẵn — không
  sửa code Construction. Query đọc (`PromptTemplateQuery`) được join thẳng bảng `construction`.

⚠️ **Xuất bản hai lần `SaveChanges`.** Index `UniquePublishedIndexName` chỉ cho một bản `PUBLISHED` mỗi metadata. Gộp "hạ bản cũ" và "nâng
nháp" vào một lần lưu thì EF có thể gửi `UPDATE` bản mới trước và đụng index. Đừng gộp lại thành một `SaveChanges`.

⚠️ **Lưu nháp: xoá cấu hình bị bỏ, `SaveChanges`, rồi mới sửa / thêm.** Unique `(PromptTemplateId, OutputKey)` và `(PromptTemplateId,
DocumentFieldId)` chỉ bỏ qua dòng `Deleted = 1` khi việc xoá đã xuống DB. Giới hạn còn lại: hai trường **đã cấu hình** đổi `FieldNameOcr`
chéo nhau (A↔B) rồi lưu một lần thì hai `UPDATE` trong cùng lần lưu vẫn có thể đụng index → lỗi 500. Hiếm (phải đổi tên OCR của metadata
giữa hai lần lưu); gặp thì bỏ một trường khỏi nháp, lưu, rồi thêm lại.

⚠️ Tạo nháp mới phải `SaveChanges` trước khi dựng cấu hình trường (cần `Id` thật của nháp). `AddAuditLog` serialize ngay lúc gọi nên luôn gọi
sau lần lưu lấy `Id`.

## Mã lỗi

| Mã | Hằng | Khi |
|---|---|---|
| `908` | `AiErrorPromptTemplateNotFound` | Không có / đã xoá |
| `909` | `AiErrorPromptTemplateNotDraft` | Lưu / xuất bản / bỏ bản không phải nháp; khôi phục từ bản nháp; nháp đã bị request khác xuất bản / bỏ |
| `910` | `AiErrorPromptTemplateDraftExists` | Khôi phục khi đang có nháp |
| `911` | `AiErrorPromptFieldInvalid` | Trường không thuộc metadata / đã xoá / tắt OCR (lưu nháp, xuất bản) |
| `912` | `AiErrorPromptOutputKeyInvalid` | `FieldNameOcr` rỗng, dài quá 450 hoặc trùng |
| `913` | `AiErrorPromptNoActiveField` | Xuất bản khi không có trường `IsActive` |

## Code

| Tầng | File |
|---|---|
| Shared | `Constants/Ai/PromptTemplateConst` (`NORMALIZE_RULES`, `PROMPT_MAX_LENGTH`, `OUTPUT_KEY_MAX_LENGTH` = 450), `ErrorCodes` 908–913, `PermissionConstants`, `AuditLogConstants` |
| Domain | `Module/Ai/Prompts/Entities/*`; `Repositories/IPromptTemplateRepository`, `IPromptFieldConfigRepository`; `Services/PromptTemplateDomainService` |
| Applications | `Module/Ai/Dtos/PromptTemplate/*` (8 DTO), `IPromptTemplateQuery`, `IPromptTemplateService` / `PromptTemplateService` |
| Data | `Module/Ai/Repositories/PromptTemplateRepository`, `PromptFieldConfigRepository`, `Queries/PromptTemplateQuery`; `Migrations/Ai/Update_PromptFieldConfig_OutputKey` |
| Api | `Controllers/Module/Ai/PromptTemplateController` |

## Đã kiểm thực tế (2026-11-20)

Gọi thật trên DB dev (API bản Release, cổng riêng), metadata tạm `ZZTEST21` 4 trường (3 bật OCR, 1 không) + một metadata phụ, user tạm
Leader / Data / QA; dọn sạch sau test (giữ audit): **53/53 đạt** — đủ các ca mục 21.5 của spec: editor rỗng; lưu lần 1 / lần 2 cùng id,
2 PUT song song ra đúng 1 nháp; `911` (không OCR / khác metadata / đã xoá), `912` (rỗng / trùng khác hoa thường / > 450), `400`; `913`;
v2 xuất bản → v1 `ARCHIVED`, đúng 1 `PUBLISHED`; tắt OCR + thêm trường → cảnh báo, preview bỏ trường, xuất bản `911` rồi lưu lại được;
đổi `FieldNameOcr` → bản cũ giữ key cũ; khôi phục `910` / `909`, bỏ nháp rồi khôi phục ra `Version` 5 (không dùng lại 4); preview đúng
từng ký tự; Leader / Data / QA `403` cả 7 API; `908` / `301`; metadata đã xoá chặn ghi cho đọc; 4 hành động audit.
Bổ sung cùng ngày **6/6 đạt**: user tạm **chỉ role Admin** (không SuperAdmin) gọi được cả 7 API; bỏ nháp rồi lưu nháp mới bằng PUT ra `Version` mới.

⚠️ User `admin` của DB dev có cả role `SuperAdmin` (bỏ qua kiểm quyền) — test quyền phải dùng user chỉ có role cần kiểm. DB dev dùng chung:
máy chạy code chưa có 4 key prompt khởi động là `SeedPermission` xoá key + quyền đã gán (gặp lúc test, [02](02-phan-quyen.md#đừng-đổi-tên-key-đã-seed)).

> **Tiếp:** hết — quay lại [README.md](README.md).
