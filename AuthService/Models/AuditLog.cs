using System.ComponentModel.DataAnnotations;

namespace AuthService.Models
{
    /// <summary>
    /// Records all significant admin and user actions.
    /// Case study §2.4 — admin can view audit logs of all
    /// significant platform actions with actor and timestamp.
    /// </summary>
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        // Who performed the action
        [Required]
        public int ActorId { get; set; }

        public string ActorUsername { get; set; } = string.Empty;

        // What action was performed
        // e.g. SUSPEND_USER, DELETE_USER, CHANGE_ROLE,
        // DELETE_PROJECT, END_SESSION, CANCEL_JOB
        [Required]
        public string Action { get; set; } = string.Empty;

        // What entity was affected
        // e.g. "User", "Project", "Session"
        public string EntityType { get; set; } = string.Empty;

        // ID of the affected entity
        public string EntityId { get; set; } = string.Empty;

        // Human readable description of what happened
        public string Description { get; set; } = string.Empty;

        // IP address of the actor
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}