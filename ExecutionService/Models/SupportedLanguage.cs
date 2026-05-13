using System.ComponentModel.DataAnnotations;

namespace ExecutionService.Models
{
    /// <summary>
    /// Registry of supported programming languages
    /// with their Docker sandbox image tags and runtime versions.
    /// </summary>
    public class SupportedLanguage
    {
        [Key]
        public int LanguageId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // Docker image tag for this language sandbox
        [Required]
        public string DockerImage { get; set; } = string.Empty;

        [Required]
        public string Version { get; set; } = string.Empty;

        // File extension e.g. .py, .js, .cs
        [Required]
        public string FileExtension { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
    }
}