using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Interfaces;
using System.Security.Claims;

namespace NotificationService.Controllers
{
    /// <summary>
    /// Handles all HTTP requests for notification management.
    /// Exposes /api/notifications endpoints.
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(
            INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost("send")]
        [Authorize]
        public async Task<IActionResult> Send(
            [FromBody] SendNotificationDto dto)
        {
            try
            {
                var notification = await _notificationService.Send(dto);
                return Ok(new
                {
                    message = "Notification sent!",
                    notification
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("bulk")]
        [Authorize]
        public async Task<IActionResult> SendBulk(
            [FromBody] SendBulkNotificationDto dto)
        {
            try
            {
                var notifications = await _notificationService
                    .SendBulk(dto);
                return Ok(new
                {
                    message = $"{notifications.Count} notifications sent!",
                    notifications
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("email")]
        [Authorize]
        public async Task<IActionResult> SendEmail(
            [FromBody] SendEmailDto dto)
        {
            try
            {
                await _notificationService.SendEmail(dto);
                return Ok(new { message = "Email sent!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("recipient/{recipientId}")]
        [Authorize]
        public async Task<IActionResult> GetByRecipient(int recipientId)
        {
            var notifications = await _notificationService
                .GetByRecipient(recipientId);
            return Ok(notifications);
        }

        [HttpGet("unread/{recipientId}")]
        [Authorize]
        public async Task<IActionResult> GetUnreadCount(int recipientId)
        {
            var count = await _notificationService
                .GetUnreadCount(recipientId);
            return Ok(new { recipientId, unreadCount = count });
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var notifications = await _notificationService.GetAll();
            return Ok(notifications);
        }

        [HttpPut("{notificationId}/read")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                var notification = await _notificationService
                    .MarkAsRead(notificationId);
                return Ok(new
                {
                    message = "Marked as read!",
                    notification
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("read-all/{recipientId}")]
        [Authorize]
        public async Task<IActionResult> MarkAllRead(int recipientId)
        {
            try
            {
                await _notificationService.MarkAllRead(recipientId);
                return Ok(new { message = "All marked as read!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{notificationId}")]
        [Authorize]
        public async Task<IActionResult> Delete(int notificationId)
        {
            try
            {
                await _notificationService
                    .DeleteNotification(notificationId);
                return Ok(new { message = "Notification deleted!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("read/{recipientId}")]
        [Authorize]
        public async Task<IActionResult> DeleteRead(int recipientId)
        {
            try
            {
                await _notificationService.DeleteRead(recipientId);
                return Ok(new { message = "Read notifications deleted!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}