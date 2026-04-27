namespace CollabService.Models
{
    /// <summary>
    /// Represents a single edit operation for OT transformation.
    /// An operation is either Insert, Delete, or Retain.
    /// Case study §2.6 — OT ensures convergent consistency
    /// under concurrent writes from multiple participants.
    /// </summary>
    public class EditOperation
    {
        // Operation type: "insert", "delete", "retain"
        public string Type { get; set; } = string.Empty;

        // Position in document where operation applies
        public int Position { get; set; }

        // Text to insert (only for insert operations)
        public string? Text { get; set; }

        // Number of characters to retain or delete
        public int Length { get; set; }

        // Who sent this operation
        public int UserId { get; set; }

        // Logical timestamp for ordering concurrent operations
        public int Revision { get; set; }
    }
}