using System.ComponentModel.DataAnnotations;

namespace CollabService.DTOs
{
    /// <summary>
    /// Data received to broadcast a code change to all session participants.
    /// </summary>
    public class BroadcastChangeDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int CursorLine { get; set; }
        public int CursorCol { get; set; }

        // ADD — OT operation fields
        // Type: "insert", "delete", "retain"
        public string OperationType { get; set; } = "full";

        // Position where the edit happened
        public int Position { get; set; }

        // Text that was inserted (for insert operations)
        public string? InsertedText { get; set; }

        // Number of chars deleted (for delete operations)
        public int DeletedLength { get; set; }

        // Client revision number — used for OT transformation
        // tells server how many operations client has seen
        public int Revision { get; set; }
    }
}