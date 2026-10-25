namespace sh.be.domain.Module.Construction.Projects.Services.Deadline
{
    public record ProjectDeadlineSettings(int WarningDays, bool IsMailEnabled, IReadOnlyList<string> DefaultedConfigNames);
}
