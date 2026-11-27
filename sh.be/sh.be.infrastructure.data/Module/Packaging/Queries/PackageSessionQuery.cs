using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Packaging.Dtos.PackageSession;
using sh.be.applications.Module.Packaging.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Packaging.Queries
{
    public class PackageSessionQuery : BaseQuery, IPackageSessionQuery
    {
        public PackageSessionQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public Task<PackageSessionProgressDto?> FindProgress(int id)
        {
            return _shDbContext.PackageSessions
                .AsNoTracking()
                .Where(ps => ps.Id == id)
                .ProjectTo<PackageSessionProgressDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<ViewPackageSessionResultDto?> FindResult(int id)
        {
            var result = await _shDbContext.PackageSessions
                .AsNoTracking()
                .Where(ps => ps.Id == id)
                .ProjectTo<ViewPackageSessionResultDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
            if (result == null)
            {
                return null;
            }

            result.Dossiers = await _shDbContext.PackageDossiers
                .AsNoTracking()
                .Where(pd => pd.SessionId == id)
                .OrderBy(pd => pd.ExcelDocumentId)
                .ThenBy(pd => pd.RowIndex)
                .ProjectTo<GetListPackageDossierDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            return result;
        }

        public Task<string?> FindMatchReport(int id)
        {
            return _shDbContext.PackageSessions
                .AsNoTracking()
                .Where(ps => ps.Id == id)
                .Select(ps => ps.MatchReport)
                .FirstOrDefaultAsync();
        }

        public async Task<BaseResponsePagingDto<GetListPackageDocumentDto>> FindDocumentPaging(int sessionId, FindPagingPackageDocumentDto dto)
        {
            var query = _shDbContext.PackageDocuments
                .AsNoTracking()
                .Where(pd => pd.SessionId == sessionId);
            if (dto.DossierId.HasValue)
            {
                query = query.Where(pd => pd.DossierId == dto.DossierId);
            }
            if (!string.IsNullOrEmpty(dto.Status))
            {
                query = query.Where(pd => pd.Status == dto.Status);
            }

            var totalCount = await query.CountAsync();

            var documents = await query
                .OrderBy(pd => pd.RelativePath)
                .ThenBy(pd => pd.Id)
                .Paging(dto)
                .ProjectTo<GetListPackageDocumentDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new BaseResponsePagingDto<GetListPackageDocumentDto>
            {
                Items = documents,
                TotalItems = totalCount,
            };
        }
    }
}
