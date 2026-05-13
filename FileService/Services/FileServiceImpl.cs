using FileService.Data;
using FileService.DTOs;
using FileService.Exceptions;
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
        private const string FileNotFound = "File not found!";

        public FileServiceImpl(FileDbContext context, IFileRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<CodeFile> CreateFile(int userId, CreateFileDto dto)
        {
            var existing = await _repository
                .FindByProjectIdAndPath(dto.ProjectId ?? 0, dto.Path);

            if (existing != null)
                throw new AlreadyExistsException("A file already exists at this path!");

            var file = new CodeFile
            {
                ProjectId = dto.ProjectId ?? 0,
                Name = dto.Name,
                Path = dto.Path,
                Language = dto.Language,
                Content = dto.Content,
                Size = System.Text.Encoding.UTF8.GetByteCount(dto.Content),
                CreatedById = userId,
                LastEditedBy = userId,
                IsFolder = false,
                ParentFolderId = dto.ParentFolderId 
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
                ?? throw new NotFoundException(FileNotFound);
            return file.Content;
        }

        public async Task<CodeFile> UpdateFileContent(
            int fileId, UpdateFileContentDto dto)
        {
            var file = await _repository.FindByFileId(fileId)
                ?? throw new NotFoundException(FileNotFound);

            file.Content = dto.Content;
            file.Size = System.Text.Encoding.UTF8.GetByteCount(dto.Content);

            file.LastEditedBy = dto.EditedByUserId ?? 0;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<CodeFile> RenameFile(int fileId, RenameFileDto dto)
        {
            var file = await _repository.FindByFileId(fileId)
                ?? throw new NotFoundException(FileNotFound);

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
                ?? throw new NotFoundException(FileNotFound);

            file.IsDeleted = true;
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<CodeFile> RestoreFile(int fileId)
        {
            var file = await _context.CodeFiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.FileId == fileId)
                ?? throw new NotFoundException(FileNotFound);

            file.IsDeleted = false;
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<CodeFile> MoveFile(int fileId, MoveFileDto dto)
        {
            var file = await _repository.FindByFileId(fileId)
                ?? throw new NotFoundException(FileNotFound);

            var existing = await _repository
                .FindByProjectIdAndPath(file.ProjectId, dto.NewPath);

            if (existing != null)
                throw new AlreadyExistsException("A file already exists at destination path!");

            file.Path = dto.NewPath;
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<CodeFile> CreateFolder(
            int userId, CreateFolderDto dto)
        {
            var existing = await _repository
                .FindByProjectIdAndPath(dto.ProjectId ?? 0, dto.Path);

            if (existing != null)
                throw new AlreadyExistsException("A folder already exists at this path!");

            var folder = new CodeFile
            {
                ProjectId = dto.ProjectId ?? 0,
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

            var lookup = allFiles.ToLookup(f => f.ParentFolderId);
            
            var rootItems = allFiles.Where(f => f.ParentFolderId == null).ToList();
            var result = new List<object>();
            
            foreach (var item in rootItems)
            {
                result.Add(BuildTreeNode(item, lookup));
            }
            
            return result;
        }

        private static object BuildTreeNode(CodeFile file, ILookup<int?, CodeFile> lookup)
        {
            if (file.IsFolder)
            {
                var children = lookup[file.FileId].ToList();
                return new
                {
                    file.FileId,
                    file.Name,
                    file.IsFolder,
                    expanded = false,
                    children = children.Select(child => BuildTreeNode(child, lookup)).ToList()
                };
            }
            else
            {
                return new
                {
                    file.FileId,
                    file.Name,
                    file.IsFolder,
                    file.Language,
                    file.Size
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