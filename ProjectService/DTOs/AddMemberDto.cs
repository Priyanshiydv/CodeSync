using System.ComponentModel.DataAnnotations;

namespace ProjectService.DTOs
{
    /// <summary>
    /// Data received to add a member to a project.
    /// </summary>
    public class AddMemberDto
    {
        [Required]
        public int UserId { get; set; }

        // OWNER, EDITOR, VIEWER
        [Required]
        [RegularExpression("^(OWNER|EDITOR|VIEWER)$",
            ErrorMessage = "Role must be OWNER, EDITOR, or VIEWER")]
        public string Role { get; set; } = "VIEWER";
    }
}