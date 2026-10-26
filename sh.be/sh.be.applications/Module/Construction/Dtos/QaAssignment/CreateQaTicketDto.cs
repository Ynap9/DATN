using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Construction.Dtos.QaAssignment
{
    public class CreateQaTicketDto
    {
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        [Required]
        public string QaUserId { get; set; } = String.Empty;
        public List<QaTicketItemDto>? Items { get; set; }
        public int? Count { get; set; }
    }
}
