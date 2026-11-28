using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.DocumentEnum;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.domain.Module.Construction.DocumentFields.Services;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class DocumentEnumQuery : BaseQuery, IDocumentEnumQuery
    {
        public DocumentEnumQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public async Task<List<GetListDocumentEnumDto>> GetList(int documentTypeId)
        {
            var documentEnums = await _shDbContext.DocumentEnums
                .AsNoTracking()
                .Where(e => e.DocumentTypeId == documentTypeId)
                .OrderBy(e => e.Name)
                .ToListAsync();
            var usedFields = await _shDbContext.DocumentFields
                .AsNoTracking()
                .Where(d => d.DocumentTypeId == documentTypeId && d.EnumId != null)
                .OrderBy(d => d.OrderTableView)
                .ThenBy(d => d.Id)
                .Select(d => new { d.EnumId, Field = new DocumentEnumFieldDto { Id = d.Id, Name = d.Name } })
                .ToListAsync();

            return documentEnums
                .Select(e => new GetListDocumentEnumDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Values = DocumentFieldDomainService.ParseEnumValues(e.ValuesJson),
                    UsedByFields = usedFields.Where(f => f.EnumId == e.Id).Select(f => f.Field).ToList(),
                })
                .ToList();
        }
    }
}
