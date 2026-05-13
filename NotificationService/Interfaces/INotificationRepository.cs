using NotificationService.Models;

namespace NotificationService.Interfaces
{
    /// <summary>
    /// Repository interface for Notification data access operations.
    /// </summary>
    public interface INotificationRepository
    {
        Task<List<Notification>> FindByRecipientId(int recipientId);
        Task<List<Notification>> FindByRecipientIdAndIsRead(
            int recipientId, bool isRead);
        Task<int> CountByRecipientIdAndIsRead(int recipientId, bool isRead);
        Task<List<Notification>> FindByType(string type);
        Task<List<Notification>> FindByRelatedId(string relatedId);
        Task DeleteByNotificationId(int notificationId);
        Task DeleteByRecipientIdAndIsRead(int recipientId, bool isRead);
    }
}