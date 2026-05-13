using FileService.Models;

namespace FileService.Interfaces
{
    /// <summary>
    /// Repository interface for CodeFile data access operations.
    /// </summary>
    public interface IFileRepository
    {
        Task<List<CodeFile>> FindByProjectId(int projectId);
        Task<CodeFile?> FindByFileId(int fileId);
        Task<CodeFile?> FindByProjectIdAndPath(int projectId, string path);
        Task<List<CodeFile>> FindByLanguage(string language);
        Task<List<CodeFile>> FindByLastEditedBy(int userId);
        Task<int> CountByProjectId(int projectId);
        Task<List<CodeFile>> FindByIsDeleted(bool isDeleted);
        Task DeleteByFileId(int fileId);
    }
}