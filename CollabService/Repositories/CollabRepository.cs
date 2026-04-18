using CollabService.Data;
using CollabService.Interfaces;
using CollabService.Models;
using Microsoft.EntityFrameworkCore;

namespace CollabService.Repositories
{
    /// <summary>
    /// Handles all database operations for CollabSession and Participant entities.
    /// </summary>
    public class CollabRepository : ICollabRepository
    {
        private readonly CollabDbContext _context;

        public CollabRepository(CollabDbContext context)
        {
            _context = context;
        }

        public async Task<CollabSession?> FindBySessionId(Guid sessionId) =>
            await _context.CollabSessions
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        public async Task<List<CollabSession>> FindByProjectId(int projectId) =>
            await _context.CollabSessions
                .Include(s => s.Participants)
                .Where(s => s.ProjectId == projectId)
                .ToListAsync();

        public async Task<List<CollabSession>> FindByFileId(int fileId) =>
            await _context.CollabSessions
                .Where(s => s.FileId == fileId)
                .ToListAsync();

        public async Task<List<CollabSession>> FindActiveByProjectId(int projectId) =>
            await _context.CollabSessions
                .Where(s => s.ProjectId == projectId && s.Status == "ACTIVE")
                .ToListAsync();

        public async Task<List<Participant>> FindParticipantsBySessionId(
            Guid sessionId) =>
            await _context.Participants
                .Where(p => p.SessionId == sessionId)
                .ToListAsync();

        public async Task<List<CollabSession>> FindByOwnerId(int ownerId) =>
            await _context.CollabSessions
                .Where(s => s.OwnerId == ownerId)
                .ToListAsync();

        public async Task<int> CountParticipants(Guid sessionId) =>
            await _context.Participants
                .CountAsync(p => p.SessionId == sessionId
                    && p.LeftAt == null);
    }
}