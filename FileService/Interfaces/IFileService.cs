using FileService.DTOs;
using FileService.Models;

namespace FileService.Interfaces
{
    /// <summary>
    /// Defines all file management operations including
    /// CRUD, content update, rename, move, soft-delete and restore.
    /// </summary>
    public interface IFileService
    {
        Task<CodeFile> CreateFile(int userId, CreateFileDto dto);
        Task<CodeFile?> GetFileById(int fileId);
        Task<List<CodeFile>> GetFilesByProject(int projectId);
        Task<string> GetFileContent(int fileId);
        Task<CodeFile> UpdateFileContent(int fileId, UpdateFileContentDto dto);
        Task<CodeFile> RenameFile(int fileId, RenameFileDto dto);
        Task DeleteFile(int fileId);
        Task<CodeFile> RestoreFile(int fileId);
        Task<CodeFile> MoveFile(int fileId, MoveFileDto dto);
        Task<CodeFile> CreateFolder(int userId, CreateFolderDto dto);
        Task<List<CodeFile>> GetFileTree(int projectId);
        Task<List<CodeFile>> SearchInProject(int projectId, string query);
    }
}