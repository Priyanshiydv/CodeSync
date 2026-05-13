using Microsoft.AspNetCore.SignalR;

namespace ExecutionService.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time execution output streaming.
    /// Streams stdout/stderr to the frontend as code executes.
    /// </summary>
    public class ExecutionHub : Hub
    {
        // Client joins a group by jobId to receive updates for that job
        public async Task JoinJob(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
        }

        public async Task LeaveJob(string jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
        }
    }
}