using System.ComponentModel.DataAnnotations;

namespace ProjectService.DTOs
{
    /// <summary>
    /// Data received from client to create a new project.
    /// </summary>
    public class CreateProjectDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Language { get; set; } = string.Empty;

        // PUBLIC or PRIVATE
        [Required]
        [RegularExpression("^(PUBLIC|PRIVATE)$",
            ErrorMessage = "Visibility must be PUBLIC or PRIVATE")]
        public string Visibility { get; set; } = "PUBLIC";

        public int? TemplateId { get; set; }
    }
}