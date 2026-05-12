using System.ComponentModel.DataAnnotations;

namespace FileService.DTOs
{
    /// <summary>
    /// Data received from client to create a new folder.
    /// </summary>
    public class CreateFolderDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Path { get; set; } = string.Empty;

        [Required]
        public int ProjectId { get; set; }

         public int? ParentFolderId { get; set; }  
    }
}