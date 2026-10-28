# Nhà cung cấp AI (module Ai)

> **Phần 20/21** · trước: [19-nhom-nhan-vien.md](19-nhom-nhan-vien.md) · mục lục: [README.md](README.md)

Mọi lời gọi AI đi qua **một cửa**: model, prompt, cách chọn model là **dữ liệu** trong DB, Admin thêm model / đổi prompt không sửa code;
code chỉ biết vài giao thức gọi; dữ liệu cơ quan không gửi ra API public. Module `Ai` tách riêng (schema `ai`, route `api/v1/ai`, lỗi
`901`–`999`), người phụ trách: thành viên Thi công. Task 19, 2026-10-27: nền móng 5 bảng + 8 API nhà cung cấp; Task 20–22 làm model, prompt, cấu hình dự án.

## Năm bảng (migration `Add_AiModule`, chạy một lần)

| Bảng | Vai trò | Trạng thái Task 19 |
|---|---|---|
| `AiProvider` | Một điểm gọi: endpoint + key + giao thức + phạm vi; một nhà cung cấp chạy nhiều model | Đủ hành vi + 8 API |
| `AiModel` | Model thuộc một nhà cung cấp (`ProviderId`), `Code` duy nhất toàn hệ thống | Chỉ `Create`, `Delete` |
| `PromptTemplate` | Prompt có phiên bản theo loại văn bản; `Create` luôn `DRAFT`, đổi trạng thái để Task 21 | Chỉ `Create`, `Delete` |
| `PromptFieldConfig` | Cấu hình từng trường trong prompt (`OutputKey`, `NormalizeRule`) | Chỉ `Create`, `Delete` |
| `ProjectAiConfig` | AI của dự án + loại văn bản: model chính / dự phòng, prompt | Chỉ `Create`, `Delete` |

DB tự chặn: `(DocumentTypeId, Version)` của prompt **không** filter (số phiên bản không dùng lại, kể cả bản đã xoá); mỗi loại văn bản tối đa
một prompt `PUBLISHED` (filter `[Status] = 'PUBLISHED' AND [Deleted] = 0`). FK sang `construction` là `Restrict`, nhưng `Project`, `DocumentType`,
`DocumentField` đều xoá mềm nên FK **không** chặn xoá trên ứng dụng — chặn / bỏ qua trường đã xoá là việc của Task 21, 22.

## Luật nhà cung cấp

| # | Luật | Sai → |
|---|---|---|
| R1 | `Code` trim + chữ thường, khớp `^[a-z0-9][a-z0-9-]{1,49}$`; **không sửa** sau khi tạo; duy nhất trong nhà cung cấp chưa xoá (xoá rồi dùng lại được) | `400` / `902` |
| R2 | `Name` trim, bắt buộc, ≤ 200, được trùng | `400` |
| R3 | `Protocol` ∈ `OPENAI_COMPAT`, `ANTHROPIC`, `GEMINI`, `CUSTOM_HTTP`; đã có model chưa xoá thì không đổi được | `400` / `904` |
| R4 | `Scope` ∈ `LOCAL`, `PUBLIC`, bắt buộc chọn; đổi được, audit ghi "Đổi phạm vi LOCAL -> PUBLIC" | `400` |
| R5 | `BaseUrl` http/https tuyệt đối, không query / fragment / **`user:pass@`**, ≤ 500, lưu bỏ `/` cuối | `400` |
| R6 | `TimeoutSeconds` 5–600 (mặc định 120), `MaxConcurrency` 1–32 (mặc định 2), `RateLimitPerMinute` `null` hoặc 1–10000 | `400` |
| R7 | `ExtraHeaders` ≤ 20 cặp, tên `^[A-Za-z0-9-]{1,100}$`, giá trị trim ≤ 500 không xuống dòng, JSON ≤ 4000; trùng tên (không phân biệt hoa thường), tên cấm (`Authorization`, `Proxy-Authorization`, `X-Api-Key`, `Api-Key`, `X-Goog-Api-Key`, `Cookie`, `Host`, `Content-Type`, `Content-Length`) hoặc trùng `ApiKeyHeaderName` | `400` |
| R8 | API key: đặt / thay / xoá qua `PUT {id}/api-key`; rỗng / `null` / toàn khoảng trắng = xoá; có giá trị thì trim, 1–1000 ký tự, không khoảng trắng | `400` |
| R9 | Tắt (`IsActive` true → false) bị chặn khi có `ProjectAiConfig` chưa xoá dùng model chưa xoá của nhà cung cấp làm model chính / dự phòng | `905` |
| R10 | Xoá mềm; còn model chưa xoá → chặn; xoá thì xoá luôn key (`ApiKeyEncrypted`, `ApiKeyHash` = `null`) | `903` |
| R11 | Quyền chỉ Admin / SuperAdmin có sẵn: 5 key `Function.Ai.Provider.*`; Leader / Data / QA không có | `403` |
| R12 | `OPENAI_COMPAT` → `Authorization: Bearer {key}`, `ANTHROPIC` → `x-api-key`, `GEMINI` → `x-goog-api-key`: 3 giao thức này `ApiKeyHeaderName`, `ApiKeyHeaderPrefix` phải `null`. `CUSTOM_HTTP`: tên header tuỳ chọn (được dùng tên cấm ở R7), prefix ≤ 20 chỉ khi có tên header, **không** trim (giữ `"Bearer "`); tên `null` = API không cần key | `400` |

