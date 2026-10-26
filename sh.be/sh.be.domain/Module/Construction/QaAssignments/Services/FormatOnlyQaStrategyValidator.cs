using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace sh.be.domain.Module.Construction.QaAssignments.Services
{
    public class FormatOnlyQaStrategyValidator : IQaStrategyValidator
    {
        public void Validate(string strategyCode, string? paramsJson)
        {
            if (!Regex.IsMatch(strategyCode, QaAssignmentConst.STRATEGY_CODE_PATTERN))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest,
                    $"Mã thuật toán chỉ gồm chữ in hoa, số, dấu \"_\" và tối đa {QaAssignmentConst.STRATEGY_CODE_MAX_LENGTH} ký tự");
            }
            if (paramsJson == null)
            {
                return;
            }
            if (paramsJson.Length > QaAssignmentConst.STRATEGY_PARAMS_MAX_LENGTH)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest,
                    $"Tham số thuật toán tối đa {QaAssignmentConst.STRATEGY_PARAMS_MAX_LENGTH} ký tự");
            }

            bool isObject;
            try
            {
                using var document = JsonDocument.Parse(paramsJson);
                isObject = document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException)
            {
                isObject = false;
            }
            if (!isObject)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Tham số thuật toán phải là một JSON object");
            }
        }
    }
}
