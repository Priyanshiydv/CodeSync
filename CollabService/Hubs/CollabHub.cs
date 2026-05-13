using Microsoft.AspNetCore.SignalR;
using CollabService.Models;
using CollabService.Services;

namespace CollabService.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time collaboration events.
    /// Handles cursor updates, code changes, and participant join/leave events.
    /// </summary>
    public class CollabHub : Hub
    {

        // OTService injected for server-side transformation
        private readonly OTService _otService;

        public CollabHub(OTService otService)
        {
            _otService = otService;
        }


        /// <summary>
        /// Called when a participant joins a session group.
        /// </summary>
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.OthersInGroup(sessionId)
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
        /// ADD — Applies OT transformation before broadcasting.
        /// Transforms incoming operation against concurrent ops
        /// then broadcasts transformed op to all other participants.
        /// </summary>
        public async Task BroadcastChange(
            string sessionId,
            int userId,
            string content,
            string operationType,
            int position,
            string? insertedText,
            int deletedLength,
            int clientRevision)
        {
            // Build operation from incoming parameters
            var incoming = new EditOperation
            {
                Type = operationType,
                Position = position,
                Text = insertedText,
                Length = deletedLength,
                UserId = userId,
                Revision = clientRevision
            };

            EditOperation transformed;
            string finalContent = content;

            if (operationType == "full")
            {
                // Full content replacement — no OT needed
                // used for initial load and paste operations
                transformed = incoming;
                transformed.Revision =
                    _otService.GetRevision(sessionId) + 1;
            }
            else
            {
                // ADD — apply OT transformation server-side
                // transforms against any concurrent operations
                // the client hasn't seen yet
                transformed = _otService.Transform(
                    sessionId, incoming);

                // Apply the transformed operation to get new content
                finalContent = _otService.ApplyOperation(
                    content, transformed);
            }

            // Broadcast transformed operation to all OTHER participants
            // they apply the same transformed op to stay in sync
            await Clients.OthersInGroup(sessionId)
                .SendAsync("ReceiveChange",
                    userId,
                    finalContent,
                    transformed.Type,
                    transformed.Position,
                    transformed.Text,
                    transformed.Length,
                    transformed.Revision);
        }
    }
}