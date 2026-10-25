namespace sh.be.domain.Module.Construction.QaAssignments.Services
{
    public interface IQaStrategyValidator
    {
        void Validate(string strategyCode, string? paramsJson);
    }
}
