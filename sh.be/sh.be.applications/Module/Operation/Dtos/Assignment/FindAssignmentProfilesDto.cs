using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class FindAssignmentProfilesDto : FindAssignmentTreeDto
    {
        [MaxLength(100)]
        public string? Box { get; set; }
    }
}
