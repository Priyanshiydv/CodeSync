using VersionService.Models;

namespace VersionService.Interfaces
{
    /// <summary>
    /// Repository interface for Snapshot data access operations.
    /// </summary>
    public interface ISnapshotRepository
    {
        Task<List<Snapshot>> FindByProjectId(int projectId);
        Task<List<Snapshot>> FindByFileId(int fileId);
        Task<List<Snapshot>> FindByAuthorId(int authorId);
        Task<List<Snapshot>> FindByBranch(string branch);
        Task<Snapshot?> FindBySnapshotId(int snapshotId);
        Task<Snapshot?> FindByHash(string hash);
        Task<Snapshot?> FindByTag(string tag);
        Task<Snapshot?> FindLatestByFileId(int fileId);
    }
}