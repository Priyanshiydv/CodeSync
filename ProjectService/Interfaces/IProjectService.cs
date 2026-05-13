using ProjectService.DTOs;
using ProjectService.Models;

namespace ProjectService.Interfaces
{
    /// <summary>
    /// Defines all project management operations.
    /// </summary>
    public interface IProjectService
    {
        Task<Project> CreateProject(int ownerId, CreateProjectDto dto);
        Task<Project?> GetProjectById(int projectId);
        Task<List<Project>> GetProjectsByOwner(int ownerId);
        Task<List<Project>> GetPublicProjects();
        Task<List<Project>> SearchProjects(string query);
        Task<List<Project>> GetProjectsByMember(int userId);
        Task<Project> UpdateProject(int projectId, UpdateProjectDto dto);
        Task ArchiveProject(int projectId);
        Task DeleteProject(int projectId);
        Task<Project> ForkProject(int projectId, int newOwnerId);
        Task StarProject(int projectId);
        Task<List<Project>> GetProjectsByLanguage(string language);
        Task<List<ProjectMember>> GetMembers(int projectId);
        Task AddMember(int projectId, AddMemberDto dto);
        Task RemoveMember(int projectId, int userId);
    }
}