namespace sh.be.applications.Module.Construction.Dtos.Project
{
    public class ViewProjectAcceptanceDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = String.Empty;
        public int Status { get; set; }
        public bool IsReady { get; set; }
        public bool CanForce { get; set; }
        public List<GetListProjectAcceptanceSummaryDto> DocumentTypes { get; set; } = new();
        public GetListProjectAcceptanceSummaryDto Total { get; set; } = new();
        public ViewProjectAcceptanceTransitionDto? LastTransition { get; set; }
    }
}
