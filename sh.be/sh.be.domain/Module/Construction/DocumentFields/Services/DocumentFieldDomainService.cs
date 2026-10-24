using sh.be.domain.Module.Construction.DocumentFields.Entities;
using sh.be.domain.Module.Construction.DocumentFields.Repositories;
using sh.be.domain.Module.Construction.DocumentTypes.Repositories;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.DocumentFields.Services
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
}
