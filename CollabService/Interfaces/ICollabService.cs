using CollabService.DTOs;
using CollabService.Models;

namespace CollabService.Interfaces
{
    /// <summary>
    /// Defines all session lifecycle, participant management,
    /// cursor update and SignalR broadcast operations.
    /// </summary>
    public interface ICollabService
    {
        Task<CollabSession> CreateSession(int ownerId, CreateSessionDto dto);
        Task<CollabSession?> GetSessionById(Guid sessionId);
        Task<List<CollabSession>> GetSessionsByProject(int projectId);
        Task<Participant> JoinSession(Guid sessionId, JoinSessionDto dto);
        Task LeaveSession(Guid sessionId, int userId);
        Task EndSession(Guid sessionId);
        Task<List<Participant>> GetParticipants(Guid sessionId);
        Task<Participant> UpdateCursor(Guid sessionId, UpdateCursorDto dto);
        Task BroadcastChange(Guid sessionId, BroadcastChangeDto dto);
        Task KickParticipant(Guid sessionId, int userId);
        Task<CollabSession?> GetActiveSession(int fileId);
        Task<IEnumerable<CollabSession>> GetAllActiveSessionsAsync();
        Task EndSessionAsync(string sessionId);
    }
}