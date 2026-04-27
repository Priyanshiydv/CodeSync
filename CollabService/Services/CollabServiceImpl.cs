using CollabService.Data;
using CollabService.DTOs;
using CollabService.Interfaces;
using CollabService.Models;
using Microsoft.AspNetCore.SignalR;
using CollabService.Hubs;
using Microsoft.EntityFrameworkCore;

namespace CollabService.Services
{
    public class CollabServiceImpl : ICollabService
    {
        private readonly CollabDbContext _context;
        private readonly ICollabRepository _repository;
        private readonly IHubContext<CollabHub> _hubContext;
        private readonly OTService _otService;

        private readonly string[] _colors = new[]
        {
            "#FF5733", "#33FF57", "#3357FF", "#FF33A8",
            "#FFB833", "#33FFF5", "#A833FF", "#FF3333"
        };

        public CollabServiceImpl(
            CollabDbContext context,
            ICollabRepository repository,
            IHubContext<CollabHub> hubContext,
            OTService otService)
        {
            _context = context;
            _repository = repository;
            _hubContext = hubContext;
            _otService = otService;
        }

        public async Task<CollabSession> CreateSession(
            int ownerId, CreateSessionDto dto)
        {
            var session = new CollabSession
            {
                ProjectId = dto.ProjectId,
                FileId = dto.FileId,
                OwnerId = ownerId,
                Language = dto.Language,
                MaxParticipants = dto.MaxParticipants,
                IsPasswordProtected = dto.IsPasswordProtected,
                SessionPassword = dto.SessionPassword,
                LastActivityAt = DateTime.UtcNow  // ADD — initialize activity timestamp
            };

            _context.CollabSessions.Add(session);
            await _context.SaveChangesAsync();

            var participant = new Participant
            {
                SessionId = session.SessionId,
                UserId = ownerId,
                Role = "HOST",
                Color = _colors[0]
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<CollabSession?> GetSessionById(Guid sessionId) =>
            await _repository.FindBySessionId(sessionId);

        public async Task<List<CollabSession>> GetSessionsByProject(
            int projectId) =>
            await _repository.FindByProjectId(projectId);

        public async Task<Participant> JoinSession(
            Guid sessionId, JoinSessionDto dto)
        {
            var session = await _repository.FindBySessionId(sessionId)
                ?? throw new Exception("Session not found!");

            if (session.Status == "ENDED")
                throw new Exception("Session has ended!");

            if (session.IsPasswordProtected &&
                session.SessionPassword != dto.SessionPassword)
                throw new Exception("Incorrect session password!");

            var count = await _repository.CountParticipants(sessionId);
            if (count >= session.MaxParticipants)
                throw new Exception("Session is full!");

            var existing = await _context.Participants
                .FirstOrDefaultAsync(p =>
                    p.SessionId == sessionId && p.UserId == dto.UserId);

            if (existing != null)
                throw new Exception("User already in session!");

            var color = _colors[count % _colors.Length];

            var participant = new Participant
            {
                SessionId = sessionId,
                UserId = dto.UserId,
                Role = "EDITOR",
                Color = color
            };

            _context.Participants.Add(participant);

            // ADD — update last activity when someone joins
            session.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync("ParticipantJoined", dto.UserId);

            return participant;
        }

        public async Task LeaveSession(Guid sessionId, int userId)
        {
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p =>
                    p.SessionId == sessionId && p.UserId == userId)
                ?? throw new Exception("Participant not found!");

            participant.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync("ParticipantLeft", userId);
        }

        public async Task EndSession(Guid sessionId)
        {
            var session = await _repository.FindBySessionId(sessionId)
                ?? throw new Exception("Session not found!");

            session.Status = "ENDED";
            session.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _otService.ClearSession(sessionId.ToString());

            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync("SessionEnded", sessionId);
        }

        public async Task<List<Participant>> GetParticipants(Guid sessionId) =>
            await _repository.FindParticipantsBySessionId(sessionId);

        public async Task<Participant> UpdateCursor(
            Guid sessionId, UpdateCursorDto dto)
        {
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p =>
                    p.SessionId == sessionId && p.UserId == dto.UserId)
                ?? throw new Exception("Participant not found!");

            participant.CursorLine = dto.CursorLine;
            participant.CursorCol = dto.CursorCol;

            // ADD — update last activity on cursor movement
            var session = await _repository.FindBySessionId(sessionId);
            if (session != null)
                session.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync("CursorUpdated",
                    dto.UserId, dto.CursorLine, dto.CursorCol);

            return participant;
        }

        public async Task BroadcastChange(
            Guid sessionId, BroadcastChangeDto dto)
        {
            // ADD — update last activity on every code change
            var session = await _repository.FindBySessionId(sessionId);
            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync("CodeChanged", dto.UserId, dto.Content);
        }

        public async Task KickParticipant(Guid sessionId, int userId)
        {
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p =>
                    p.SessionId == sessionId && p.UserId == userId)
                ?? throw new Exception("Participant not found!");

            participant.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync("ParticipantKicked", userId);
        }

        public async Task<CollabSession?> GetActiveSession(int fileId) =>
            await _context.CollabSessions
                .FirstOrDefaultAsync(s =>
                    s.FileId == fileId && s.Status == "ACTIVE");

        // ADD — returns all ACTIVE sessions, used by SessionCleanupWorker
        public async Task<IEnumerable<CollabSession>> GetAllActiveSessionsAsync()
        {
            return await _context.CollabSessions
                .Where(s => s.Status == "ACTIVE")
                .ToListAsync();
        }

        // ADD — ends session by string sessionId, used by SessionCleanupWorker
        public async Task EndSessionAsync(string sessionId)
        {
            if (Guid.TryParse(sessionId, out var guid))
            {
                var session = await _repository.FindBySessionId(guid);
                if (session != null)
                {
                    session.Status = "ENDED";
                    session.EndedAt = DateTime.UtcNow;
                    session.LastActivityAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
        }

    }  
}  