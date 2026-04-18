using System.Security.Cryptography;
using System.Text;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.EntityFrameworkCore;
using VersionService.Data;
using VersionService.DTOs;
using VersionService.Interfaces;
using VersionService.Models;

namespace VersionService.Services
{
    /// <summary>
    /// Implements all version control operations including snapshot creation,
    /// SHA-256 hashing, DiffPlex diff computation, branch management and restore.
    /// </summary>
    public class VersionServiceImpl : IVersionService
    {
        private readonly VersionDbContext _context;
        private readonly ISnapshotRepository _repository;

        public VersionServiceImpl(
            VersionDbContext context,
            ISnapshotRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        /// <summary>
        /// Computes SHA-256 hash of content for integrity verification
        /// </summary>
        private static string ComputeHash(string content)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(bytes).ToLower();
        }

        public async Task<Snapshot> CreateSnapshot(
            int authorId, CreateSnapshotDto dto)
        {
            var snapshot = new Snapshot
            {
                ProjectId = dto.ProjectId,
                FileId = dto.FileId,
                AuthorId = authorId,
                Message = dto.Message,
                Content = dto.Content,
                // SHA-256 hash for integrity verification on restore
                Hash = ComputeHash(dto.Content),
                ParentSnapshotId = dto.ParentSnapshotId,
                Branch = dto.Branch
            };

            _context.Snapshots.Add(snapshot);
            await _context.SaveChangesAsync();
            return snapshot;
        }

        public async Task<Snapshot?> GetSnapshotById(int snapshotId) =>
            await _repository.FindBySnapshotId(snapshotId);

        public async Task<List<Snapshot>> GetSnapshotsByFile(int fileId) =>
            await _repository.FindByFileId(fileId);

        public async Task<List<Snapshot>> GetSnapshotsByProject(int projectId) =>
            await _repository.FindByProjectId(projectId);

        public async Task<List<Snapshot>> GetSnapshotsByBranch(string branch) =>
            await _repository.FindByBranch(branch);

        public async Task<Snapshot?> GetLatestSnapshot(int fileId) =>
            await _repository.FindLatestByFileId(fileId);

        public async Task<Snapshot> RestoreSnapshot(
            int snapshotId, int authorId)
        {
            var original = await _repository.FindBySnapshotId(snapshotId)
                ?? throw new Exception("Snapshot not found!");

            // Verify integrity using SHA-256 hash before restoring
            var computedHash = ComputeHash(original.Content);
            if (computedHash != original.Hash)
                throw new Exception(
                    "Snapshot integrity check failed! Content may be corrupted.");

            // Non-destructive restore - creates NEW snapshot with old content
            // instead of modifying history
            var restored = new Snapshot
            {
                ProjectId = original.ProjectId,
                FileId = original.FileId,
                AuthorId = authorId,
                Message = $"Restored from snapshot #{snapshotId}",
                Content = original.Content,
                Hash = original.Hash,
                ParentSnapshotId = snapshotId,
                Branch = original.Branch
            };

            _context.Snapshots.Add(restored);
            await _context.SaveChangesAsync();
            return restored;
        }

        public async Task<object> DiffSnapshots(
            int snapshotId1, int snapshotId2)
        {
            var snapshot1 = await _repository.FindBySnapshotId(snapshotId1)
                ?? throw new Exception($"Snapshot {snapshotId1} not found!");

            var snapshot2 = await _repository.FindBySnapshotId(snapshotId2)
                ?? throw new Exception($"Snapshot {snapshotId2} not found!");

            // Use DiffPlex Myers algorithm for line-by-line diff computation
            var diffBuilder = new InlineDiffBuilder(new DiffPlex.Differ());
            var diff = diffBuilder.BuildDiffModel(
                snapshot1.Content, snapshot2.Content);

            // Build structured diff result with line annotations
            var lines = diff.Lines.Select(line => new
            {
                text = line.Text,
                type = line.Type.ToString(), // Unchanged, Inserted, Deleted
                position = line.Position
            }).ToList();

            return new
            {
                snapshotId1,
                snapshotId2,
                totalChanges = lines.Count(
                    l => l.type != ChangeType.Unchanged.ToString()),
                lines
            };
        }

        public async Task<Snapshot> CreateBranch(
            int authorId, CreateBranchDto dto)
        {
            var fromSnapshot = await _repository
                .FindBySnapshotId(dto.FromSnapshotId)
                ?? throw new Exception("Source snapshot not found!");

            // Create first snapshot on new branch from existing snapshot
            var branchSnapshot = new Snapshot
            {
                ProjectId = fromSnapshot.ProjectId,
                FileId = fromSnapshot.FileId,
                AuthorId = authorId,
                Message = $"Created branch '{dto.BranchName}'",
                Content = fromSnapshot.Content,
                Hash = ComputeHash(fromSnapshot.Content),
                ParentSnapshotId = dto.FromSnapshotId,
                Branch = dto.BranchName
            };

            _context.Snapshots.Add(branchSnapshot);
            await _context.SaveChangesAsync();
            return branchSnapshot;
        }

        public async Task<Snapshot> TagSnapshot(TagSnapshotDto dto)
        {
            var snapshot = await _repository
                .FindBySnapshotId(dto.SnapshotId)
                ?? throw new Exception("Snapshot not found!");

            // Check tag is not already used
            var existing = await _repository.FindByTag(dto.Tag);
            if (existing != null)
                throw new Exception($"Tag '{dto.Tag}' already exists!");

            snapshot.Tag = dto.Tag;
            await _context.SaveChangesAsync();
            return snapshot;
        }

        public async Task<List<Snapshot>> GetFileHistory(int fileId) =>
            await _context.Snapshots
                .Where(s => s.FileId == fileId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
    }
}