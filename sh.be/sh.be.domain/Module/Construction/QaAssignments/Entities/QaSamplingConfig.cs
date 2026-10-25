using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Db;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.QaAssignments.Entities
{
    [Table(nameof(QaSamplingConfig), Schema = DbSchemas.Construction)]
    [Index(nameof(ProjectId), IsUnique = true, Name = UniqueProjectIndexName)]
    public class QaSamplingConfig : ISoftDeleted
    {
        public const string UniqueProjectIndexName = $"IX_{nameof(QaSamplingConfig)}_{nameof(ProjectId)}";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int ProjectId { get; private set; }
        public int SampleSize { get; private set; }
        [Precision(5, 2)]
        public decimal ThresholdPercent { get; private set; }
        [MaxLength(QaAssignmentConst.STRATEGY_CODE_MAX_LENGTH)]
        public string StrategyCode { get; private set; } = String.Empty;
        [MaxLength(QaAssignmentConst.STRATEGY_PARAMS_MAX_LENGTH)]
        public string? StrategyParamsJson { get; private set; }
        public bool StopEarlyOnReject { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private QaSamplingConfig()
        {
        }

        public static QaSamplingConfig Default(int projectId)
        {
            return new QaSamplingConfig
            {
                ProjectId = projectId,
                SampleSize = QaAssignmentConst.DEFAULT_SAMPLE_SIZE,
                ThresholdPercent = QaAssignmentConst.DEFAULT_THRESHOLD_PERCENT,
                StrategyCode = QaAssignmentConst.DEFAULT_STRATEGY,
                StopEarlyOnReject = QaAssignmentConst.DEFAULT_STOP_EARLY,
            };
        }

        public static QaSamplingConfig Create(int projectId, int sampleSize, decimal thresholdPercent, string strategyCode, string? strategyParamsJson,
            bool stopEarlyOnReject, string? createdBy, DateTime createdDate)
        {
            var config = new QaSamplingConfig
            {
                ProjectId = projectId,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
            config.SetValues(sampleSize, thresholdPercent, strategyCode, strategyParamsJson, stopEarlyOnReject);
            return config;
        }

        public void Update(int sampleSize, decimal thresholdPercent, string strategyCode, string? strategyParamsJson, bool stopEarlyOnReject,
            string? modifiedBy, DateTime modifiedDate)
        {
            SetValues(sampleSize, thresholdPercent, strategyCode, strategyParamsJson, stopEarlyOnReject);
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        private void SetValues(int sampleSize, decimal thresholdPercent, string strategyCode, string? strategyParamsJson, bool stopEarlyOnReject)
        {
            if (sampleSize < 1 || sampleSize > QaAssignmentConst.SAMPLE_SIZE_MAX)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Cỡ mẫu phải từ 1 đến {QaAssignmentConst.SAMPLE_SIZE_MAX}");
            }
            if (thresholdPercent < 0 || thresholdPercent > QaAssignmentConst.THRESHOLD_MAX || decimal.Round(thresholdPercent, 2) != thresholdPercent)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Ngưỡng sai phải từ 0 đến {QaAssignmentConst.THRESHOLD_MAX}, tối đa 2 chữ số thập phân");
            }
            SampleSize = sampleSize;
            ThresholdPercent = thresholdPercent;
            StrategyCode = strategyCode;
            StrategyParamsJson = strategyParamsJson;
            StopEarlyOnReject = stopEarlyOnReject;
        }
    }
}
