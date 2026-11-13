namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class UpdateMemberPriceItemDto
    {
        public int MemberId { get; set; }
        public decimal? CompanyPrice { get; set; }
        public decimal? ActualPrice { get; set; }
    }
}
