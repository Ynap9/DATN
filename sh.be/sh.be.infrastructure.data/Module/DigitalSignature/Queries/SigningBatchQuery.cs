using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch;
using sh.be.applications.Module.DigitalSignature.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.DigitalSignature;

namespace sh.be.infrastructure.data.Module.DigitalSignature.Queries
{
    public class SigningBatchQuery : BaseQuery, ISigningBatchQuery
    {
        public SigningBatchQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public Task<FindSigningBatchByIdDto?> FindById(int id)
        {
            return _shDbContext.SigningBatches
                .AsNoTracking()
                .Where(sb => sb.Id == id)
                .ProjectTo<FindSigningBatchByIdDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public Task<FindSigningBatchByIdDto?> FindRunning(string createdBy)
        {
            return _shDbContext.SigningBatches
                .AsNoTracking()
                .Where(sb => sb.CreatedBy == createdBy && SigningBatchConst.RUNNING_STATUSES.Contains(sb.Status))
                .OrderByDescending(sb => sb.Id)
                .ProjectTo<FindSigningBatchByIdDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public Task<List<GetListSigningBatchFileDto>> GetFiles(int signingBatchId)
        {
            return _shDbContext.SigningBatchFiles
                .AsNoTracking()
                .Where(sbf => sbf.SigningBatchId == signingBatchId)
                .OrderBy(sbf => sbf.Order)
                .ProjectTo<GetListSigningBatchFileDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
    }
}
