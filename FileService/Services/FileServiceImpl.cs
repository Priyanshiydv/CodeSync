using FileService.Data;
using FileService.DTOs;
using FileService.Interfaces;
using FileService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileService.Services
{
    /// <summary>
    /// Implements all file management operations including
    /// CRUD, soft-delete, restore, move, rename and file tree retrieval.
    /// </summary>
    public class FileServiceImpl : IFileService
    {
        private readonly FileDbContext _context;
        private readonly IFileRepository _repository;

        public FileServiceImpl(FileDbContext context, IFileRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<CodeFile> CreateFile(int userId, CreateFileDto dto)
        {
            // Check if file already exists at this path
            var existing = await _repository
                .FindByProjectIdAndPath(dto.ProjectId, dto.Path);

            if (existing != null)
                throw new Exception("A file already exists at this path!");

            var file = new CodeFile
            {
                ProjectId = dto.ProjectId,
                Name = dto.Name,
                Path = dto.Path,
                Language = dto.Language,
                Content = dto.Content,
                Size = System.Text.Encoding.UTF8.GetByteCount(dto.Content),
                CreatedById = userId,
                LastEditedBy = userId,
                IsFolder = false
            };

            _context.CodeFiles.Add(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<CodeFile?> GetFileById(int fileId) =>
            await _repository.FindByFileId(fileId);

        public async Task<List<CodeFile>> GetFilesByProject(int projectId) =>
            await _repository.FindByProjectId(projectId);

        public async Task<string> GetFileContent(int fileId)
        {
            var file = await _repository.FindByFileId(fileId)
                ?? throw new Exception("File not found!");
            return file.Content;
        }

        public async Task<CodeFile> UpdateFileContent(
            int fileId, UpdateFileContentDto dto)
        {
            var file = await _repository.FindByFileId(fileId)
                ?? throw new Exception("File not found!");

            file.Content = dto.Content;
            file.Size = System.Text.Encoding.UTF8.GetByteCount(dto.Content);

            // Record who made this edit for collaborative attribution
            file.LastEditedBy = dto.EditedByUserId;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<CodeFile> RenameFile(int fileId, RenameFileDto dto)
        {
            var file = await _repository.FindByFileId(fileId)
                ?? throw new Exception("File not found!");

            // Update path with new name
            var pathParts = file.Path.Split('/');
            pathParts[^1] = dto.NewName;
            file.Path = string.Join("/", pathParts);
            file.Name = dto.NewName;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return file;
        }

        public async Task DeleteFile(int fileId)
        {
            var file = await _repository.FindByFileId(fileId)
                ?? throw new Exception("File not found!");

            // Soft delete - preserves file for potential restoration
            file.IsDeleted = true;
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<CodeFile> RestoreFile(int fileId)
        {
            // Bypass soft delete filter to find deleted file
            var file = await _context.CodeFiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.FileId == fileId)
                ?? throw new Exception("File not found!");

            file.IsDeleted = false;
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<CodeFile> MoveFile(int fileId, MoveFileDto dto)
        {
            var file = await _repository.FindByFileId(fileId)
                ?? throw new Exception("File not found!");

            // Check nothing exists at destination path
            var existing = await _repository
                .FindByProjectIdAndPath(file.ProjectId, dto.NewPath);

            if (existing != null)
                throw new Exception("A file already exists at destination path!");

            file.Path = dto.NewPath;
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<CodeFile> CreateFolder(
            int userId, CreateFolderDto dto)
        {
            var existing = await _repository
                .FindByProjectIdAndPath(dto.ProjectId, dto.Path);

            if (existing != null)
                throw new Exception("A folder already exists at this path!");

            var folder = new CodeFile
            {
                ProjectId = dto.ProjectId,
                Name = dto.Name,
                Path = dto.Path,
                Language = "folder",
                Content = string.Empty,
                CreatedById = userId,
                LastEditedBy = userId,
                IsFolder = true,
                ParentFolderId = dto.ParentFolderId
            };

            _context.CodeFiles.Add(folder);
            await _context.SaveChangesAsync();
            return folder;
        }

       public async Task<List<object>> GetFileTree(int projectId)
        {
            var allFiles = await _context.CodeFiles
                .Where(f => f.ProjectId == projectId && !f.IsDeleted)
                .ToListAsync();

            // Get root level items (ParentFolderId is null)
            var rootItems = allFiles.Where(f => f.ParentFolderId == null).ToList();
            var result = new List<object>();

            foreach (var item in rootItems)
            {
                result.Add(BuildTreeHierarchy(item, allFiles));
            }

            return result;
        }

        private object BuildTreeHierarchy(CodeFile item, List<CodeFile> allFiles)
        {
            if (item.IsFolder)
            {
                var children = allFiles.Where(f => f.ParentFolderId == item.FileId).ToList();
                return new
                {
                    item.FileId,
                    item.Name,
                    item.IsFolder,
                    expanded = false,
                    children = children.Select(c => BuildTreeHierarchy(c, allFiles)).ToList()
                };
            }
            else
            {
                return new
                {
                    item.FileId,
                    item.Name,
                    item.IsFolder,
                    item.Language,
                    item.Size
                };
            }
        }

        public async Task<List<CodeFile>> SearchInProject(
            int projectId, string query) =>
            await _context.CodeFiles
                .Where(f => f.ProjectId == projectId &&
                    (f.Name.Contains(query) || f.Content.Contains(query)))
                .ToListAsync();
    }
}