using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using NotificationService.Clients;
using NotificationService.Data;
using NotificationService.DTOs;
using NotificationService.Hubs;
using NotificationService.Interfaces;
using NotificationService.Models;

namespace NotificationService.Services
{
    /// <summary>
    /// Implements notification dispatch, bulk send, email via MailKit,
    /// read-state management and real-time SignalR badge updates.
    /// </summary>
    public class NotificationServiceImpl : INotificationService
    {
        private readonly NotificationDbContext _context;
        private readonly INotificationRepository _repository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationServiceImpl> _logger;
        private readonly IAuthServiceClient _authServiceClient;

        public NotificationServiceImpl(
            NotificationDbContext context,
            INotificationRepository repository,
            IHubContext<NotificationHub> hubContext,
            IConfiguration configuration,
            ILogger<NotificationServiceImpl> logger,
            IAuthServiceClient authServiceClient)
        {
            _context = context;
            _repository = repository;
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
            _authServiceClient = authServiceClient;
        }

        public async Task<Notification> Send(SendNotificationDto dto)
        {
            var notification = new Notification
            {
                RecipientId = dto.RecipientId,
                ActorId = dto.ActorId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                RelatedId = dto.RelatedId,
                RelatedType = dto.RelatedType
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Push real-time unread count to recipient via SignalR
            var unreadCount = await GetUnreadCount(dto.RecipientId);
            await _hubContext.Clients
                .Group($"user_{dto.RecipientId}")
                .SendAsync("UnreadCountUpdated", unreadCount);

            return notification;
        }

        public async Task<List<Notification>> SendBulk(
            SendBulkNotificationDto dto)
        {
            var notifications = new List<Notification>();
            // Get recipients based on target
            List<int> recipientIds;
            if (dto.RecipientIds != null && dto.RecipientIds.Any())
            {
                recipientIds = dto.RecipientIds;
            }
            else if (dto.TargetRole == "ALL")
            {
                recipientIds = await _authServiceClient.GetAllUserIds();
            }
            else
            {
                recipientIds = await _authServiceClient.GetUserIdsByRole(dto.TargetRole!);
            }

            foreach (var recipientId in recipientIds)
            {
                var notification = new Notification
                {
                    RecipientId = recipientId,
                    ActorId = dto.ActorId,
                    Type = dto.Type,
                    Title = dto.Title,
                    Message = dto.Message
                };
                notifications.Add(notification);
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            // Push updates
            foreach (var recipientId in recipientIds.Distinct())
            {
                var unreadCount = await GetUnreadCount(recipientId);
                await _hubContext.Clients
                    .Group($"user_{recipientId}")
                    .SendAsync("UnreadCountUpdated", unreadCount);
            }

            return notifications;
        }
        public async Task<Notification> MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.NotificationId == notificationId)
                ?? throw new Exception("Notification not found!");

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            // Update badge count via SignalR
            var unreadCount = await GetUnreadCount(
                notification.RecipientId);
            await _hubContext.Clients
                .Group($"user_{notification.RecipientId}")
                .SendAsync("UnreadCountUpdated", unreadCount);

            return notification;
        }

        public async Task MarkAllRead(int recipientId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == recipientId
                    && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
                n.IsRead = true;

            await _context.SaveChangesAsync();

            // Badge count is now 0
            await _hubContext.Clients
                .Group($"user_{recipientId}")
                .SendAsync("UnreadCountUpdated", 0);
        }

        public async Task DeleteRead(int recipientId) =>
            await _repository
                .DeleteByRecipientIdAndIsRead(recipientId, true);

        public async Task<List<Notification>> GetByRecipient(
            int recipientId) =>
            await _repository.FindByRecipientId(recipientId);

        public async Task<int> GetUnreadCount(int recipientId) =>
            await _repository
                .CountByRecipientIdAndIsRead(recipientId, false);

        public async Task DeleteNotification(int notificationId) =>
            await _repository.DeleteByNotificationId(notificationId);

        public async Task<List<Notification>> GetAll() =>
            await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task SendEmail(SendEmailDto dto)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(
                    _configuration["Email:From"]
                    ?? "noreply@codesync.com"));
                email.To.Add(MailboxAddress.Parse(dto.ToEmail));
                email.Subject = dto.Subject;
                email.Body = new TextPart("plain")
                {
                    Text = dto.Body
                };

                // For local testing - logs email instead of sending
                if (_configuration["Email:UseFakeSmtp"] == "true")
                {
                    _logger.LogInformation(
                        "FAKE EMAIL to: {To} Subject: {Subject} Body: {Body}",
                        dto.ToEmail, dto.Subject, dto.Body);
                    return;
                }

                // For production - sends real email via SMTP
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _configuration["Email:SmtpHost"],
                    int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
                    SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}",
                    dto.ToEmail);
            }
        }
    }
}