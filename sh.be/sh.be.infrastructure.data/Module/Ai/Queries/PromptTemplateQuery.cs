using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Ai.Dtos.PromptTemplate;
using sh.be.applications.Module.Ai.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Ai;

namespace sh.be.infrastructure.data.Module.Ai.Queries
{
    public class PromptTemplateQuery : BaseQuery, IPromptTemplateQuery
    {
        public PromptTemplateQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        // Màn sửa prompt: bản đang dùng, bản nháp, prompt và cấu hình mọi trường OCR hiện có theo bản đang xem (nháp, không có thì bản đang dùng)
        public async Task<ViewPromptEditorDto> FindEditor(int documentTypeId)
        {
            var documentTypeName = await _shDbContext.DocumentTypes.AsNoTracking()
                .Where(d => d.Id == documentTypeId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync();

            var templates = await _shDbContext.PromptTemplates.AsNoTracking()
                .Where(t => t.DocumentTypeId == documentTypeId
                    && (t.Status == PromptTemplateConst.STATUS_DRAFT || t.Status == PromptTemplateConst.STATUS_PUBLISHED))
                .Select(t => new
                {
                    Info = new PromptTemplateInfoDto
                    {
                        Id = t.Id,
                        Version = t.Version,
                        Status = t.Status,
                        Note = t.Note,
                        SourceVersion = _shDbContext.PromptTemplates.Where(s => s.Id == t.SourceTemplateId).Select(s => (int?)s.Version).FirstOrDefault(),
                        ModifiedByName = _shDbContext.Users.Where(u => u.Id == (t.ModifiedBy ?? t.CreatedBy)).Select(u => u.UserName).FirstOrDefault(),
                        ModifiedDate = t.ModifiedDate ?? t.CreatedDate,
                        PublishedByName = _shDbContext.Users.Where(u => u.Id == t.PublishedBy).Select(u => u.UserName).FirstOrDefault(),
                        PublishedDate = t.PublishedDate,
                    },
                    t.SystemPrompt,
                    t.InstructionPrompt,
                })
                .ToListAsync();
            var published = templates.FirstOrDefault(t => t.Info.Status == PromptTemplateConst.STATUS_PUBLISHED);
            var draft = templates.FirstOrDefault(t => t.Info.Status == PromptTemplateConst.STATUS_DRAFT);
            var editing = draft ?? published;
            var editingId = editing?.Info.Id;

            var fields = await (
                from field in _shDbContext.DocumentFields.AsNoTracking().Where(f => f.DocumentTypeId == documentTypeId && f.IsFieldOcr)
                join config in _shDbContext.PromptFieldConfigs.AsNoTracking().Where(c => c.PromptTemplateId == editingId)
                    on field.Id equals config.DocumentFieldId into fieldConfigs
                from config in fieldConfigs.DefaultIfEmpty()
                orderby field.OrderListView, field.Id
                select new PromptFieldEditorDto
                {
                    DocumentFieldId = field.Id,
                    Name = field.Name,
                    OutputKey = field.FieldNameOcr,
                    Description = config == null ? String.Empty : config.Description,
                    Example = config == null ? null : config.Example,
                    NormalizeRule = config == null ? PromptTemplateConst.NORMALIZE_NONE : config.NormalizeRule,
                    IsRequired = config != null && config.IsRequired,
                    IsActive = config == null || config.IsActive,
                    InTemplate = config != null,
                })
                .ToListAsync();

            return new ViewPromptEditorDto
            {
                DocumentTypeId = documentTypeId,
                DocumentTypeName = documentTypeName ?? String.Empty,
                Published = published?.Info,
                Draft = draft?.Info,
                EditingId = editingId,
                SystemPrompt = editing?.SystemPrompt ?? String.Empty,
                InstructionPrompt = editing?.InstructionPrompt ?? String.Empty,
                Fields = fields,
            };
        }

        // Mọi phiên bản chưa xoá của metadata, mới nhất trước
        public Task<List<GetListPromptTemplateVersionDto>> FindVersions(int documentTypeId)
        {
            return _shDbContext.PromptTemplates.AsNoTracking()
                .Where(t => t.DocumentTypeId == documentTypeId)
                .OrderByDescending(t => t.Version)
                .Select(t => new GetListPromptTemplateVersionDto
                {
                    Id = t.Id,
                    Version = t.Version,
                    Status = t.Status,
                    Note = t.Note,
                    SourceVersion = _shDbContext.PromptTemplates.Where(s => s.Id == t.SourceTemplateId).Select(s => (int?)s.Version).FirstOrDefault(),
                    CreatedByName = _shDbContext.Users.Where(u => u.Id == t.CreatedBy).Select(u => u.UserName).FirstOrDefault(),
                    CreatedDate = t.CreatedDate,
                    PublishedByName = _shDbContext.Users.Where(u => u.Id == t.PublishedBy).Select(u => u.UserName).FirstOrDefault(),
                    PublishedDate = t.PublishedDate,
                })
                .ToListAsync();
        }

        // Tên trường theo id, kể cả trường đã xoá (tên lúc còn), theo thứ tự danh sách
        public Task<List<string>> GetDocumentFieldNames(List<int> documentFieldIds)
        {
            return _shDbContext.DocumentFields.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(f => documentFieldIds.Contains(f.Id))
                .OrderBy(f => f.OrderListView)
                .ThenBy(f => f.Id)
                .Select(f => f.Name)
                .ToListAsync();
        }
    }
}
