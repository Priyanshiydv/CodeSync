using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CollabService.Data;

namespace CollabService.Controllers
{
    /// <summary>
    /// Admin-only endpoints for session monitoring and management.
    /// All endpoints require ADMIN role.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN, Admin")]
    public class AdminController : ControllerBase
    {
        private readonly CollabDbContext _context;

        public AdminController(CollabDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/sessions/active
        [HttpGet("sessions/active")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var sessions = await _context.CollabSessions
                .Where(s => s.Status == "ACTIVE")
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.SessionId,
                    s.ProjectId,
                    s.FileId,
                    s.OwnerId,
                    s.Language,
                    s.Status,
                    s.CreatedAt,
                    s.MaxParticipants,
                    s.IsPasswordProtected,
                    participantCount = _context.Participants
                        .Count(p => p.SessionId == s.SessionId && p.LeftAt == null)
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // GET: api/admin/sessions
        [HttpGet("sessions")]
        public async Task<IActionResult> GetAllSessions()
        {
            var sessions = await _context.CollabSessions
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.SessionId,
                    s.ProjectId,
                    s.FileId,
                    s.OwnerId,
                    s.Language,
                    s.Status,
                    s.CreatedAt,
                    s.EndedAt,
                    s.MaxParticipants,
                    s.IsPasswordProtected,
                    participantCount = _context.Participants
                        .Count(p => p.SessionId == s.SessionId)
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // POST: api/admin/sessions/{sessionId}/end
        [HttpPost("sessions/{sessionId}/end")]
        public async Task<IActionResult> EndSession(Guid sessionId)
        {
            var session = await _context.CollabSessions.FindAsync(sessionId);
            if (session == null)
                return NotFound(new { message = "Session not found" });

            if (session.Status == "ENDED")
                return BadRequest(new { message = "Session already ended" });

            session.Status = "ENDED";
            session.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Session ended successfully" });
        }

        // GET: api/admin/sessions/{sessionId}/participants
        [HttpGet("sessions/{sessionId}/participants")]
        public async Task<IActionResult> GetSessionParticipants(Guid sessionId)
        {
            var participants = await _context.Participants
                .Where(p => p.SessionId == sessionId)
                .OrderBy(p => p.JoinedAt)
                .Select(p => new
                {
                    p.ParticipantId,
                    p.UserId,
                    p.Role,
                    p.JoinedAt,
                    p.LeftAt,
                    p.CursorLine,
                    p.CursorCol,
                    p.Color,
                    isActive = p.LeftAt == null
                })
                .ToListAsync();

            return Ok(participants);
        }

        // GET: api/admin/analytics/sessions
        [HttpGet("analytics/sessions")]
        public async Task<IActionResult> GetSessionAnalytics()
        {
            var totalSessions = await _context.CollabSessions.CountAsync();
            var activeSessions = await _context.CollabSessions.CountAsync(s => s.Status == "ACTIVE");
            var endedSessions = await _context.CollabSessions.CountAsync(s => s.Status == "ENDED");

            var avgParticipantsPerSession = await _context.CollabSessions
                .Select(s => _context.Participants.Count(p => p.SessionId == s.SessionId))
                .AverageAsync();

            var sessionsByLanguage = await _context.CollabSessions
                .GroupBy(s => s.Language)
                .Select(g => new
                {
                    language = g.Key,
                    count = g.Count(),
                    active = g.Count(s => s.Status == "ACTIVE")
                })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            var recentSessions = await _context.CollabSessions
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .Select(s => new
                {
                    s.SessionId,
                    s.Language,
                    s.CreatedAt,
                    s.Status,
                    participants = _context.Participants.Count(p => p.SessionId == s.SessionId)
                })
                .ToListAsync();

            return Ok(new
            {
                totalSessions,
                activeSessions,
                endedSessions,
                avgParticipantsPerSession,
                sessionsByLanguage,
                recentSessions
            });
        }

        // DELETE: api/admin/sessions/{sessionId}
        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(Guid sessionId)
        {
            var session = await _context.CollabSessions.FindAsync(sessionId);
            if (session == null)
                return NotFound(new { message = "Session not found" });

            // Delete participants first
            var participants = await _context.Participants
                .Where(p => p.SessionId == sessionId)
                .ToListAsync();

            _context.Participants.RemoveRange(participants);
            _context.CollabSessions.Remove(session);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Session deleted successfully" });
        }
    }
}