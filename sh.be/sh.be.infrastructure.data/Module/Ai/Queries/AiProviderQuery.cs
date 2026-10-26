using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Ai.Dtos.AiProvider;
using sh.be.applications.Module.Ai.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Ai;
using sh.be.shared.HttpRequest.BaseRequest;
using System.Text.Json;

namespace sh.be.infrastructure.data.Module.Ai.Queries
{
    public class AiProviderQuery : BaseQuery, IAiProviderQuery
    {
        public AiProviderQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public async Task<BaseResponsePagingDto<GetListAiProviderDto>> FindPaging(FindPagingAiProviderDto dto)
        {
            var query = _shDbContext.AiProviders.AsNoTracking();
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(p => p.Code.Contains(dto.Keyword) || p.Name.Contains(dto.Keyword) || p.BaseUrl.Contains(dto.Keyword));
            }
            if (!string.IsNullOrEmpty(dto.Protocol))
            {
                query = query.Where(p => p.Protocol == dto.Protocol);
            }
            if (!string.IsNullOrEmpty(dto.Scope))
            {
                query = query.Where(p => p.Scope == dto.Scope);
            }
            if (dto.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == dto.IsActive);
            }

            var totalCount = await query.CountAsync();

            var aiProviders = await query
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.Id)
                .Paging(dto)
                .Select(p => new GetListAiProviderDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Protocol = p.Protocol,
                    BaseUrl = p.BaseUrl,
                    Scope = p.Scope,
                    HasApiKey = p.HasApiKey,
                    ApiKeyFingerprint = p.ApiKeyHash == null ? null : p.ApiKeyHash.Substring(0, AiProviderConst.API_KEY_FINGERPRINT_LENGTH),
                    IsActive = p.IsActive,
                    ModelCount = _shDbContext.AiModels.Count(m => m.ProviderId == p.Id),
                    CreatedDate = p.CreatedDate,
                })
                .ToListAsync();

            return new BaseResponsePagingDto<GetListAiProviderDto>
            {
                Items = aiProviders,
                TotalItems = totalCount,
            };
        }

        public Task<List<GetListAiProviderDropDownDto>> GetDropDown()
        {
            return _shDbContext.AiProviders
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new GetListAiProviderDropDownDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Protocol = p.Protocol,
                    Scope = p.Scope,
                    HasApiKey = p.HasApiKey,
                })
                .ToListAsync();
        }

        public async Task<FindAiProviderByIdDto?> FindById(int id)
        {
            var aiProvider = await _shDbContext.AiProviders
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new FindAiProviderByIdDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Protocol = p.Protocol,
                    BaseUrl = p.BaseUrl,
                    Scope = p.Scope,
                    HasApiKey = p.HasApiKey,
                    ApiKeyFingerprint = p.ApiKeyHash == null ? null : p.ApiKeyHash.Substring(0, AiProviderConst.API_KEY_FINGERPRINT_LENGTH),
                    ApiKeyHeaderName = p.ApiKeyHeaderName,
                    ApiKeyHeaderPrefix = p.ApiKeyHeaderPrefix,
                    TimeoutSeconds = p.TimeoutSeconds,
                    MaxConcurrency = p.MaxConcurrency,
                    RateLimitPerMinute = p.RateLimitPerMinute,
                    ExtraHeadersJson = p.ExtraHeadersJson,
                    IsActive = p.IsActive,
                    ModelCount = _shDbContext.AiModels.Count(m => m.ProviderId == p.Id),
                    CreatedBy = p.CreatedBy,
                    CreatedDate = p.CreatedDate,
                    ModifiedBy = p.ModifiedBy,
                    ModifiedDate = p.ModifiedDate,
                })
                .FirstOrDefaultAsync();
            if (aiProvider?.ExtraHeadersJson != null)
            {
                aiProvider.ExtraHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(aiProvider.ExtraHeadersJson) ?? new();
            }
            return aiProvider;
        }
    }
}
