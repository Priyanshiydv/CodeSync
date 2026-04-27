using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services
{
    /// <summary>
    /// Handles writing audit log entries for all
    /// significant admin and platform actions.
    /// Case study §2.4 and §6 — timestamped audit trail.
    /// </summary>
    public class AuditLogService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            AuthDbContext context,
            ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(
            int actorId,
            string actorUsername,
            string action,
            string entityType,
            string entityId,
            string description,
            string? ipAddress = null)
        {
            try
            {
                var log = new AuditLog
                {
                    ActorId = actorId,
                    ActorUsername = actorUsername,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.Now
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "AUDIT | {Action} | Actor: {Actor} | " +
                    "Entity: {EntityType}/{EntityId} | {Description}",
                    action, actorUsername,
                    entityType, entityId, description);
            }
            catch (Exception ex)
            {
                // Never let audit logging break the main action
                _logger.LogError(ex,
                    "Failed to write audit log for action {Action}",
                    action);
            }
        }

        public async Task<List<AuditLog>> GetAllAsync(
            int page = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByActorAsync(int actorId)
        {
            return await _context.AuditLogs
                .Where(a => a.ActorId == actorId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByEntityAsync(
            string entityType, string entityId)
        {
            return await _context.AuditLogs
                .Where(a => a.EntityType == entityType
                    && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}