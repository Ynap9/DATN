namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetListProgressQaUserDto : ProgressUserDto
    {
        public int ReviewCount { get; set; }
        public int ReviewDocumentCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
    }
}
