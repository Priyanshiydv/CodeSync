using Microsoft.AspNetCore.SignalR;

namespace CollabService.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time collaboration events.
    /// Handles cursor updates, code changes, and participant join/leave events.
    /// </summary>
    public class CollabHub : Hub
    {
        /// <summary>
        /// Called when a participant joins a session group.
        /// </summary>
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Group(sessionId)
                .SendAsync("ParticipantJoined", Context.ConnectionId);
        }

        /// <summary>
        /// Called when a participant leaves a session group.
        /// </summary>
        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Group(sessionId)
                .SendAsync("ParticipantLeft", Context.ConnectionId);
        }

        /// <summary>
        /// Broadcasts cursor position update to all session participants.
        /// </summary>
        public async Task UpdateCursor(
            string sessionId, int userId, int line, int col)
        {
            await Clients.OthersInGroup(sessionId)
                .SendAsync("CursorUpdated", userId, line, col);
        }

        /// <summary>
        /// Broadcasts code change to all session participants.
        /// OT/CRDT logic applied before broadcasting.
        /// </summary>
        public async Task BroadcastChange(
            string sessionId, int userId, string content)
        {
            await Clients.OthersInGroup(sessionId)
                .SendAsync("CodeChanged", userId, content);
        }
    }
}