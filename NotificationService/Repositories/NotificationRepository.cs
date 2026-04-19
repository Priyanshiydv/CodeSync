using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Interfaces;
using NotificationService.Models;

namespace NotificationService.Repositories
{
    /// <summary>
    /// Handles all database operations for Notification entity.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationDbContext _context;

        public NotificationRepository(NotificationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Notification>> FindByRecipientId(
            int recipientId) =>
            await _context.Notifications
                .Where(n => n.RecipientId == recipientId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task<List<Notification>> FindByRecipientIdAndIsRead(
            int recipientId, bool isRead) =>
            await _context.Notifications
                .Where(n => n.RecipientId == recipientId
                    && n.IsRead == isRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task<int> CountByRecipientIdAndIsRead(
            int recipientId, bool isRead) =>
            await _context.Notifications
                .CountAsync(n => n.RecipientId == recipientId
                    && n.IsRead == isRead);

        public async Task<List<Notification>> FindByType(string type) =>
            await _context.Notifications
                .Where(n => n.Type == type)
                .ToListAsync();

        public async Task<List<Notification>> FindByRelatedId(
            string relatedId) =>
            await _context.Notifications
                .Where(n => n.RelatedId == relatedId)
                .ToListAsync();

        public async Task DeleteByNotificationId(int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.NotificationId == notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByRecipientIdAndIsRead(
            int recipientId, bool isRead)
        {
            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == recipientId
                    && n.IsRead == isRead)
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
        }
    }
}