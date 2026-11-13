using Microsoft.EntityFrameworkCore;
using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Operation.Assignments.Entities
{
    [Table(nameof(AssignmentTicketItem), Schema = DbSchemas.Operation)]
    [Index(nameof(AssignmentTicketId), Name = $"IX_{nameof(AssignmentTicketItem)}_{nameof(AssignmentTicketId)}")]
    public class AssignmentTicketItem : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int AssignmentTicketId { get; private set; }
        [MaxLength(100)]
        public string Box { get; private set; } = String.Empty;
        [MaxLength(100)]
        public string? ProfileRange { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private AssignmentTicketItem()
        {
        }

        public static AssignmentTicketItem Create(int assignmentTicketId, string box, string profileRange)
        {
            return new AssignmentTicketItem
            {
                AssignmentTicketId = assignmentTicketId,
                Box = box.Trim(),
                ProfileRange = profileRange.Trim(),
            };
        }
    }
}
