using VersionService.DTOs;
using VersionService.Models;

namespace VersionService.Interfaces
{
    /// <summary>
    /// Defines all snapshot CRUD, branch management,
    /// tag assignment, diff computation and history retrieval operations.
    /// </summary>
    public interface IVersionService
    {
        Task<Snapshot> CreateSnapshot(int authorId, CreateSnapshotDto dto);
        Task<Snapshot?> GetSnapshotById(int snapshotId);
        Task<List<Snapshot>> GetSnapshotsByFile(int fileId);
        Task<List<Snapshot>> GetSnapshotsByProject(int projectId);
        Task<List<Snapshot>> GetSnapshotsByBranch(string branch);
        Task<Snapshot?> GetLatestSnapshot(int fileId);
        Task<Snapshot> RestoreSnapshot(int snapshotId, int authorId);
        Task<object> DiffSnapshots(int snapshotId1, int snapshotId2);
        Task<Snapshot> CreateBranch(int authorId, CreateBranchDto dto);
        Task<Snapshot> TagSnapshot(TagSnapshotDto dto);
        Task<List<Snapshot>> GetFileHistory(int fileId);
    }
}