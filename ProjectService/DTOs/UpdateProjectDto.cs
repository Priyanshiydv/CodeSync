namespace ProjectService.DTOs
{
    /// <summary>
    /// Data received from client to update an existing project.
    /// </summary>
    public class UpdateProjectDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Language { get; set; }

        // PUBLIC or PRIVATE
        public string? Visibility { get; set; }
    }
}