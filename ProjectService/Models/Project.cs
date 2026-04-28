using System.ComponentModel.DataAnnotations;

namespace ProjectService.Models
{
    /// <summary>
    /// Represents a code project in the CodeSync platform.
    /// </summary>
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Visibility: PUBLIC or PRIVATE
        /// </summary>
        [Required]
        public string Visibility { get; set; } = "PUBLIC";

        // Nullable - only set if project is created from a template
        public int? TemplateId { get; set; }

        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Denormalised fields for efficient sorting on discovery feeds
        public int StarCount { get; set; } = 0;

        public int ForkCount { get; set; } = 0;

        // Navigation property
        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    }
}