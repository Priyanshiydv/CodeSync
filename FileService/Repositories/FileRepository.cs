using FileService.Data;
using FileService.Interfaces;
using FileService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileService.Repositories
{
    /// <summary>
    /// Handles all database operations for CodeFile entity.
    /// </summary>
    public class FileRepository : IFileRepository
    {
        private readonly FileDbContext _context;

        public FileRepository(FileDbContext context)
        {
            _context = context;
        }

        public async Task<List<CodeFile>> FindByProjectId(int projectId) =>
            await _context.CodeFiles
                .Where(f => f.ProjectId == projectId)
                .ToListAsync();

        public async Task<CodeFile?> FindByFileId(int fileId) =>
            await _context.CodeFiles
                .FirstOrDefaultAsync(f => f.FileId == fileId);

        public async Task<CodeFile?> FindByProjectIdAndPath(
            int projectId, string path) =>
            await _context.CodeFiles
                .FirstOrDefaultAsync(f =>
                    f.ProjectId == projectId && f.Path == path);

        public async Task<List<CodeFile>> FindByLanguage(string language) =>
            await _context.CodeFiles
                .Where(f => f.Language == language)
                .ToListAsync();

        public async Task<List<CodeFile>> FindByLastEditedBy(int userId) =>
            await _context.CodeFiles
                .Where(f => f.LastEditedBy == userId)
                .ToListAsync();

        public async Task<int> CountByProjectId(int projectId) =>
            await _context.CodeFiles
                .CountAsync(f => f.ProjectId == projectId);

        public async Task<List<CodeFile>> FindByIsDeleted(bool isDeleted)
        {
            // Bypass soft delete filter to find deleted files
            return await _context.CodeFiles
                .IgnoreQueryFilters()
                .Where(f => f.IsDeleted == isDeleted)
                .ToListAsync();
        }

        public async Task DeleteByFileId(int fileId)
        {
            var file = await _context.CodeFiles
                .FirstOrDefaultAsync(f => f.FileId == fileId);
            if (file != null)
            {
                _context.CodeFiles.Remove(file);
                await _context.SaveChangesAsync();
            }
        }
    }
}