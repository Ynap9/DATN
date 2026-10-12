using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.Services
{
    public class DocumentFieldDomainService
    {
        private readonly IDocumentFieldRepository _documentFieldRepository;
        private readonly IDocumentTypeRepository _documentTypeRepository;

        public DocumentFieldDomainService(IDocumentFieldRepository documentFieldRepository, IDocumentTypeRepository documentTypeRepository)
        {
            _documentFieldRepository = documentFieldRepository;
            _documentTypeRepository = documentTypeRepository;
        }

        public Task LockOrderAsync(int documentTypeId)
        {
            return _documentTypeRepository.LockAsync(documentTypeId);
        }

        public void EnsureHasIds(IReadOnlyCollection<int> ids)
        {
            if (ids.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Danh sách trường cần xóa không được để trống");
            }
        }

        public void EnsureAllFound(IReadOnlyCollection<int> ids, IReadOnlyCollection<DocumentField> documentFields)
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

        public void EnsureHasFieldNames(IReadOnlyCollection<string> fieldNames)
        {
            if (fieldNames.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Danh sách tên trường không được để trống");
            }
        }

        public async Task EnsureUniqueFieldsAsync(int documentTypeId, IEnumerable<string> fields)
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

        public async Task MoveToOrderAsync(DocumentField movedField, int order, string? modifiedBy, DateTime modifiedDate)
        {
            var orderedFields = await GetOrderedFieldsExceptAsync(movedField);
            var position = Math.Clamp(order, 1, orderedFields.Count + 1);
            orderedFields.Insert(position - 1, movedField);
            RenumberOrders(orderedFields, modifiedBy, modifiedDate);
        }

        private async Task<List<DocumentField>> GetOrderedFieldsExceptAsync(DocumentField excludedField)
        {
            var documentFields = await _documentFieldRepository.GetByDocumentTypeIdAsync(excludedField.DocumentTypeId);
            return documentFields.Where(d => d.Id != excludedField.Id).ToList();
        }

        private static void RenumberOrders(List<DocumentField> orderedFields, string? modifiedBy, DateTime modifiedDate)
        {
            for (var index = 0; index < orderedFields.Count; index++)
            {
                var order = index + 1;
                var documentField = orderedFields[index];
                if (documentField.OrderTableView != order || documentField.OrderListView != order)
                {
                    documentField.ChangeOrder(order, modifiedBy, modifiedDate);
                }
            }
        }
    }
}
