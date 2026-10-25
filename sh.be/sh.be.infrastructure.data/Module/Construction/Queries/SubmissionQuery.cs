using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class SubmissionQuery : BaseQuery, ISubmissionQuery
    {
        private const string ExportDateFormat = "dd/MM/yyyy HH:mm:ss";
        private static readonly IReadOnlyList<string> ExportFixedHeaders =
            ["STT", "Tên file", "Hồ sơ", "Hộp", "Đường dẫn", "Trạng thái kiểm tra", "Người kiểm tra", "Thời gian kiểm tra", "Trạng thái QA", "Lý do QA",
                "Người sửa cuối", "Ngày sửa cuối"];
        private static readonly IReadOnlyDictionary<int, string> CheckStatusNames = new Dictionary<int, string>
        {
            { SubmissionConst.NOT_CHECKED, "Chưa kiểm tra" },
            { SubmissionConst.CHECKED, "Đã kiểm tra" },
            { SubmissionConst.RETURNED, "Trả lại" },
        };
        private static readonly IReadOnlyDictionary<int, string> QaStatusNames = new Dictionary<int, string>
        {
            { SubmissionConst.QA_NOT_REVIEWED, "Chưa QA" },
            { SubmissionConst.QA_PASSED, "Đạt" },
            { SubmissionConst.QA_FAILED, "Không đạt" },
        };

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

        public async Task<ViewQaStatsSummaryDto> FindQaStatsSummary(FindQaStatsDto dto, DateTime fromDate, DateTime toDateExclusive)
        {
            var reviews = await FindQaReviewsAsync(dto.ProjectId, dto.DocumentTypeId, fromDate, toDateExclusive);
            var chargeableFieldIds = await FindConfiguredFieldIdsAsync(dto.ProjectId, dto.DocumentTypeId, true);

            var entryUserId = dto.EntryUserId?.Trim();
            var isAllEntryUsers = string.IsNullOrEmpty(entryUserId);
            var summary = BuildQaStats<ViewQaStatsSummaryDto>(
                reviews.Where(r => isAllEntryUsers || r.EntryUserId == entryUserId).ToList(),
                reviews.Where(r => r.IsFirstReview && (isAllEntryUsers || r.FirstEntryUserId == entryUserId)).ToList(),
                chargeableFieldIds);
            summary.ChargeableFieldCount = chargeableFieldIds.Count;
            return summary;
        }

        public async Task<List<GetListQaFieldStatsDto>> FindQaFieldStats(FindQaStatsDto dto, DateTime fromDate, DateTime toDateExclusive)
        {
            var entryUserId = dto.EntryUserId?.Trim();
            var reviews = (await FindQaReviewsAsync(dto.ProjectId, dto.DocumentTypeId, fromDate, toDateExclusive))
                .Where(r => string.IsNullOrEmpty(entryUserId) || r.EntryUserId == entryUserId)
                .ToList();
            var configuredFieldIds = await FindConfiguredFieldIdsAsync(dto.ProjectId, dto.DocumentTypeId, false);
            var wrongCountByFieldId = reviews
                .SelectMany(r => r.WrongDocumentFieldIds)
                .GroupBy(documentFieldId => documentFieldId)
                .ToDictionary(g => g.Key, g => g.Count());
            var documentFieldIds = configuredFieldIds.Union(wrongCountByFieldId.Keys).ToList();

            var documentFields = await _shDbContext.DocumentFields
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(d => documentFieldIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name, d.Field, d.IsChargeable, d.OrderListView, d.Deleted })
                .ToListAsync();

            return documentFields
                .OrderByDescending(d => wrongCountByFieldId.GetValueOrDefault(d.Id))
                .ThenBy(d => d.OrderListView)
                .ThenBy(d => d.Id)
                .Select(d =>
                {
                    var wrongCount = wrongCountByFieldId.GetValueOrDefault(d.Id);
                    return new GetListQaFieldStatsDto
                    {
                        DocumentFieldId = d.Id,
                        Name = d.Deleted ? d.Name + SubmissionConst.DELETED_FIELD_NAME_SUFFIX : d.Name,
                        Field = d.Field,
                        IsConfigured = configuredFieldIds.Contains(d.Id),
                        IsChargeable = d.IsChargeable,
                        ReviewCount = reviews.Count,
                        WrongCount = wrongCount,
                        WrongRate = ToPercent(wrongCount, reviews.Count),
                        AccuracyRate = ToPercent(reviews.Count - wrongCount, reviews.Count),
                    };
                })
                .ToList();
        }

        public async Task<BaseResponsePagingDto<GetListQaEntryUserStatsDto>> FindQaEntryUserStatsPaging(FindPagingQaEntryUserStatsDto dto, DateTime fromDate,
            DateTime toDateExclusive)
        {
            var reviews = await FindQaReviewsAsync(dto.ProjectId, dto.DocumentTypeId, fromDate, toDateExclusive);
            var chargeableFieldIds = await FindConfiguredFieldIdsAsync(dto.ProjectId, dto.DocumentTypeId, true);

            var entryUserIds = reviews.Select(r => r.EntryUserId)
                .Concat(reviews.Where(r => r.IsFirstReview).Select(r => r.FirstEntryUserId))
                .Distinct()
                .ToList();
            var knownUserIds = entryUserIds.Where(userId => userId != null).Select(userId => userId!).ToList();
            var userById = await _shDbContext.Users
                .AsNoTracking()
                .Where(u => knownUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.UserName, u.FullName });

            var entryUsers = entryUserIds
                .Select(userId =>
                {
                    var entryUser = BuildQaStats<GetListQaEntryUserStatsDto>(
                        reviews.Where(r => r.EntryUserId == userId).ToList(),
                        reviews.Where(r => r.IsFirstReview && r.FirstEntryUserId == userId).ToList(),
                        chargeableFieldIds);
                    var user = userId != null ? userById.GetValueOrDefault(userId) : null;
                    entryUser.UserId = userId;
                    entryUser.UserName = userId == null ? SubmissionConst.UNKNOWN_ENTRY_USER_NAME : user?.UserName;
                    entryUser.FullName = user?.FullName;
                    return entryUser;
                })
                .Where(u => string.IsNullOrEmpty(dto.Keyword)
                    || (u.UserName?.Contains(dto.Keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (u.FullName?.Contains(dto.Keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            var orderedEntryUsers = dto.OrderBy == SubmissionConst.QA_STATS_ORDER_ACCURACY
                ? entryUsers.OrderBy(u => u.FieldAccuracyRate)
                : entryUsers.OrderByDescending(u => u.ReviewCount);

            return new BaseResponsePagingDto<GetListQaEntryUserStatsDto>
            {
                Items = orderedEntryUsers
                    .ThenBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
                    .Skip(dto.GetSkip())
                    .Take(dto.PageSize)
                    .ToList(),
                TotalItems = entryUsers.Count,
            };
        }

        public async Task<List<GetListSubmissionTimelineDto>> FindTimeline(int submissionId)
        {
            var checkLogs = await _shDbContext.SubmissionLogs
                .AsNoTracking()
                .Where(l => l.SubmissionId == submissionId)
                .Select(l => new { l.Id, l.UserId, l.OldStatus, l.NewStatus, l.Note, l.ChangedDataCount, l.Description, l.CreatedDate })
                .ToListAsync();
            var qaLogs = await _shDbContext.SubmissionQaLogs
                .AsNoTracking()
                .Where(l => l.SubmissionId == submissionId)
                .Select(l => new { l.Id, l.UserId, l.NewStatusQa, l.Reason, l.Description, l.CreatedDate })
                .ToListAsync();

            var qaLogIds = qaLogs.Select(l => l.Id).ToList();
            var wrongFieldsByQaLogId = (await (from f in _shDbContext.SubmissionQaLogFields.AsNoTracking()
                                               join d in _shDbContext.DocumentFields.IgnoreQueryFilters().AsNoTracking() on f.DocumentFieldId equals d.Id
                                               where qaLogIds.Contains(f.SubmissionQaLogId)
                                               orderby d.OrderListView, d.Id
                                               select new
                                               {
                                                   f.SubmissionQaLogId,
                                                   d.Id,
                                                   Name = d.Deleted ? d.Name + SubmissionConst.DELETED_FIELD_NAME_SUFFIX : d.Name,
                                               })
                .ToListAsync())
                .ToLookup(f => f.SubmissionQaLogId, f => new SubmissionTimelineWrongFieldDto { DocumentFieldId = f.Id, Name = f.Name });

            var entryUserIdByQaLogId = await QueryQaLogEntryUsers(_shDbContext.SubmissionQaLogs.Where(l => l.SubmissionId == submissionId))
                .ToDictionaryAsync(e => e.QaLogId, e => e.EntryUserId);

            var userIds = checkLogs.Select(l => l.UserId)
                .Concat(qaLogs.Select(l => l.UserId))
                .Concat(entryUserIdByQaLogId.Values.Where(userId => userId != null).Select(userId => userId!))
                .Distinct()
                .ToList();
            var userById = await _shDbContext.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.UserName, u.FullName });

            var checkEvents = checkLogs.Select(l => new GetListSubmissionTimelineDto
            {
                EventType = SubmissionConst.TIMELINE_EVENT_CHECK,
                LogId = l.Id,
                CreatedDate = l.CreatedDate,
                UserId = l.UserId,
                UserName = userById.GetValueOrDefault(l.UserId)?.UserName,
                FullName = userById.GetValueOrDefault(l.UserId)?.FullName,
                Description = l.Description,
                OldStatusCheck = l.OldStatus,
                NewStatusCheck = l.NewStatus,
                Note = l.Note,
                ChangedDataCount = l.ChangedDataCount,
            });
            var qaEvents = qaLogs.Select(l =>
            {
                var entryUserId = entryUserIdByQaLogId[l.Id];
                return new GetListSubmissionTimelineDto
                {
                    EventType = SubmissionConst.TIMELINE_EVENT_QA,
                    LogId = l.Id,
                    CreatedDate = l.CreatedDate,
                    UserId = l.UserId,
                    UserName = userById.GetValueOrDefault(l.UserId)?.UserName,
                    FullName = userById.GetValueOrDefault(l.UserId)?.FullName,
                    Description = l.Description,
                    NewStatusQa = l.NewStatusQa,
                    Reason = l.Reason,
                    WrongFields = wrongFieldsByQaLogId[l.Id].ToList(),
                    EntryUserId = entryUserId,
                    EntryUserName = entryUserId == null ? SubmissionConst.UNKNOWN_ENTRY_USER_NAME : userById.GetValueOrDefault(entryUserId)?.UserName,
                };
            });

            return checkEvents
                .Concat(qaEvents)
                .OrderBy(e => e.CreatedDate)
                .ThenBy(e => e.EventType == SubmissionConst.TIMELINE_EVENT_CHECK ? 0 : 1)
                .ThenBy(e => e.LogId)
                .Take(SubmissionConst.TIMELINE_MAX_EVENTS)
                .ToList();
        }

        public async Task<SubmissionExportTableDto> FindExportTable(ISubmissionFilter filter)
        {
            var query = ApplyFilter(filter, filter.StatusCheck, filter.StatusQa);
            if (await query.CountAsync() > SubmissionConst.EXPORT_MAX_ROWS)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorExportTooManyRows,
                    $"Số hồ sơ cần xuất vượt quá {SubmissionConst.EXPORT_MAX_ROWS}, hãy lọc bớt");
            }

            var submissions = await (from s in query.OrderBy(s => s.Id)
                                     let lastLog = _shDbContext.SubmissionLogs
                                         .Where(l => l.SubmissionId == s.Id)
                                         .OrderByDescending(l => l.CreatedDate)
                                         .ThenByDescending(l => l.Id)
                                         .FirstOrDefault()
                                     select new
                                     {
                                         s.Id,
                                         s.FileName,
                                         s.ProfileRange,
                                         s.Box,
                                         s.FileUrlS3Object,
                                         s.StatusCheck,
                                         CheckedBy = lastLog != null ? lastLog.UserId : null,
                                         CheckedDate = lastLog != null ? (DateTime?)lastLog.CreatedDate : null,
                                         s.StatusQa,
                                         s.QaReason,
                                         s.ModifiedBy,
                                         s.ModifiedDate,
                                     })
                .ToListAsync();

            var documentFields = await (from pf in _shDbContext.ProjectDocumentTypeDocumentFields.AsNoTracking()
                                        join d in _shDbContext.DocumentFields.AsNoTracking() on pf.DocumentFieldId equals d.Id
                                        where pf.ProjectId == filter.ProjectId && pf.DocumentTypeId == filter.DocumentTypeId && d.IsShowTableView
                                        orderby d.OrderTableView, d.Id
                                        select new { d.Id, d.Name })
                .ToListAsync();
            var documentFieldIds = documentFields.Select(d => d.Id).ToList();

            var submissionIds = query.Select(s => s.Id);
            var dataByCell = _shDbContext.SubmissionDatas
                .AsNoTracking()
                .Where(sd => sd.ProjectId == filter.ProjectId && sd.DocumentTypeId == filter.DocumentTypeId
                    && documentFieldIds.Contains(sd.DocumentFieldId) && submissionIds.Contains(sd.SubmissionId))
                .Select(sd => new { sd.SubmissionId, sd.DocumentFieldId, sd.Data })
                .ToDictionary(sd => (sd.SubmissionId, sd.DocumentFieldId), sd => sd.Data);

            var userIds = submissions.Select(s => s.CheckedBy).Concat(submissions.Select(s => s.ModifiedBy))
                .Where(userId => userId != null).Select(userId => userId!).Distinct().ToList();
            var userNameById = await _shDbContext.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Id);

            return new SubmissionExportTableDto
            {
                ProjectName = await _shDbContext.Projects.AsNoTracking().Where(p => p.Id == filter.ProjectId).Select(p => p.Name).FirstOrDefaultAsync()
                    ?? String.Empty,
                DocumentTypeName = await _shDbContext.DocumentTypes.AsNoTracking().Where(d => d.Id == filter.DocumentTypeId).Select(d => d.Name).FirstOrDefaultAsync()
                    ?? String.Empty,
                Headers = ExportFixedHeaders.Concat(documentFields.Select(d => d.Name)).ToList(),
                Rows = submissions.Select((s, index) => new List<string>
                    {
                        (index + 1).ToString(),
                        s.FileName,
                        s.ProfileRange,
                        s.Box,
                        s.FileUrlS3Object,
                        CheckStatusNames.GetValueOrDefault(s.StatusCheck, String.Empty),
                        s.CheckedBy != null ? userNameById.GetValueOrDefault(s.CheckedBy, s.CheckedBy) : String.Empty,
                        s.CheckedDate?.ToString(ExportDateFormat) ?? String.Empty,
                        QaStatusNames.GetValueOrDefault(s.StatusQa, String.Empty),
                        s.QaReason ?? String.Empty,
                        s.ModifiedBy != null ? userNameById.GetValueOrDefault(s.ModifiedBy, s.ModifiedBy) : String.Empty,
                        s.ModifiedDate?.ToString(ExportDateFormat) ?? String.Empty,
                    }
                    .Concat(documentFieldIds.Select(fieldId => dataByCell.GetValueOrDefault((s.Id, fieldId), String.Empty)))
                    .ToList())
                    .ToList(),
            };
        }

        public async Task<SubmissionExportNameDto?> FindExportName(int projectId, int documentTypeId)
        {
            return await (from p in _shDbContext.Projects.AsNoTracking()
                          from d in _shDbContext.DocumentTypes.AsNoTracking()
                          where p.Id == projectId && d.Id == documentTypeId
                          select new SubmissionExportNameDto { ProjectName = p.Name, DocumentTypeName = d.Name })
                .FirstOrDefaultAsync();
        }

        private async Task<List<QaReview>> FindQaReviewsAsync(int projectId, int documentTypeId, DateTime fromDate, DateTime toDateExclusive)
        {
            var qaLogs = from q in _shDbContext.SubmissionQaLogs.AsNoTracking()
                         join s in _shDbContext.Submissions.AsNoTracking() on q.SubmissionId equals s.Id
                         where q.ProjectId == projectId && q.DocumentTypeId == documentTypeId && q.CreatedDate >= fromDate && q.CreatedDate < toDateExclusive
                         select q;

            var reviewLogs = await (from q in qaLogs
                                    join e in QueryQaLogEntryUsers(qaLogs) on q.Id equals e.QaLogId
                                    select new
                                    {
                                        q.Id,
                                        q.SubmissionId,
                                        q.NewStatusQa,
                                        e.EntryUserId,
                                        IsFirstReview = !_shDbContext.SubmissionQaLogs.Any(o => o.SubmissionId == q.SubmissionId && o.Id < q.Id),
                                    })
                .ToListAsync();

            var wrongFieldIdsByQaLogId = (await (from f in _shDbContext.SubmissionQaLogFields.AsNoTracking()
                                                 join q in qaLogs on f.SubmissionQaLogId equals q.Id
                                                 select new { f.SubmissionQaLogId, f.DocumentFieldId })
                .ToListAsync())
                .ToLookup(f => f.SubmissionQaLogId, f => f.DocumentFieldId);

            return reviewLogs
                .GroupBy(l => l.SubmissionId)
                .Select(g =>
                {
                    var lastLog = g.MaxBy(l => l.Id)!;
                    var firstLog = g.FirstOrDefault(l => l.IsFirstReview);
                    return new QaReview(g.Key, lastLog.NewStatusQa, lastLog.EntryUserId, wrongFieldIdsByQaLogId[lastLog.Id].ToList(),
                        firstLog != null, firstLog?.NewStatusQa == SubmissionConst.QA_PASSED, firstLog?.EntryUserId);
                })
                .ToList();
        }

        private IQueryable<QaLogEntryUser> QueryQaLogEntryUsers(IQueryable<SubmissionQaLog> qaLogs)
        {
            return qaLogs.Select(q => new QaLogEntryUser
            {
                QaLogId = q.Id,
                EntryUserId = _shDbContext.SubmissionLogs
                    .Where(l => l.SubmissionId == q.SubmissionId && l.CreatedDate <= q.CreatedDate)
                    .OrderByDescending(l => l.CreatedDate)
                    .ThenByDescending(l => l.Id)
                    .Select(l => l.UserId)
                    .FirstOrDefault(),
            });
        }

        private async Task<HashSet<int>> FindConfiguredFieldIdsAsync(int projectId, int documentTypeId, bool isChargeableOnly)
        {
            return (await (from pf in _shDbContext.ProjectDocumentTypeDocumentFields.AsNoTracking()
                           join d in _shDbContext.DocumentFields.AsNoTracking() on pf.DocumentFieldId equals d.Id
                           where pf.ProjectId == projectId && pf.DocumentTypeId == documentTypeId && (!isChargeableOnly || d.IsChargeable)
                           select d.Id)
                .ToListAsync())
                .ToHashSet();
        }

        private static TStats BuildQaStats<TStats>(IReadOnlyCollection<QaReview> reviews, IReadOnlyCollection<QaReview> firstReviews,
            IReadOnlySet<int> chargeableFieldIds) where TStats : QaStatsMetricDto, new()
        {
            var reviewedDocumentCount = reviews.Select(r => r.SubmissionId).Distinct().Count();
            var failedCount = reviews.Count(r => r.NewStatusQa == SubmissionConst.QA_FAILED);
            var firstPassCount = firstReviews.Count(r => r.IsFirstPassed);
            var wrongFieldCount = reviews.Sum(r => r.WrongDocumentFieldIds.Count(chargeableFieldIds.Contains));
            var reviewedCellCount = reviews.Count * chargeableFieldIds.Count;

            return new TStats
            {
                ReviewCount = reviews.Count,
                PassedCount = reviews.Count(r => r.NewStatusQa == SubmissionConst.QA_PASSED),
                FailedCount = failedCount,
                ReviewedDocumentCount = reviewedDocumentCount,
                FirstReviewCount = firstReviews.Count,
                FirstPassCount = firstPassCount,
                FirstPassRate = ToPercent(firstPassCount, firstReviews.Count),
                WrongFieldCount = wrongFieldCount,
                ReviewedCellCount = reviewedCellCount,
                FieldAccuracyRate = ToPercent(reviewedCellCount - wrongFieldCount, reviewedCellCount),
                AvgReturnPerDocument = reviewedDocumentCount == 0 ? 0 : Math.Round((decimal)failedCount / reviewedDocumentCount, 2),
            };
        }

        private static decimal ToPercent(int value, int total)
        {
            return total == 0 ? 0 : Math.Round(value * 100m / total, 2);
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

        private record QaReview(int SubmissionId, int NewStatusQa, string? EntryUserId, List<int> WrongDocumentFieldIds, bool IsFirstReview,
            bool IsFirstPassed, string? FirstEntryUserId);

        private class QaLogEntryUser
        {
            public int QaLogId { get; init; }
            public string? EntryUserId { get; init; }
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
