using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Operation.Assignments.Entities
{
    [Table(nameof(AssignmentTicketItem), Schema = DbSchemas.Operation)]
    [Index(nameof(AssignmentTicketId), Name = $"IX_{nameof(AssignmentTicketItem)}_{nameof(AssignmentTicketId)}")]
    public class AssignmentTicketItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int AssignmentTicketId { get; private set; }
        [MaxLength(100)]
        public string Box { get; private set; } = String.Empty;
        [MaxLength(100)]
        public string? ProfileRange { get; private set; }

        private AssignmentTicketItem()
        {
        }

        public static AssignmentTicketItem Create(int assignmentTicketId, string box, string? profileRange)
        {
            var trimmedProfileRange = profileRange?.Trim();
            return new AssignmentTicketItem
            {
                AssignmentTicketId = assignmentTicketId,
                Box = box.Trim(),
                ProfileRange = String.IsNullOrEmpty(trimmedProfileRange) ? null : trimmedProfileRange,
            };
        }
    }
}