Luật R1–R6, R12 nằm trong `AiProvider.Create` / `Update`; R7 ở `AiProviderHeaderPolicy`; R8 ở `AiProviderApiKeyPolicy`; R3, R9, R10, mã trùng
ở `AiProviderDomainService`. Lỗi định dạng trả `ApiResponse` mã `400` kèm thông điệp tiếng Việt cụ thể.

## Bảo mật API key

- DB giữ `ApiKeyEncrypted` (DataProtection, purpose `sh.be.ai.provider-api-key` — gateway cần key gốc để gọi AI) và `ApiKeyHash`
  (SHA-256 hex chữ thường — để nhận diện). Không lưu key chữ thường ở đâu; không chỉ lưu hash vì hash một chiều thì không gọi được AI.
- API đọc (paging, dropdown, chi tiết) chỉ trả `hasApiKey` + `apiKeyFingerprint` (8 ký tự đầu của hash).
- Key gốc chỉ ra khỏi server qua `GET {id}/api-key/download`: file `{code}-api-key-{yyyyMMddHHmmss}.json`, `Cache-Control: no-store`, 11 trường
  `type`, `provider_code`, `provider_name`, `protocol`, `base_url`, `api_key_header_name`, `api_key_header_prefix`, `api_key`, `api_key_fingerprint`,
  `exported_by` (userId), `exported_at` (giờ VN, ISO 8601 `+07:00`). Mỗi lần tải ghi audit `Export` với `{ Id, Code, ApiKeyFingerprint }`.
- Key không vào log, audit, exception. API đặt / tải key log `id=..., hasApiKey=...` thay vì log dto. Header thêm **không** phải nơi chứa
  bí mật: trả ra API và ghi audit dạng chữ thường.

**Khác spec (đã chốt 2026-10-27):** tải key chỉ kiểm quyền `Function.Ai.Provider.DownloadApiKey`, **không** kiểm thêm vai trò Admin trong service
(người dùng: Admin hiếm khi giao quyền này cho role khác) — nên không có `PermissionGrantPolicy.IsAdmin`. BaseUrl chặn thêm `user:pass@`.

⚠️ Khoá DataProtection phải lưu ra volume (`DataProtection:KeysPath`, [../architecture/05-infrastructure-external.md](../architecture/05-infrastructure-external.md)):
mất khoá thì mọi key đã lưu thành rác, tải key báo `907`, Admin phải nhập lại. Đừng "sửa" bằng cách lưu key chữ thường.

## API — `api/v1/ai/provider`

| # | Method | Route | Quyền | Vào / ra |
|---|---|---|---|---|
| 1 | GET | `paging` | `View` | `keyword` (Code / Name / BaseUrl), `protocol`, `scope`, `isActive`, `pageSize` 1–200 → `GetListAiProviderDto` (+ `modelCount`), sắp `CreatedDate`, `Id` giảm dần |
| 2 | GET | `dropdown` | `View` | Chỉ nhà cung cấp đang bật, sắp `Name` → `Id`, `Code`, `Name`, `Protocol`, `Scope`, `HasApiKey` |
| 3 | GET | `{id:int}` | `View` | `FindAiProviderByIdDto`: như paging + header key, timeout, đồng thời, rate limit, `extraHeaders` (object), người tạo / sửa |
| 4 | POST | (gốc) | `Create` | `CreateAiProviderDto` (không có key) → `id` mới, để FE gọi tiếp API 6 |
| 5 | PUT | `{id:int}` | `Update` | `UpdateAiProviderDto` (không có `Code`; gửi kèm thì bị bỏ qua) |
| 6 | PUT | `{id:int}/api-key` | `Update` | `{ apiKey }` → `{ hasApiKey, apiKeyFingerprint }` |
| 7 | DELETE | `{id:int}` | `Delete` | Xoá mềm |
| 8 | GET | `{id:int}/api-key/download` | `DownloadApiKey` | File JSON; lỗi vẫn trả `ApiResponse` |

