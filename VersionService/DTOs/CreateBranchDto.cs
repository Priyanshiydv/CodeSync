using System.ComponentModel.DataAnnotations;

namespace VersionService.DTOs
{
    /// <summary>
    /// Data received to create a new branch from an existing snapshot.
    /// </summary>
    public class CreateBranchDto
    {
        [Required]
        public string BranchName { get; set; } = string.Empty;

        [Required]
        public int FromSnapshotId { get; set; }
    }
}