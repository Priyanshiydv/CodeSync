using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectService.Models
{
    /// <summary>
    /// Join table managing project membership.
    /// Links users to projects with assigned roles.
    /// </summary>
    public class ProjectMember
    {
        [Key]
        public int MemberId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Role: OWNER, EDITOR, VIEWER
        /// </summary>
        [Required]
        public string Role { get; set; } = "VIEWER";

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [JsonIgnore]
        public Project? Project { get; set; }
    }
}