Không tìm thấy (kể cả đã xoá) → `901`. Mọi thao tác ghi có audit `ACTION_AI_PROVIDER_*`, snapshot không có `ApiKeyEncrypted` / `ApiKeyHash`.
Sửa / xoá / đặt key / tải key: một `SaveChanges`, không mở transaction tay. Tạo mới mở transaction, `SaveChanges` hai lần (lấy `Id` thật
rồi mới dựng audit) — ⚠️ `AddAuditLog` serialize `newData` ngay lúc gọi, gọi trước `SaveChanges` thì audit ghi `Id = 0`.

## Mã lỗi

| Mã | Hằng | Khi |
|---|---|---|
| `901` | `AiErrorProviderNotFound` | Không có / đã xoá |
| `902` | `AiErrorProviderCodeExists` | Trùng mã sau chuẩn hoá |
| `903` | `AiErrorProviderHasModels` | Xoá khi còn model |
| `904` | `AiErrorProviderProtocolLocked` | Đổi giao thức khi đã có model |
| `905` | `AiErrorProviderInUse` | Tắt khi dự án đang dùng |
| `906` | `AiErrorProviderApiKeyMissing` | Tải key khi chưa có key (`AiProvider.GetApiKeyEncrypted`) |
| `907` | `AiErrorProviderApiKeyUnreadable` | Giải mã lỗi (khoá DataProtection đã đổi, dữ liệu rác) |

Task 20–22 thêm tiếp từ `908`.

## Code

| Tầng | File |
|---|---|
| Shared | `Constants/Ai/AiProviderConst`, `AiModelConst`, `PromptTemplateConst`, `ProjectAiConfigConst`; `DbSchemas.Ai`, `SwaggerConstants.AI` |
| Domain | `Module/Ai/{Providers,Models,Prompts,ProjectConfigs}/Entities/*`; `Providers/Services/AiProviderHeaderPolicy`, `AiProviderApiKeyPolicy`, `AiProviderDomainService`; `Providers/Repositories/IAiProviderRepository` |
| Applications | `Module/Ai/Dtos/AiProvider/*`, `IAiProviderQuery`, `IAiProviderService` / `AiProviderService`; cổng ra `Base/Interfaces/External/Ai/ISecretProtectorExternal` |
| Data | `Module/Ai/Repositories/AiProviderRepository`, `Queries/AiProviderQuery`; `Migrations/Ai/Add_AiModule` |
| External | `Ai/Implements/DataProtectionSecretProtectorExternal` (singleton) |
| Api | `Controllers/Module/Ai/AiProviderController`; Swagger nhóm `ai` |

## Đã kiểm thực tế (2026-10-27)

Gọi thật trên DB dev bằng `admin` và một user tạm role `Data`, dữ liệu mang tiền tố `zztest`, xoá sạch sau khi test (kể cả audit, user):
**69/69 đạt** — đủ các ca mục 19.8 của task: xoá mềm rồi tạo lại cùng mã; DB chặn 2 prompt `PUBLISHED` và trùng phiên bản; R1–R8, R12 (code `GPU Local`,
`a`, `-abc` → `400`, `GPU-LOCAL` → `902`; URL `ftp`, query, fragment, `user:pass@` → `400`; 21 header, header cấm, trùng hoa thường → `400`); chèn tay model
→ `904` / `903`, chèn tay `ProjectAiConfig` → `905`, sửa tên vẫn được; đặt key → hash đúng SHA-256, cột mã hoá khác key; paging / dropdown / chi tiết
không lộ key hay hash đầy đủ; tải key đủ 11 trường, `no-store`, 1 dòng audit; chuỗi rác → `907`; chưa có key → `906`; log NLog + `AuditLog` không có key;
restart app cùng `KeysPath` vẫn giải mã đúng; Data → `403`, không token → `401`; Swagger `v1-ai` đủ 8 route.
Chưa kiểm: xác thực lại Google sau khi bật `KeysPath` (cần đăng nhập Google tay).

⚠️ `IDataProtector.Unprotect` gặp chuỗi không phải Base64Url ném `FormatException`, **không** phải `CryptographicException` — `TryUnprotect` bắt cả hai.

> **Tiếp:** [21-thanh-vien-du-an.md](21-thanh-vien-du-an.md) — thành viên dự án: Admin / Leader phân người vào (dự án, loại văn bản), gỡ tự thu hồi văn bản.
