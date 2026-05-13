using ProjectService.Models;

namespace ProjectService.Interfaces
{
    /// <summary>
    /// Repository interface for Project data access operations.
    /// </summary>
    public interface IProjectRepository
    {
        Task<List<Project>> FindByOwnerId(int ownerId);
        Task<Project?> FindByProjectId(int projectId);
        Task<List<Project>> FindByVisibility(string visibility);
        Task<List<Project>> FindByLanguage(string language);
        Task<List<Project>> SearchByName(string name);
        Task<List<Project>> FindByMemberUserId(int userId);
        Task<List<Project>> FindByIsArchived(bool isArchived);
        Task<int> CountByOwnerId(int ownerId);
    }
}