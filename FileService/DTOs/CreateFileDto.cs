using System.ComponentModel.DataAnnotations;

namespace FileService.DTOs
{
    /// <summary>
    /// Data received from client to create a new file.
    /// </summary>
    public class CreateFileDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Path { get; set; } = string.Empty;

        public string Language { get; set; } = "plaintext";

        public string Content { get; set; } = string.Empty;

        [Required]
        public int ProjectId { get; set; }
    }
}