using CollabService.Models;

namespace CollabService.Interfaces
{
    /// <summary>
    /// Repository interface for CollabSession data access operations.
    /// </summary>
    public interface ICollabRepository
    {
        Task<CollabSession?> FindBySessionId(Guid sessionId);
        Task<List<CollabSession>> FindByProjectId(int projectId);
        Task<List<CollabSession>> FindByFileId(int fileId);
        Task<List<CollabSession>> FindActiveByProjectId(int projectId);
        Task<List<Participant>> FindParticipantsBySessionId(Guid sessionId);
        Task<List<CollabSession>> FindByOwnerId(int ownerId);
        Task<int> CountParticipants(Guid sessionId);
        // ADD this to existing ICollabRepository interface
        Task<IEnumerable<CollabSession>> FindActiveSessionsAsync();
    }
}