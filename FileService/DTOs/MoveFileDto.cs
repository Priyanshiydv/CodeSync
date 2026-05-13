using System.ComponentModel.DataAnnotations;

namespace FileService.DTOs
{
    /// <summary>
    /// Data received to move a file to a new path.
    /// </summary>
    public class MoveFileDto
    {
        [Required]
        public string NewPath { get; set; } = string.Empty;
    }
}