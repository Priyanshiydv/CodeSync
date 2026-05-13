using Microsoft.EntityFrameworkCore;
using VersionService.Data;
using VersionService.Interfaces;
using VersionService.Models;

namespace VersionService.Repositories
{
    /// <summary>
    /// Handles all database operations for Snapshot entity.
    /// </summary>
    public class SnapshotRepository : ISnapshotRepository
    {
        private readonly VersionDbContext _context;

        public SnapshotRepository(VersionDbContext context)
        {
            _context = context;
        }

        public async Task<List<Snapshot>> FindByProjectId(int projectId) =>
            await _context.Snapshots
                .Where(s => s.ProjectId == projectId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        public async Task<List<Snapshot>> FindByFileId(int fileId) =>
            await _context.Snapshots
                .Where(s => s.FileId == fileId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        public async Task<List<Snapshot>> FindByAuthorId(int authorId) =>
            await _context.Snapshots
                .Where(s => s.AuthorId == authorId)
                .ToListAsync();

        public async Task<List<Snapshot>> FindByBranch(string branch) =>
            await _context.Snapshots
                .Where(s => s.Branch == branch)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        public async Task<Snapshot?> FindBySnapshotId(int snapshotId) =>
            await _context.Snapshots
                .FirstOrDefaultAsync(s => s.SnapshotId == snapshotId);

        public async Task<Snapshot?> FindByHash(string hash) =>
            await _context.Snapshots
                .FirstOrDefaultAsync(s => s.Hash == hash);

        public async Task<Snapshot?> FindByTag(string tag) =>
            await _context.Snapshots
                .FirstOrDefaultAsync(s => s.Tag == tag);

        // Returns most recent snapshot for a file
        public async Task<Snapshot?> FindLatestByFileId(int fileId) =>
            await _context.Snapshots
                .Where(s => s.FileId == fileId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
    }
}