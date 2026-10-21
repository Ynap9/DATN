using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Services.Check.Models;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.Services.Check
{
    public static class SubmissionCheckPolicy
    {
        public static SubmissionCheckPlan BuildPlan(Submission submission, IReadOnlyList<SubmissionDataInput> inputs,
            IReadOnlyList<ProjectDocumentTypeDocumentField> mappings, IReadOnlyList<SubmissionData> existingDatas,
            string modifiedBy, DateTime modifiedDate)
        {
            if (inputs.GroupBy(input => input.DocumentFieldId).Any(group => group.Count() > 1))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionDataDuplicateField);
            }

            var mappingByFieldId = mappings.ToDictionary(mapping => mapping.DocumentFieldId);
            if (inputs.Any(input => !mappingByFieldId.ContainsKey(input.DocumentFieldId)))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentFieldNotInProject);
            }

            var datasByFieldId = existingDatas
                .GroupBy(data => data.DocumentFieldId)
                .ToDictionary(group => group.Key, group => group.OrderBy(data => data.Id).First());

            var datasToUpdate = new List<SubmissionData>();
            var datasToInsert = new List<SubmissionData>();
            var changes = new List<SubmissionDataChange>();

            foreach (var input in inputs)
            {
                if (datasByFieldId.TryGetValue(input.DocumentFieldId, out var data))
                {
                    var oldData = data.Data;
                    if (data.ChangeData(input.Data, modifiedBy, modifiedDate))
                    {
                        datasToUpdate.Add(data);
                        changes.Add(new SubmissionDataChange(input.DocumentFieldId, oldData, data.Data));
                    }
                    continue;
                }

                var newData = input.Data.Trim();
                if (newData.Length == 0)
                {
                    continue;
                }

                var mapping = mappingByFieldId[input.DocumentFieldId];
                datasToInsert.Add(submission.CreateData(input.DocumentFieldId, newData, mapping.OrderFieldImport, String.Empty, modifiedBy, modifiedDate));
                changes.Add(new SubmissionDataChange(input.DocumentFieldId, String.Empty, newData));
            }

            return new SubmissionCheckPlan(datasToUpdate, datasToInsert, changes);
        }
    }
}
