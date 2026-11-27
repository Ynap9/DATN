using sh.be.domain.Module.Ai.Prompts.Entities;
using sh.be.domain.Module.Ai.Prompts.Repositories;
using sh.be.domain.Module.Construction.DocumentFields.Entities;
using sh.be.domain.Module.Construction.DocumentTypes.Repositories;
using sh.be.shared.Constants.Ai;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Ai.Prompts.Services
{
    public class PromptTemplateDomainService
    {
        private static readonly Dictionary<string, string> NormalizeRuleDescriptions = new()
        {
            { PromptTemplateConst.NORMALIZE_TRIM, "bỏ khoảng trắng thừa" },
            { PromptTemplateConst.NORMALIZE_DIGITS, "chỉ gồm chữ số" },
            { PromptTemplateConst.NORMALIZE_NUMBER, "số, không dấu phân cách" },
            { PromptTemplateConst.NORMALIZE_DATE_DMY, "ngày dạng dd/MM/yyyy" },
            { PromptTemplateConst.NORMALIZE_UPPER, "chữ in hoa" },
        };

        private readonly IPromptTemplateRepository _promptTemplateRepository;
        private readonly IDocumentTypeRepository _documentTypeRepository;

        public PromptTemplateDomainService(IPromptTemplateRepository promptTemplateRepository, IDocumentTypeRepository documentTypeRepository)
        {
            _promptTemplateRepository = promptTemplateRepository;
            _documentTypeRepository = documentTypeRepository;
        }

        // Metadata phải còn (chưa xoá mềm), không thì 301
        public async Task EnsureDocumentTypeExistsAsync(int documentTypeId)
        {
            _ = await _documentTypeRepository.FindByIdAsync(documentTypeId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotFound);
        }

        // Bản nháp hiện có của metadata; chưa có thì tạo nháp rỗng từ bản đang dùng (21.4)
        public async Task<PromptTemplate> GetOrCreateDraftAsync(int documentTypeId, string? createdBy, DateTime createdDate)
        {
            var draft = await _promptTemplateRepository.FindDraftAsync(documentTypeId);
            if (draft != null)
            {
                return draft;
            }
            var published = await _promptTemplateRepository.FindPublishedAsync(documentTypeId);
            return await CreateDraftAsync(documentTypeId, published?.Id, String.Empty, String.Empty, null, createdBy, createdDate);
        }

        // Tạo nháp mới chép prompt chung của bản ARCHIVED / PUBLISHED; đang có nháp → 910 (21.10)
        public async Task<PromptTemplate> CreateRestoredDraftAsync(PromptTemplate source, string? createdBy, DateTime createdDate)
        {
            if (source.Status == PromptTemplateConst.STATUS_DRAFT)
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorPromptTemplateNotDraft);
            }
            if (await _promptTemplateRepository.FindDraftAsync(source.DocumentTypeId) != null)
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorPromptTemplateDraftExists);
            }
            return await CreateDraftAsync(source.DocumentTypeId, source.Id, source.SystemPrompt, source.InstructionPrompt, source.Note, createdBy, createdDate);
        }

        // Đọc lại bản nháp sau khi khoá metadata: bản đã bị request khác xuất bản / bỏ thì 909 (21.14)
        // Hai người cùng sửa nháp: ngày sửa cuối người gọi đang giữ phải khớp nháp hiện có (chưa có nháp thì phải null), khác → 914
        public static void EnsureDraftNotChanged(PromptTemplate? draft, DateTime? draftModifiedDate)
        {
            if (draft?.LastChangedDate != draftModifiedDate)
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorPromptDraftChanged);
            }
        }

        public async Task<PromptTemplate> EnsureDraftAsync(PromptTemplate template)
        {
            var draft = await _promptTemplateRepository.FindDraftAsync(template.DocumentTypeId);
            if (draft?.Id != template.Id)
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorPromptTemplateNotDraft);
            }
            return draft;
        }

        // Dựng cấu hình trường mới của một bản prompt từ giá trị đã kiểm (gọi sau khi bản đã có Id)
        public static List<PromptFieldConfig> CreateFieldConfigs(PromptTemplate template, List<PromptFieldConfigValue> values, string? createdBy, DateTime createdDate)
        {
            return values.Select(value => PromptFieldConfig.Create(template.Id, value.DocumentFieldId, value.OutputKey, value.Description, value.Example,
                value.NormalizeRule, value.IsRequired, value.Order, value.IsActive, createdBy, createdDate)).ToList();
        }

        // Kiểm cấu hình trường gửi lên khi lưu nháp (21.5, 21.6): chỉ trường OCR của metadata, key theo FieldNameOcr hiện tại
        public static List<PromptFieldConfigValue> BuildFieldConfigs(List<DocumentField> documentFields, List<PromptFieldInput> inputs)
        {
            if (inputs.GroupBy(input => input.DocumentFieldId).Any(group => group.Count() > 1))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Một trường chỉ được khai một lần");
            }

            var ocrFieldById = documentFields.Where(field => field.IsFieldOcr).ToDictionary(field => field.Id);
            var selectedFields = new List<DocumentField>();
            foreach (var input in inputs)
            {
                if (!ocrFieldById.TryGetValue(input.DocumentFieldId, out var field))
                {
                    var fieldName = documentFields.FirstOrDefault(documentField => documentField.Id == input.DocumentFieldId)?.Name ?? $"Id = {input.DocumentFieldId}";
                    throw new UserFriendlyException(ErrorCodes.AiErrorPromptFieldInvalid, $"Trường {fieldName} không thuộc metadata, đã xoá hoặc chưa bật OCR");
                }
                if (input.Description.Trim().Length > PromptTemplateConst.FIELD_DESCRIPTION_MAX_LENGTH
                    || input.Example?.Trim().Length > PromptTemplateConst.FIELD_EXAMPLE_MAX_LENGTH)
                {
                    throw new UserFriendlyException(ErrorCodes.BadRequest,
                        $"Trường {field.Name}: mô tả tối đa {PromptTemplateConst.FIELD_DESCRIPTION_MAX_LENGTH}, ví dụ tối đa {PromptTemplateConst.FIELD_EXAMPLE_MAX_LENGTH} ký tự");
                }
                if (!PromptTemplateConst.NORMALIZE_RULES.Contains(input.NormalizeRule))
                {
                    throw new UserFriendlyException(ErrorCodes.BadRequest, $"Trường {field.Name}: quy tắc chuẩn hoá không hợp lệ");
                }
                selectedFields.Add(field);
            }

            var invalidKeyFieldIds = FindInvalidOutputKeyFieldIds(selectedFields);
            var invalidKeyField = selectedFields.FirstOrDefault(field => invalidKeyFieldIds.Contains(field.Id));
            if (invalidKeyField != null)
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorPromptOutputKeyInvalid,
                    $"Trường {invalidKeyField.Name} chưa có tên trường OCR, tên dài quá {PromptTemplateConst.OUTPUT_KEY_MAX_LENGTH} ký tự hoặc trùng tên trường OCR với trường khác");
            }

            return inputs.Select(input =>
            {
                var field = ocrFieldById[input.DocumentFieldId];
                var example = input.Example?.Trim();
                return new PromptFieldConfigValue(field.Id, field.FieldNameOcr!.Trim(), input.Description.Trim(), String.IsNullOrEmpty(example) ? null : example,
                    input.NormalizeRule, input.IsRequired, field.OrderListView, input.IsActive);
            }).ToList();
        }

        // Cấu hình trường chép sang nháp khôi phục: bỏ trường không còn hợp lệ và trường có key theo FieldNameOcr hiện tại rỗng / quá dài / trùng (21.10)
        public static List<PromptFieldConfigValue> BuildRestoredFieldConfigs(List<PromptFieldConfig> sourceConfigs, List<DocumentField> documentFields)
        {
            var ocrFieldById = documentFields.Where(field => field.IsFieldOcr).ToDictionary(field => field.Id);
            var validConfigs = sourceConfigs.Where(config => ocrFieldById.ContainsKey(config.DocumentFieldId)).ToList();
            var invalidKeyFieldIds = FindInvalidOutputKeyFieldIds(validConfigs.Select(config => ocrFieldById[config.DocumentFieldId]).ToList());

            return validConfigs
                .Where(config => !invalidKeyFieldIds.Contains(config.DocumentFieldId))
                .Select(config =>
                {
                    var field = ocrFieldById[config.DocumentFieldId];
                    return new PromptFieldConfigValue(field.Id, field.FieldNameOcr!.Trim(), config.Description, config.Example, config.NormalizeRule,
                        config.IsRequired, field.OrderListView, config.IsActive);
                })
                .ToList();
        }

        // Cảnh báo lệch trường (21.8): trường OCR hiện có mà bản chưa cấu hình, và cấu hình trỏ trường đã xoá / tắt OCR / khác metadata
        public static PromptFieldClassification Classify(List<int> configuredFieldIds, List<DocumentField> documentFields)
        {
            var ocrFieldIds = documentFields.Where(field => field.IsFieldOcr).Select(field => field.Id).ToList();
            return new PromptFieldClassification(
                ocrFieldIds.Except(configuredFieldIds).ToList(),
                configuredFieldIds.Except(ocrFieldIds).ToList());
        }

        // Điều kiện xuất bản (21.9): không còn cấu hình trường không hợp lệ (911), có ít nhất một trường đang dùng (913)
        public static void EnsurePublishable(List<PromptFieldConfig> configs, List<DocumentField> documentFields)
        {
            var classification = Classify(configs.Select(config => config.DocumentFieldId).ToList(), documentFields);
            if (classification.InvalidFieldIds.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorPromptFieldInvalid);
            }
            if (!configs.Any(config => config.IsActive))
            {
                throw new UserFriendlyException(ErrorCodes.AiErrorPromptNoActiveField);
            }
        }

        // Ghép prompt gửi AI (21.11): bỏ cấu hình không hợp lệ và trường tạm tắt, trường theo Order; hàm thuần để Task 22B gọi lại
        public static ComposedPrompt Compose(PromptTemplate template, List<PromptFieldConfig> configs, List<DocumentField> documentFields)
        {
            var ocrFieldById = documentFields.Where(field => field.IsFieldOcr).ToDictionary(field => field.Id);
            var lines = new List<string>();
            if (template.InstructionPrompt.Length > 0)
            {
                lines.Add(template.InstructionPrompt);
                lines.Add(String.Empty);
            }

            lines.Add("Trích xuất các trường sau và trả về DUY NHẤT một JSON object, không giải thích thêm:");
            foreach (var config in configs.Where(config => config.IsActive && ocrFieldById.ContainsKey(config.DocumentFieldId)).OrderBy(config => config.Order))
            {
                var line = $"- \"{config.OutputKey}\" ({ocrFieldById[config.DocumentFieldId].Name})";
                if (config.IsRequired)
                {
                    line += ", bắt buộc";
                }
                // Mô tả / ví dụ / định dạng tách bằng " | " để không dính vào nhau khi mô tả không kết thúc bằng dấu câu
                var details = new List<string>();
                if (config.Description.Length > 0)
                {
                    details.Add(config.Description);
                }
                if (!String.IsNullOrEmpty(config.Example))
                {
                    details.Add($"Ví dụ: {config.Example}");
                }
                if (NormalizeRuleDescriptions.TryGetValue(config.NormalizeRule, out var ruleDescription))
                {
                    details.Add($"Định dạng: {ruleDescription}");
                }
                if (details.Count > 0)
                {
                    line += $": {String.Join(" | ", details)}";
                }
                lines.Add(line);
            }
            lines.Add("Trường không tìm thấy trong văn bản thì để chuỗi rỗng \"\".");

            return new ComposedPrompt(template.SystemPrompt, String.Join("\n", lines));
        }

        // Tạo nháp số phiên bản kế tiếp (kể cả bản đã xoá) và đưa vào repository; SaveChanges để có Id
        private async Task<PromptTemplate> CreateDraftAsync(int documentTypeId, int? sourceTemplateId, string systemPrompt, string instructionPrompt,
            string? note, string? createdBy, DateTime createdDate)
        {
            var version = await _promptTemplateRepository.GetNextVersionAsync(documentTypeId);
            var draft = PromptTemplate.Create(documentTypeId, version, systemPrompt, instructionPrompt, sourceTemplateId, note, createdBy, createdDate);
            _promptTemplateRepository.Add(draft);
            return draft;
        }

        // Id trường có FieldNameOcr rỗng, quá OUTPUT_KEY_MAX_LENGTH hoặc trùng (không phân biệt hoa thường, như collation DB) với trường khác trong danh sách
        private static HashSet<int> FindInvalidOutputKeyFieldIds(List<DocumentField> fields)
        {
            var outputKeys = fields.Select(field => (field.Id, OutputKey: field.FieldNameOcr?.Trim() ?? String.Empty)).ToList();
            var duplicateKeys = outputKeys
                .GroupBy(item => item.OutputKey, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return outputKeys
                .Where(item => item.OutputKey.Length == 0 || item.OutputKey.Length > PromptTemplateConst.OUTPUT_KEY_MAX_LENGTH || duplicateKeys.Contains(item.OutputKey))
                .Select(item => item.Id)
                .ToHashSet();
        }
    }

    public record PromptFieldInput(int DocumentFieldId, string Description, string? Example, string NormalizeRule, bool IsRequired, bool IsActive);

    public record PromptFieldConfigValue(int DocumentFieldId, string OutputKey, string Description, string? Example, string NormalizeRule,
        bool IsRequired, int Order, bool IsActive);

    public record PromptFieldClassification(List<int> MissingFieldIds, List<int> InvalidFieldIds);

    public record ComposedPrompt(string SystemPrompt, string Prompt);
}
