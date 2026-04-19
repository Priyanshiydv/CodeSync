using NotificationService.DTOs;
using NotificationService.Models;

namespace NotificationService.Interfaces
{
    /// <summary>
    /// Defines notification dispatch, retrieval,
    /// read-state management and deletion operations.
    /// </summary>
    public interface INotificationService
    {
        Task<Notification> Send(SendNotificationDto dto);
        Task<List<Notification>> SendBulk(SendBulkNotificationDto dto);
        Task<Notification> MarkAsRead(int notificationId);
        Task MarkAllRead(int recipientId);
        Task DeleteRead(int recipientId);
        Task<List<Notification>> GetByRecipient(int recipientId);
        Task<int> GetUnreadCount(int recipientId);
        Task DeleteNotification(int notificationId);
        Task SendEmail(SendEmailDto dto);
        Task<List<Notification>> GetAll();
    }
}