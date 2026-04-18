using System.ComponentModel.DataAnnotations;

namespace FileService.DTOs
{
    /// <summary>
    /// Data received to rename a file or folder.
    /// </summary>
    public class RenameFileDto
    {
        [Required]
        public string NewName { get; set; } = string.Empty;
    }
}