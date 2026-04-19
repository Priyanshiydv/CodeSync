using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notification delivery.
    /// Pushes unread badge count to recipient in real time.
    /// </summary>
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Client joins their personal notification group
        /// using their userId so they only receive their own notifications
        /// </summary>
        public async Task JoinNotificationGroup(string userId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId, $"user_{userId}");
        }

        /// <summary>
        /// Client leaves their notification group on disconnect
        /// </summary>
        public async Task LeaveNotificationGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId, $"user_{userId}");
        }
    }
}