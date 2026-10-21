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
            var query = ApplyFilter(dto, dto.StatusCheck, dto.StatusQa);
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
                    StatusQa = s.StatusQa,
                    QaReason = s.QaReason,
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
                    StatusQa = s.StatusQa,
                    QaReason = s.QaReason,
                    CreatedDate = s.CreatedDate,
                    ModifiedDate = s.ModifiedDate,
                    ModifiedBy = s.ModifiedBy,
                })
                .FirstOrDefaultAsync();
            if (detail == null)
            {
                return null;
            }

            await FillFieldsAndPositionAsync(detail, filter, includePosition);
            return detail;
        }

        public Task<int?> FindNextId(int id, ISubmissionFilter filter)
        {
            return ApplyFilter(filter, filter.StatusCheck, filter.StatusQa)
                .Where(s => s.Id > id)
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
        }

        public Task<int?> FindPreviousId(int id, ISubmissionFilter filter)
        {
            return ApplyFilter(filter, filter.StatusCheck, filter.StatusQa)
                .Where(s => s.Id < id)
                .OrderByDescending(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<ViewCountSubmissionDto> Count(ISubmissionFilter filter)
        {
            var counts = await ApplyFilter(filter, null, filter.StatusQa)
                .GroupBy(_ => 1)
                .Select(g => new ViewCountSubmissionDto
                {
                    TotalChecked = g.Count(s => s.StatusCheck == SubmissionConst.CHECKED),
                    TotalNotChecked = g.Count(s => s.StatusCheck == SubmissionConst.NOT_CHECKED),
                    TotalReturned = g.Count(s => s.StatusCheck == SubmissionConst.RETURNED),
                })
                .SingleOrDefaultAsync() ?? new ViewCountSubmissionDto();

            counts.Total = filter.StatusCheck switch
            {
                SubmissionConst.CHECKED => counts.TotalChecked,
                SubmissionConst.NOT_CHECKED => counts.TotalNotChecked,
                SubmissionConst.RETURNED => counts.TotalReturned,
                _ => counts.TotalChecked + counts.TotalNotChecked + counts.TotalReturned,
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

        public async Task<BaseResponsePagingDto<GetListQaSubmissionDto>> FindQaPaging(FindPagingQaSubmissionDto dto)
        {
            var query = ApplyFilter(dto, dto.StatusCheck, dto.StatusQa);
            var totalItems = await query.CountAsync();

            var submissions = await (from s in query.OrderBy(s => s.Id).Paging(dto)
                                     let lastLog = _shDbContext.SubmissionLogs
                                         .Where(l => l.SubmissionId == s.Id)
                                         .OrderByDescending(l => l.CreatedDate)
                                         .ThenByDescending(l => l.Id)
                                         .FirstOrDefault()
                                     select new GetListQaSubmissionDto
                                     {
                                         Id = s.Id,
                                         Name = s.Name,
                                         FileName = s.FileName,
                                         FileUrlS3Object = s.FileUrlS3Object,
                                         ProfileRange = s.ProfileRange,
                                         Box = s.Box,
                                         StatusCheck = s.StatusCheck,
                                         StatusQa = s.StatusQa,
                                         QaReason = s.QaReason,
                                         QaBy = s.QaBy,
                                         QaUserName = _shDbContext.Users.Where(u => u.Id == s.QaBy).Select(u => u.UserName).FirstOrDefault(),
                                         QaDate = s.QaDate,
                                         CheckedBy = lastLog != null ? lastLog.UserId : null,
                                         CheckedUserName = lastLog != null
                                             ? _shDbContext.Users.Where(u => u.Id == lastLog.UserId).Select(u => u.UserName).FirstOrDefault()
                                             : null,
                                         CheckedDate = lastLog != null ? lastLog.CreatedDate : null,
                                     })
                .ToListAsync();

            var qaCounts = await ApplyFilter(dto, dto.StatusCheck, null)
                .GroupBy(_ => 1)
                .Select(g => new ViewQaCountDto
                {
                    TotalNotReviewed = g.Count(s => s.StatusQa == SubmissionConst.QA_NOT_REVIEWED),
                    TotalPassed = g.Count(s => s.StatusQa == SubmissionConst.QA_PASSED),
                    TotalFailed = g.Count(s => s.StatusQa == SubmissionConst.QA_FAILED),
                })
                .SingleOrDefaultAsync() ?? new ViewQaCountDto();

            return new BaseResponsePagingDto<GetListQaSubmissionDto>
            {
                Items = submissions,
                TotalItems = totalItems,
                CustomData = qaCounts,
            };
        }

        public async Task<ViewQaSubmissionDetailDto?> FindQaById(int id, ISubmissionFilter filter, bool includePosition)
        {
            var detail = await (from s in _shDbContext.Submissions.AsNoTracking().Where(s => s.Id == id)
                                let lastLog = _shDbContext.SubmissionLogs
                                    .Where(l => l.SubmissionId == s.Id)
                                    .OrderByDescending(l => l.CreatedDate)
                                    .ThenByDescending(l => l.Id)
                                    .FirstOrDefault()
                                select new ViewQaSubmissionDetailDto
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
                                    StatusQa = s.StatusQa,
                                    QaReason = s.QaReason,
                                    CreatedDate = s.CreatedDate,
                                    ModifiedDate = s.ModifiedDate,
                                    ModifiedBy = s.ModifiedBy,
                                    QaBy = s.QaBy,
                                    QaUserName = _shDbContext.Users.Where(u => u.Id == s.QaBy).Select(u => u.UserName).FirstOrDefault(),
                                    QaDate = s.QaDate,
                                    CheckedBy = lastLog != null ? lastLog.UserId : null,
                                    CheckedUserName = lastLog != null
                                        ? _shDbContext.Users.Where(u => u.Id == lastLog.UserId).Select(u => u.UserName).FirstOrDefault()
                                        : null,
                                    CheckedDate = lastLog != null ? lastLog.CreatedDate : null,
                                    CheckNote = lastLog != null ? lastLog.Note : null,
                                })
                .FirstOrDefaultAsync();
            if (detail == null)
            {
                return null;
            }

            await FillFieldsAndPositionAsync(detail, filter, includePosition);
            return detail;
        }

        public async Task<BaseResponsePagingDto<GetListSubmissionQaLogDto>> FindQaLogPaging(int submissionId, FindPagingSubmissionQaLogDto dto)
        {
            var query = _shDbContext.SubmissionQaLogs
                .AsNoTracking()
                .Where(l => l.SubmissionId == submissionId);
            var totalItems = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedDate)
                .ThenByDescending(l => l.Id)
                .Paging(dto)
                .Select(l => new GetListSubmissionQaLogDto
                {
                    Id = l.Id,
                    SubmissionId = l.SubmissionId,
                    UserId = l.UserId,
                    UserName = _shDbContext.Users.Where(u => u.Id == l.UserId).Select(u => u.UserName).FirstOrDefault(),
                    FullName = _shDbContext.Users.Where(u => u.Id == l.UserId).Select(u => u.FullName).FirstOrDefault(),
                    OldStatusQa = l.OldStatusQa,
                    NewStatusQa = l.NewStatusQa,
                    OldStatusCheck = l.OldStatusCheck,
                    NewStatusCheck = l.NewStatusCheck,
                    Reason = l.Reason,
                    Description = l.Description,
                    CreatedDate = l.CreatedDate,
                })
                .ToListAsync();

            var qaLogIds = logs.Select(l => l.Id).ToList();
            var wrongFields = await _shDbContext.SubmissionQaLogFields
                .AsNoTracking()
                .Where(f => qaLogIds.Contains(f.SubmissionQaLogId))
                .OrderBy(f => f.DocumentFieldId)
                .Select(f => new { f.SubmissionQaLogId, f.DocumentFieldId })
                .ToListAsync();
            foreach (var log in logs)
            {
                log.WrongDocumentFieldIds = wrongFields
                    .Where(f => f.SubmissionQaLogId == log.Id)
                    .Select(f => f.DocumentFieldId)
                    .ToList();
            }

            return new BaseResponsePagingDto<GetListSubmissionQaLogDto>
            {
                Items = logs,
                TotalItems = totalItems,
            };
        }

        public Task<List<GetListSubmissionFieldValueDto>> FindFieldValues(FindSubmissionFieldValueDto dto)
        {
            var submissionIds = ApplyFilter(dto, dto.StatusCheck, dto.StatusQa, false, dto.DocumentFieldId).Select(s => s.Id);
            var query = _shDbContext.SubmissionDatas
                .AsNoTracking()
                .Where(sd => sd.ProjectId == dto.ProjectId && sd.DocumentTypeId == dto.DocumentTypeId && sd.DocumentFieldId == dto.DocumentFieldId
                    && sd.Data != String.Empty && submissionIds.Contains(sd.SubmissionId));

            var keyword = dto.Keyword?.Trim();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(sd => sd.Data.Contains(keyword));
            }

            return query
                .GroupBy(sd => sd.Data)
                .Select(g => new GetListSubmissionFieldValueDto
                {
                    Value = g.Key,
                    SubmissionCount = g.Count(),
                })
                .OrderBy(v => v.Value)
                .Take(SubmissionConst.FIELD_VALUE_MAX_ITEMS)
                .ToListAsync();
        }

        public async Task<ViewSubmissionTreeDto> FindTree(ISubmissionFilter filter)
        {
            var boxCounts = await ApplyFilter(filter, filter.StatusCheck, filter.StatusQa)
                .GroupBy(s => new { s.ProfileRange, s.Box })
                .Select(g => new
                {
                    g.Key.ProfileRange,
                    g.Key.Box,
                    Count = new SubmissionTreeCountDto
                    {
                        Total = g.Count(),
                        TotalNotChecked = g.Count(s => s.StatusCheck == SubmissionConst.NOT_CHECKED),
                        TotalChecked = g.Count(s => s.StatusCheck == SubmissionConst.CHECKED),
                        TotalReturned = g.Count(s => s.StatusCheck == SubmissionConst.RETURNED),
                        TotalQaPassed = g.Count(s => s.StatusQa == SubmissionConst.QA_PASSED),
                        TotalQaFailed = g.Count(s => s.StatusQa == SubmissionConst.QA_FAILED),
                    },
                })
                .OrderBy(b => b.ProfileRange)
                .ThenBy(b => b.Box)
                .ToListAsync();

            var profileRanges = boxCounts
                .GroupBy(b => b.ProfileRange)
                .Select(g => new SubmissionProfileRangeNodeDto
                {
                    ProfileRange = g.Key,
                    Count = SumTreeCounts(g.Select(b => b.Count)),
                    Boxes = g.Select(b => new SubmissionBoxNodeDto { Box = b.Box, Count = b.Count }).ToList(),
                })
                .ToList();

            return new ViewSubmissionTreeDto
            {
                ProfileRanges = profileRanges,
                Total = SumTreeCounts(profileRanges.Select(p => p.Count)),
            };
        }

        public async Task<BaseResponsePagingDto<GetListProjectSubmissionLogDto>> FindProjectLogPaging(FindPagingProjectSubmissionLogDto dto)
        {
            var query = _shDbContext.SubmissionLogs
                .AsNoTracking()
                .Where(l => l.ProjectId == dto.ProjectId && l.DocumentTypeId == dto.DocumentTypeId);

            var userId = dto.UserId?.Trim();
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(l => l.UserId == userId);
            }
            if (dto.OldStatus.HasValue)
            {
                query = query.Where(l => l.OldStatus == dto.OldStatus.Value);
            }
            if (dto.FromDate.HasValue)
            {
                var fromDate = dto.FromDate.Value.Date;
                query = query.Where(l => l.CreatedDate >= fromDate);
            }
            if (dto.ToDate.HasValue)
            {
                var toDateExclusive = dto.ToDate.Value.Date.AddDays(1);
                query = query.Where(l => l.CreatedDate < toDateExclusive);
            }

            var totalItems = await query.CountAsync();
            var logs = await (from l in query.OrderByDescending(l => l.CreatedDate).ThenByDescending(l => l.Id).Paging(dto)
                              let submission = _shDbContext.Submissions.IgnoreQueryFilters().FirstOrDefault(s => s.Id == l.SubmissionId)
                              select new GetListProjectSubmissionLogDto
                              {
                                  Id = l.Id,
                                  SubmissionId = l.SubmissionId,
                                  FileName = submission != null ? submission.FileName : String.Empty,
                                  ProfileRange = submission != null ? submission.ProfileRange : String.Empty,
                                  Box = submission != null ? submission.Box : String.Empty,
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

            return new BaseResponsePagingDto<GetListProjectSubmissionLogDto>
            {
                Items = logs,
                TotalItems = totalItems,
            };
        }

        public async Task<ViewSubmissionSummaryDto> FindSummary(int projectId, int documentTypeId)
        {
            var summary = await _shDbContext.Submissions
                .AsNoTracking()
                .Where(s => s.ProjectId == projectId && s.DocumentTypeId == documentTypeId)
                .GroupBy(_ => 1)
                .Select(g => new ViewSubmissionSummaryDto
                {
                    TotalSubmissions = g.Count(),
                    TotalNotChecked = g.Count(s => s.StatusCheck == SubmissionConst.NOT_CHECKED),
                    TotalChecked = g.Count(s => s.StatusCheck == SubmissionConst.CHECKED),
                    TotalReturned = g.Count(s => s.StatusCheck == SubmissionConst.RETURNED),
                    TotalQaNotReviewed = g.Count(s => s.StatusQa == SubmissionConst.QA_NOT_REVIEWED),
                    TotalQaPassed = g.Count(s => s.StatusQa == SubmissionConst.QA_PASSED),
                    TotalQaFailed = g.Count(s => s.StatusQa == SubmissionConst.QA_FAILED),
                })
                .SingleOrDefaultAsync() ?? new ViewSubmissionSummaryDto();

            var isChargeableByFieldId = await (from pf in _shDbContext.ProjectDocumentTypeDocumentFields.AsNoTracking()
                                               join d in _shDbContext.DocumentFields.AsNoTracking() on pf.DocumentFieldId equals d.Id
                                               where pf.ProjectId == projectId && pf.DocumentTypeId == documentTypeId
                                               select new { d.Id, d.IsChargeable })
                .ToDictionaryAsync(d => d.Id, d => d.IsChargeable);

            var filledCellCounts = await (from sd in _shDbContext.SubmissionDatas.AsNoTracking()
                                          join s in _shDbContext.Submissions.AsNoTracking().Where(s => s.ProjectId == projectId && s.DocumentTypeId == documentTypeId)
                                              on sd.SubmissionId equals s.Id
                                          where sd.ProjectId == projectId && sd.DocumentTypeId == documentTypeId && sd.Data != String.Empty
                                          group sd by new { sd.DocumentFieldId, s.StatusCheck } into g
                                          select new { g.Key.DocumentFieldId, g.Key.StatusCheck, FilledCells = g.Count() })
                .ToListAsync();
            var configuredCellCounts = filledCellCounts.Where(c => isChargeableByFieldId.ContainsKey(c.DocumentFieldId)).ToList();

            summary.CheckedPercent = summary.TotalSubmissions == 0 ? 0 : Math.Round(summary.TotalChecked * 100m / summary.TotalSubmissions, 2);
            summary.TotalFields = isChargeableByFieldId.Count;
            summary.TotalChargeableFields = isChargeableByFieldId.Count(f => f.Value);
            summary.TotalFilledCells = configuredCellCounts.Sum(c => c.FilledCells);
            summary.TotalChargeableFilledCellsOfChecked = configuredCellCounts
                .Where(c => isChargeableByFieldId[c.DocumentFieldId] && c.StatusCheck == SubmissionConst.CHECKED)
                .Sum(c => c.FilledCells);
            return summary;
        }

        private static SubmissionTreeCountDto SumTreeCounts(IEnumerable<SubmissionTreeCountDto> counts)
        {
            var countList = counts.ToList();
            return new SubmissionTreeCountDto
            {
                Total = countList.Sum(c => c.Total),
                TotalNotChecked = countList.Sum(c => c.TotalNotChecked),
                TotalChecked = countList.Sum(c => c.TotalChecked),
                TotalReturned = countList.Sum(c => c.TotalReturned),
                TotalQaPassed = countList.Sum(c => c.TotalQaPassed),
                TotalQaFailed = countList.Sum(c => c.TotalQaFailed),
            };
        }

        private async Task FillFieldsAndPositionAsync(ViewSubmissionDetailDto detail, ISubmissionFilter filter, bool includePosition)
        {
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

            if (detail.StatusQa == SubmissionConst.QA_FAILED)
            {
                detail.QaWrongDocumentFieldIds = await _shDbContext.SubmissionQaLogFields
                    .AsNoTracking()
                    .Where(f => f.SubmissionQaLogId == _shDbContext.SubmissionQaLogs
                        .Where(l => l.SubmissionId == detail.Id)
                        .OrderByDescending(l => l.CreatedDate)
                        .ThenByDescending(l => l.Id)
                        .Select(l => l.Id)
                        .FirstOrDefault())
                    .OrderBy(f => f.DocumentFieldId)
                    .Select(f => f.DocumentFieldId)
                    .ToListAsync();
            }

            if (includePosition)
            {
                var counts = await ApplyFilter(filter, filter.StatusCheck, filter.StatusQa)
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
        }

        private IQueryable<Submission> ApplyFilter(ISubmissionFilter filter, int? statusCheck, int? statusQa, bool includeKeyword = true,
            int? excludedDocumentFieldId = null)
        {
            var query = _shDbContext.Submissions
                .AsNoTracking()
                .Where(s => s.ProjectId == filter.ProjectId && s.DocumentTypeId == filter.DocumentTypeId);

            if (statusCheck.HasValue)
            {
                query = query.Where(s => s.StatusCheck == statusCheck.Value);
            }

            if (statusQa.HasValue)
            {
                query = query.Where(s => s.StatusQa == statusQa.Value);
            }

            var keyword = filter.Keyword?.Trim();
            if (includeKeyword && !string.IsNullOrEmpty(keyword))
            {
                query = query.Where(s => s.FileName.Contains(keyword) || s.ProfileRange.Contains(keyword) || s.Box.Contains(keyword));
            }

            query = ApplyFieldFilter(query, filter.DocumentFieldId1, filter.FieldValue1, excludedDocumentFieldId);
            return ApplyFieldFilter(query, filter.DocumentFieldId2, filter.FieldValue2, excludedDocumentFieldId);
        }

        private IQueryable<Submission> ApplyFieldFilter(IQueryable<Submission> query, int? documentFieldId, string? fieldValue, int? excludedDocumentFieldId)
        {
            var value = fieldValue?.Trim();
            if (!documentFieldId.HasValue || string.IsNullOrEmpty(value) || documentFieldId == excludedDocumentFieldId)
            {
                return query;
            }

            var fieldId = documentFieldId.Value;
            return query.Where(s => _shDbContext.SubmissionDatas.Any(sd => sd.SubmissionId == s.Id && sd.DocumentFieldId == fieldId && sd.Data == value));
        }
    }
}
