using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Dtos.DocumentType;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.domain.Module.Construction.DocumentTypes.Entities;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class DocumentTypeQuery : BaseQuery, IDocumentTypeQuery
    {
        public DocumentTypeQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        // Dropdown loại văn bản: Admin mọi loại, còn lại loại thuộc cặp dự án mình được phân
        public Task<List<GetListDocumentTypeDropDownDto>> GetDocumentTypeDropDown(string userId, bool isAdmin)
        {
            return FilterViewable(_shDbContext.DocumentTypes.AsNoTracking(), userId, isAdmin)
                .OrderBy(d => d.Name)
                .ProjectTo<GetListDocumentTypeDropDownDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        // Paging loại văn bản, cùng phạm vi xem với dropdown; kèm số trường và số dự án đang dùng (người khác Admin chỉ đếm dự án mình được phân)
        public async Task<BaseResponsePagingDto<GetListDocumentTypeDto>> FindPaging(FindPagingDocumentTypeDto dto, string userId, bool isAdmin)
        {
            var query = FilterViewable(_shDbContext.DocumentTypes.AsNoTracking(), userId, isAdmin);
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(d => d.Name.Contains(dto.Keyword));
            }

            var totalCount = await query.CountAsync();

            var documentTypes = await query
                .OrderByDescending(d => d.CreatedDate)
                .ThenByDescending(d => d.Id)
                .Paging(dto)
                .Select(d => new GetListDocumentTypeDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    DefaultView = d.DefaultView,
                    CreatedDate = d.CreatedDate,
                    FieldCount = _shDbContext.DocumentFields.Count(f => f.DocumentTypeId == d.Id),
                    // Cùng luật chặn xoá IsUsedByProjectAsync (dự án chưa xoá, mọi trạng thái); điều kiện thành viên khớp FilterViewable
                    ProjectCount = _shDbContext.ProjectDocumentTypes
                        .Where(pd => pd.DocumentTypeId == d.Id
                            && _shDbContext.Projects.Any(p => p.Id == pd.ProjectId)
                            && (isAdmin || _shDbContext.ProjectMembers.Any(m => m.ProjectId == pd.ProjectId && m.DocumentTypeId == pd.DocumentTypeId && m.UserId == userId)))
                        .Select(pd => pd.ProjectId)
                        .Distinct()
                        .Count(),
                })
                .ToListAsync();

            return new BaseResponsePagingDto<GetListDocumentTypeDto>
            {
                Items = documentTypes,
                TotalItems = totalCount,
            };
        }

        public Task<FindDocumentTypeByIdDto?> FindById(int id)
        {
            return _shDbContext.DocumentTypes
                .AsNoTracking()
                .Where(d => d.Id == id)
                .ProjectTo<FindDocumentTypeByIdDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<List<GetListDocumentFieldDto>> GetListDocumentField(int id)
        {
            var documentFields = await _shDbContext.DocumentFields
                .AsNoTracking()
                .Where(d => d.DocumentTypeId == id)
                .OrderBy(d => d.OrderTableView)
                .ThenBy(d => d.Id)
                .ProjectTo<GetListDocumentFieldDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            var enumNames = await _shDbContext.DocumentEnums
                .AsNoTracking()
                .Where(e => e.DocumentTypeId == id)
                .ToDictionaryAsync(e => e.Id, e => e.Name);

            foreach (var documentField in documentFields.Where(d => d.EnumId.HasValue))
            {
                documentField.EnumName = enumNames.GetValueOrDefault(documentField.EnumId!.Value);
            }
            return documentFields;
        }

        public Task<List<string>> GetListTemplateHeader(int id)
        {
            return _shDbContext.DocumentFields
                .AsNoTracking()
                .Where(d => d.DocumentTypeId == id)
                .OrderBy(d => d.OrderTableView)
                .ThenBy(d => d.Id)
                .Select(d => string.IsNullOrWhiteSpace(d.FieldImport) ? d.Name : d.FieldImport!)
                .ToListAsync();
        }

        // Cùng luật DocumentTypeRepository.IsAssignedToUserAsync: thành viên chưa gỡ ở cặp dự án – loại văn bản đang gắn
        private IQueryable<DocumentType> FilterViewable(IQueryable<DocumentType> query, string userId, bool isAdmin)
        {
            return query.Where(d => isAdmin || _shDbContext.ProjectDocumentTypes.Any(pd => pd.DocumentTypeId == d.Id
                && _shDbContext.ProjectMembers.Any(m => m.ProjectId == pd.ProjectId && m.DocumentTypeId == pd.DocumentTypeId && m.UserId == userId)));
        }
    }
}
