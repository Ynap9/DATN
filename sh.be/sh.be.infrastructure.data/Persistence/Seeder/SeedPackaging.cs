using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Core.Entities;
using sh.be.domain.Module.Packaging.Entities;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Core;
using sh.be.shared.Utils;

namespace sh.be.infrastructure.data.Persistence.Seeder
{
    public static class SeedPackaging
    {
        private const string CreatedBy = "system";

        public static async Task SeedAsync(ShDbContext dbContext)
        {
            var createdDate = DateTimeUtils.GetVietnamTime();

            var existingCodeKeys = (await dbContext.PackageCodeValues
                    .Select(c => new { c.CodeGroup, c.Code })
                    .ToListAsync())
                .Select(c => (c.CodeGroup, c.Code))
                .ToHashSet();
            var newCodeValues = PackagingSeedData.CodeGroups
                .SelectMany(g => g.Values.Select((value, index) =>
                    PackageCodeValue.Create(g.CodeGroup, value.Code, value.Name, index + 1, CreatedBy, createdDate)))
                .Where(c => !existingCodeKeys.Contains((c.CodeGroup, c.Code)))
                .ToList();
            dbContext.PackageCodeValues.AddRange(newCodeValues);

            var schemas = await dbContext.PackageSchemas.ToListAsync();
            var newSchemas = PackagingSeedData.Schemas
                .Where(s => !schemas.Any(e => e.PackageType == s.PackageType && e.MetadataType == s.MetadataType))
                .Select(s => PackageSchema.Create(s.PackageType, s.MetadataType, s.Name, s.XsdFileName, s.MetadataFilePrefix,
                    CreatedBy, createdDate))
                .ToList();
            dbContext.PackageSchemas.AddRange(newSchemas);
            await dbContext.SaveChangesAsync();

            var schemaIds = schemas.Concat(newSchemas).ToDictionary(s => (s.PackageType, s.MetadataType), s => s.Id);
            var existingFieldKeys = (await dbContext.PackageSchemaFields
                    .Select(f => new { f.PackageSchemaId, f.ElementName })
                    .ToListAsync())
                .Select(f => (f.PackageSchemaId, f.ElementName))
                .ToHashSet();
            var newFields = PackagingSeedData.Schemas
                .SelectMany(s => s.Fields.Select((field, index) => PackageSchemaField.Create(schemaIds[(s.PackageType, s.MetadataType)],
                    field.ElementName, field.DisplayName, field.DataType, field.MaxLength, field.Requirement, field.ConditionElement,
                    field.ConditionValues, field.CodeGroup, field.AllowMultipleValues, field.Description, index + 1,
                    CreatedBy, createdDate)))
                .Where(f => !existingFieldKeys.Contains((f.PackageSchemaId, f.ElementName)))
                .ToList();

            if (newCodeValues.Count == 0 && newSchemas.Count == 0 && newFields.Count == 0)
            {
                return;
            }

            dbContext.PackageSchemaFields.AddRange(newFields);
            dbContext.AuditLogs.Add(AuditLog.Create(
                null,
                AuditLogConstants.ACTION_SEED_PACKAGING,
                AuditLogConstants.ACTION_TYPE_CREATE,
                $"Khởi tạo cấu hình đóng gói: {newCodeValues.Count} mã, {newSchemas.Count} schema, {newFields.Count} trường",
                null,
                null,
                nameof(SeedPackaging),
                null,
                createdDate));
            await dbContext.SaveChangesAsync();
        }
    }
}
