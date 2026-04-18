using System.ComponentModel.DataAnnotations;

namespace VersionService.DTOs
{
    /// <summary>
    /// Data received to tag a snapshot with a release label.
    /// </summary>
    public class TagSnapshotDto
    {
        [Required]
        public int SnapshotId { get; set; }

        // Semantic version tag e.g. v1.0.0
        [Required]
        public string Tag { get; set; } = string.Empty;
    }
}