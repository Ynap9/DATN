using sh.be.domain.Module.Construction.DocumentFields.Entities;
using sh.be.domain.Module.Construction.DocumentFields.Repositories;
using sh.be.domain.Module.Construction.DocumentTypes.Repositories;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace sh.be.domain.Module.Construction.DocumentFields.Services
{
    public class DocumentFieldDomainService
    {
        private static readonly JsonSerializerOptions EnumValuesJsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        private readonly IDocumentFieldRepository _documentFieldRepository;
        private readonly IDocumentTypeRepository _documentTypeRepository;

        public DocumentFieldDomainService(IDocumentFieldRepository documentFieldRepository, IDocumentTypeRepository documentTypeRepository)
        {
            _documentFieldRepository = documentFieldRepository;
            _documentTypeRepository = documentTypeRepository;
        }

        public static bool IsFieldImportMatch(string? fieldImport, string header)
        {
            var fieldImportWords = SplitWords(fieldImport);
            return fieldImportWords.Length > 0
                && fieldImportWords.SequenceEqual(SplitWords(header), StringComparer.OrdinalIgnoreCase);
        }

        public static string ToFieldImportMatchKey(string? text)
        {
            return string.Join(' ', SplitWords(text));
        }

        private static string[] SplitWords(string? text)
        {
            return (text ?? String.Empty)
                .Normalize(NormalizationForm.FormC)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }

        public Task LockOrderAsync(int documentTypeId)
        {
            return _documentTypeRepository.LockAsync(documentTypeId);
        }

        public void EnsureHasIds(List<int> ids)
        {
            if (ids.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Danh sách trường cần xóa không được để trống");
            }
        }

        public void EnsureAllFound(List<int> ids, List<DocumentField> documentFields)
        {
            var missingIds = ids.Except(documentFields.Select(d => d.Id)).ToList();
            if (missingIds.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentFieldNotFound,
                    $"Trường không tồn tại trong loại văn bản: Id = {string.Join(", ", missingIds)}");
            }
        }

        public async Task EnsureDocumentTypeExistsAsync(int documentTypeId)
        {
            if (await _documentTypeRepository.FindByIdAsync(documentTypeId) == null)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotFound);
            }
        }

        public async Task EnsureUniqueFieldAsync(int documentTypeId, string field, int? excludedId = null)
        {
            if (await _documentFieldRepository.ExistsByFieldAsync(documentTypeId, field, excludedId))
            {
                throw new UserFriendlyException(ErrorCodes.Found, $"Trường {field} đã tồn tại trong loại văn bản");
            }
        }

        public void EnsureHasFieldNames(List<string> fieldNames)
        {
            if (fieldNames.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Danh sách tên trường không được để trống");
            }
        }

        public async Task EnsureUniqueFieldsAsync(int documentTypeId, List<string> fields)
        {
            foreach (var field in fields)
            {
                await EnsureUniqueFieldAsync(documentTypeId, field);
            }
        }

        public async Task<int> GetNextOrderAsync(int documentTypeId)
        {
            return await _documentFieldRepository.GetMaxOrderAsync(documentTypeId) + 1;
        }

        // Tên danh mục trim, 1–200 ký tự
        public static string NormalizeEnumName(string? name)
        {
            var normalizedName = name?.Trim() ?? String.Empty;
            if (normalizedName.Length == 0 || normalizedName.Length > DocumentFieldConst.ENUM_NAME_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Tên danh mục phải từ 1 đến {DocumentFieldConst.ENUM_NAME_MAX_LENGTH} ký tự");
            }
            return normalizedName;
        }

        // Giá trị danh mục: trim từng dòng, bỏ dòng trống, bỏ trùng không phân biệt hoa thường (giữ cách viết lần đầu), giữ thứ tự nhập
        public static List<string> NormalizeEnumValues(List<string>? values)
        {
            var normalizedValues = (values ?? [])
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .DistinctBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (normalizedValues.Count == 0 || normalizedValues.Count > DocumentFieldConst.ENUM_VALUES_MAX_COUNT)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Danh mục phải có từ 1 đến {DocumentFieldConst.ENUM_VALUES_MAX_COUNT} giá trị");
            }
            var tooLongValue = normalizedValues.FirstOrDefault(value => value.Length > DocumentFieldConst.ENUM_VALUE_MAX_LENGTH);
            if (tooLongValue != null)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest,
                    $"Giá trị \"{tooLongValue[..30]}...\" dài quá {DocumentFieldConst.ENUM_VALUE_MAX_LENGTH} ký tự");
            }
            return normalizedValues;
        }

        public static string SerializeEnumValues(List<string> values)
        {
            return JsonSerializer.Serialize(values, EnumValuesJsonOptions);
        }

        public static List<string> ParseEnumValues(string valuesJson)
        {
            return JsonSerializer.Deserialize<List<string>>(valuesJson) ?? [];
        }

        // Giá trị danh mục hiện tại của từng trường kiểu danh mục trong loại văn bản, theo Id trường
        public async Task<Dictionary<int, EnumFieldValues>> GetEnumFieldValuesAsync(int documentTypeId)
        {
            var enumValuesById = (await _documentFieldRepository.GetEnumsByDocumentTypeIdAsync(documentTypeId))
                .ToDictionary(documentEnum => documentEnum.Id, documentEnum => ParseEnumValues(documentEnum.ValuesJson));
            return (await _documentFieldRepository.GetByDocumentTypeIdAsync(documentTypeId))
                .Where(field => field.EnumId.HasValue && enumValuesById.ContainsKey(field.EnumId.Value))
                .ToDictionary(field => field.Id, field => new EnumFieldValues(field.Name, enumValuesById[field.EnumId!.Value]));
        }

        public async Task EnsureUniqueEnumNameAsync(int documentTypeId, string name, int? excludedId = null)
        {
            if (await _documentFieldRepository.ExistsEnumByNameAsync(documentTypeId, name, excludedId))
            {
                throw new UserFriendlyException(ErrorCodes.Found, $"Danh mục {name} đã tồn tại trong loại văn bản");
            }
        }

        public async Task<DocumentEnum> FindEnumAsync(int documentTypeId, int enumId)
        {
            return await _documentFieldRepository.FindEnumByIdAsync(documentTypeId, enumId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentEnumNotFound);
        }

        public async Task EnsureEnumNotUsedAsync(int enumId)
        {
            var fieldNames = await _documentFieldRepository.GetFieldNamesByEnumIdAsync(enumId);
            if (fieldNames.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentEnumInUse, $"Danh mục đang được dùng bởi trường: {string.Join(", ", fieldNames)}");
            }
        }

        // Kiểu "enum" bắt buộc trỏ tới danh mục chưa xoá của cùng loại văn bản; kiểu "string" thì không giữ danh mục
        public async Task<int?> ResolveEnumIdAsync(int documentTypeId, string fieldType, int? enumId)
        {
            if (!DocumentFieldConst.FIELD_TYPES.Contains(fieldType))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Kiểu dữ liệu chỉ nhận {string.Join(" hoặc ", DocumentFieldConst.FIELD_TYPES)}");
            }
            if (fieldType == DocumentFieldConst.FIELD_TYPE_STRING)
            {
                return null;
            }
            return (await FindEnumAsync(documentTypeId, enumId ?? 0)).Id;
        }

        public Task MoveOrderTableViewAsync(DocumentField movedField, int targetOrder, string? modifiedBy, DateTime modifiedDate)
        {
            return MoveOrderAsync(movedField, targetOrder, d => d.OrderTableView,
                (d, order) => d.ChangeOrderTableView(order, modifiedBy, modifiedDate));
        }

        public Task MoveOrderListViewAsync(DocumentField movedField, int targetOrder, string? modifiedBy, DateTime modifiedDate)
        {
            return MoveOrderAsync(movedField, targetOrder, d => d.OrderListView,
                (d, order) => d.ChangeOrderListView(order, modifiedBy, modifiedDate));
        }

        private async Task MoveOrderAsync(DocumentField movedField, int targetOrder,
            Func<DocumentField, int> getOrder, Action<DocumentField, int> changeOrder)
        {
            var documentFields = await _documentFieldRepository.GetByDocumentTypeIdAsync(movedField.DocumentTypeId);
            var fromOrder = getOrder(movedField);
            var toOrder = Math.Clamp(targetOrder, 1, documentFields.Max(getOrder));
            if (fromOrder == toOrder)
            {
                return;
            }

            var shift = fromOrder < toOrder ? -1 : 1;
            var minOrder = Math.Min(fromOrder, toOrder);
            var maxOrder = Math.Max(fromOrder, toOrder);
            var shiftedFields = documentFields
                .Where(d => d.Id != movedField.Id && getOrder(d) >= minOrder && getOrder(d) <= maxOrder)
                .ToList();

            foreach (var documentField in shiftedFields)
            {
                changeOrder(documentField, getOrder(documentField) + shift);
            }
            changeOrder(movedField, toOrder);
        }
    }

    public record EnumFieldValues(string FieldName, List<string> Values);
}
