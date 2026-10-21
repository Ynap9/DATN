using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.domain.Module.Construction.Entities;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class SubmissionQuery : BaseQuery, ISubmissionQuery
    {
        public SubmissionQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public async Task<BaseResponsePagingDto<GetListSubmissionDto>> FindPaging(FindPagingSubmissionDto dto)
        {
            var query = ApplyFilter(dto, dto.StatusCheck);
            var totalItems = await query.CountAsync();

            var submissions = await query
                .OrderBy(s => s.Id)
                .Paging(dto)
                .Select(s => new GetListSubmissionDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    FileName = s.FileName,
                    FileUrlS3Object = s.FileUrlS3Object,
                    ProfileRange = s.ProfileRange,
                    Box = s.Box,
                    StatusCheck = s.StatusCheck,
                    CreatedDate = s.CreatedDate,
                    ModifiedDate = s.ModifiedDate,
                })
                .ToListAsync();

            var documentFields = await _shDbContext.ProjectDocumentTypeDocumentFields
                .AsNoTracking()
                .Where(pf => pf.ProjectId == dto.ProjectId && pf.DocumentTypeId == dto.DocumentTypeId)
                .Join(_shDbContext.DocumentFields.AsNoTracking().Where(d => d.IsShowTableView),
                    pf => pf.DocumentFieldId,
                    d => d.Id,
                    (pf, d) => d)
                .OrderBy(d => d.OrderTableView)
                .ThenBy(d => d.Id)
                .ProjectTo<GetListDocumentFieldDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            var documentFieldIds = documentFields.Select(d => d.Id).ToList();
            var submissionIds = submissions.Select(s => s.Id).ToList();

            var submissionDatas = await _shDbContext.SubmissionDatas
                .AsNoTracking()
                .Where(sd => submissionIds.Contains(sd.SubmissionId) && documentFieldIds.Contains(sd.DocumentFieldId))
                .Select(sd => new { sd.Id, sd.SubmissionId, sd.DocumentFieldId, sd.Data, sd.IndexRow, sd.IndexGoogleSheet })
                .ToListAsync();

            foreach (var submission in submissions)
            {
                submission.SubmissionDatas = submissionDatas
                    .Where(sd => sd.SubmissionId == submission.Id)
                    .OrderBy(sd => documentFieldIds.IndexOf(sd.DocumentFieldId))
                    .Select(sd => new SubmissionDataDto
                    {
                        Id = sd.Id,
                        DocumentFieldId = sd.DocumentFieldId,
                        Data = sd.Data,
                        IndexRow = sd.IndexRow,
                        IndexGoogleSheet = sd.IndexGoogleSheet,
                        DocumentField = documentFields[documentFieldIds.IndexOf(sd.DocumentFieldId)],
                    })
                    .ToList();
            }

            return new BaseResponsePagingDto<GetListSubmissionDto>
            {
                Items = submissions,
                TotalItems = totalItems,
                CustomData = documentFields.Select(d => new SubmissionColumnDto
                {
                    DocumentFieldId = d.Id,
                    Name = d.Name,
                    Field = d.Field,
                    OrderTableView = d.OrderTableView,
                    Width = d.Width,
                }).ToList(),
            };
        }

        public async Task<ViewSubmissionDetailDto?> FindById(int id, ISubmissionFilter filter, bool includePosition)
        {
            var detail = await _shDbContext.Submissions
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new ViewSubmissionDetailDto
                {
                    Id = s.Id,
                    ProjectId = s.ProjectId,
                    DocumentTypeId = s.DocumentTypeId,
                    Name = s.Name,
                    FileName = s.FileName,
                    FileUrlS3Object = s.FileUrlS3Object,
                    ProfileRange = s.ProfileRange,
                    Box = s.Box,
                    StatusCheck = s.StatusCheck,
                    CreatedDate = s.CreatedDate,
                    ModifiedDate = s.ModifiedDate,
                    ModifiedBy = s.ModifiedBy,
                })
                .FirstOrDefaultAsync();
            if (detail == null)
            {
                return null;
            }

            detail.Fields = await (from pf in _shDbContext.ProjectDocumentTypeDocumentFields.AsNoTracking()
                                   join d in _shDbContext.DocumentFields.AsNoTracking() on pf.DocumentFieldId equals d.Id
                                   join sd in _shDbContext.SubmissionDatas.AsNoTracking().Where(x => x.SubmissionId == detail.Id)
                                       on d.Id equals sd.DocumentFieldId into datas
                                   from sd in datas.DefaultIfEmpty()
                                   where pf.ProjectId == detail.ProjectId && pf.DocumentTypeId == detail.DocumentTypeId
                                   orderby d.OrderListView, d.Id
                                   select new SubmissionFieldDto
                                   {
                                       DocumentFieldId = d.Id,
                                       Name = d.Name,
                                       Field = d.Field,
                                       FieldType = d.FieldType,
                                       IsShow = d.IsShow,
                                       IsChargeable = d.IsChargeable,
                                       OrderListView = d.OrderListView,
                                       IsShowListView = d.IsShowListView,
                                       Width = d.Width,
                                       SubmissionDataId = sd != null ? sd.Id : null,
                                       Data = sd != null ? sd.Data : String.Empty,
                                       IndexGoogleSheet = sd != null ? sd.IndexGoogleSheet : null,
                                   })
                .ToListAsync();

            if (includePosition)
            {
                var counts = await ApplyFilter(filter, filter.StatusCheck)
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        TotalItems = g.Count(),
                        BeforeCount = g.Count(s => s.Id < detail.Id),
                        IsMatched = g.Count(s => s.Id == detail.Id),
                    })
                    .SingleOrDefaultAsync();

                detail.TotalItems = counts?.TotalItems ?? 0;
                detail.Position = counts is { IsMatched: > 0 } ? counts.BeforeCount + 1 : null;
            }
            return detail;
        }

        public Task<int?> FindNextId(int id, ISubmissionFilter filter)
        {
            return ApplyFilter(filter, filter.StatusCheck)
                .Where(s => s.Id > id)
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
        }

        public Task<int?> FindPreviousId(int id, ISubmissionFilter filter)
        {
            return ApplyFilter(filter, filter.StatusCheck)
                .Where(s => s.Id < id)
                .OrderByDescending(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<ViewCountSubmissionDto> Count(ISubmissionFilter filter)
        {
            var counts = await ApplyFilter(filter, null)
                .GroupBy(_ => 1)
                .Select(g => new ViewCountSubmissionDto
                {
                    TotalChecked = g.Count(s => s.StatusCheck == SubmissionConst.CHECKED),
                    TotalNotChecked = g.Count(s => s.StatusCheck == SubmissionConst.NOT_CHECKED),
                })
                .SingleOrDefaultAsync() ?? new ViewCountSubmissionDto();

            counts.Total = filter.StatusCheck switch
            {
                SubmissionConst.CHECKED => counts.TotalChecked,
                SubmissionConst.NOT_CHECKED => counts.TotalNotChecked,
                _ => counts.TotalChecked + counts.TotalNotChecked,
            };
            return counts;
        }

        public async Task<BaseResponsePagingDto<GetListSubmissionLogDto>> FindLogPaging(int submissionId, FindPagingSubmissionLogDto dto)
        {
            var query = _shDbContext.SubmissionLogs
                .AsNoTracking()
                .Where(l => l.SubmissionId == submissionId);
            var totalItems = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedDate)
                .ThenByDescending(l => l.Id)
                .Paging(dto)
                .Select(l => new GetListSubmissionLogDto
                {
                    Id = l.Id,
                    SubmissionId = l.SubmissionId,
                    UserId = l.UserId,
                    UserName = _shDbContext.Users.Where(u => u.Id == l.UserId).Select(u => u.UserName).FirstOrDefault(),
                    FullName = _shDbContext.Users.Where(u => u.Id == l.UserId).Select(u => u.FullName).FirstOrDefault(),
                    OldStatus = l.OldStatus,
                    NewStatus = l.NewStatus,
                    Description = l.Description,
                    Note = l.Note,
                    ChangedDataCount = l.ChangedDataCount,
                    CreatedDate = l.CreatedDate,
                })
                .ToListAsync();

            return new BaseResponsePagingDto<GetListSubmissionLogDto>
            {
                Items = logs,
                TotalItems = totalItems,
            };
        }

        private IQueryable<Submission> ApplyFilter(ISubmissionFilter filter, int? statusCheck)
        {
            var query = _shDbContext.Submissions
                .AsNoTracking()
                .Where(s => s.ProjectId == filter.ProjectId && s.DocumentTypeId == filter.DocumentTypeId);

            if (statusCheck.HasValue)
            {
                query = query.Where(s => s.StatusCheck == statusCheck.Value);
            }

            var keyword = filter.Keyword?.Trim();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(s => s.FileName.Contains(keyword) || s.ProfileRange.Contains(keyword) || s.Box.Contains(keyword));
            }

            query = ApplyFieldFilter(query, filter.DocumentFieldId1, filter.FieldValue1);
            return ApplyFieldFilter(query, filter.DocumentFieldId2, filter.FieldValue2);
        }

        private IQueryable<Submission> ApplyFieldFilter(IQueryable<Submission> query, int? documentFieldId, string? fieldValue)
        {
            var value = fieldValue?.Trim();
            if (!documentFieldId.HasValue || string.IsNullOrEmpty(value))
            {
                return query;
            }

            var fieldId = documentFieldId.Value;
            return query.Where(s => _shDbContext.SubmissionDatas.Any(sd => sd.SubmissionId == s.Id && sd.DocumentFieldId == fieldId && sd.Data == value));
        }
    }
}
