using NUnit.Framework;
using Moq;
using NotificationService.Services;
using NotificationService.Interfaces;
using NotificationService.Models;
using NotificationService.DTOs;
using NotificationService.Data;
using NotificationService.Hubs;
using NotificationService.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeSync.Tests
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private Mock<INotificationRepository> _notifRepoMock;
        private Mock<IHubContext<NotificationHub>> _hubContextMock;
        private Mock<IConfiguration> _configMock;
        private Mock<ILogger<NotificationServiceImpl>> _loggerMock;
        private Mock<IAuthServiceClient> _authClientMock;
        private NotificationDbContext _context;
        private NotificationServiceImpl _notifService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NotificationDbContext(options);
            _notifRepoMock = new Mock<INotificationRepository>();
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<NotificationServiceImpl>>();
            _authClientMock = new Mock<IAuthServiceClient>();

            // Mock full SignalR hub chain to prevent NullReferenceException
            _hubContextMock = new Mock<IHubContext<NotificationHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClientProxy.Setup(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Mock unread count repo call used by Send/MarkAsRead
            _notifRepoMock.Setup(r => r.CountByRecipientIdAndIsRead(It.IsAny<int>(), false))
                .ReturnsAsync(0);

            _notifService = new NotificationServiceImpl(
                _context,
                _notifRepoMock.Object,
                _hubContextMock.Object,
                _configMock.Object,
                _loggerMock.Object,
                _authClientMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // Test 1: Send should create notification with IsRead false by default
        [Test]
        public async Task Send_ShouldCreateNotification_WithIsReadFalse()
        {
            var dto = new SendNotificationDto
            {
                RecipientId = 2, ActorId = 1,
                Type = "SESSION_INVITE", Title = "Session Invite", Message = "Join my session"
            };

            var result = await _notifService.Send(dto);

            Assert.That(result.IsRead, Is.False);
            Assert.That(result.RecipientId, Is.EqualTo(2));
        }

        // Test 2: Send should store correct notification type
        [Test]
        public async Task Send_ShouldStoreCorrectType()
        {
            var dto = new SendNotificationDto
            {
                RecipientId = 2, ActorId = 1,
                Type = "SNAPSHOT", Title = "New Snapshot", Message = "A snapshot was created"
            };

            var result = await _notifService.Send(dto);

            Assert.That(result.Type, Is.EqualTo("SNAPSHOT"));
        }

        // Test 3: MarkAsRead should set IsRead to true
        [Test]
        public async Task MarkAsRead_ShouldSetIsRead_ToTrue()
        {
            var notification = new Notification
            {
                NotificationId = 1, RecipientId = 1, ActorId = 2,
                Type = "COMMENT", Title = "New Comment", Message = "Someone commented", IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var result = await _notifService.MarkAsRead(1);

            Assert.That(result.IsRead, Is.True);
        }

        // Test 4: GetUnreadCount should return correct count
        [Test]
        public async Task GetUnreadCount_ShouldReturnCorrectCount()
        {
            _notifRepoMock.Setup(r => r.CountByRecipientIdAndIsRead(1, false)).ReturnsAsync(3);

            var result = await _notifService.GetUnreadCount(1);

            Assert.That(result, Is.EqualTo(3));
        }

        // Test 5: GetByRecipient should return notifications for user
        [Test]
        public async Task GetByRecipient_ShouldReturnNotifications_ForUser()
        {
            var notifications = new List<Notification>
            {
                new Notification { NotificationId = 1, RecipientId = 1, ActorId=2, Type="COMMENT", Title="T1", Message="M1" },
                new Notification { NotificationId = 2, RecipientId = 1, ActorId=3, Type="SNAPSHOT", Title="T2", Message="M2" }
            };
            _notifRepoMock.Setup(r => r.FindByRecipientId(1)).ReturnsAsync(notifications);

            var result = await _notifService.GetByRecipient(1);

            Assert.That(result.Count, Is.EqualTo(2));
        }

        // Test 6: MarkAllRead should mark all notifications as read for user
        [Test]
        public async Task MarkAllRead_ShouldMarkAll_AsRead()
        {
            var notifications = new List<Notification>
            {
                new Notification { NotificationId = 1, RecipientId = 1, ActorId=2, Type="COMMENT", Title="T1", Message="M1", IsRead = false },
                new Notification { NotificationId = 2, RecipientId = 1, ActorId=3, Type="SNAPSHOT", Title="T2", Message="M2", IsRead = false }
            };
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            await _notifService.MarkAllRead(1);

            var updated = await _context.Notifications.Where(n => n.RecipientId == 1).ToListAsync();
            Assert.That(updated.All(n => n.IsRead), Is.True);
        }

        // Test 7: Send should store RelatedId for deep linking
        [Test]
        public async Task Send_ShouldStoreRelatedId_ForDeepLinking()
        {
            var dto = new SendNotificationDto
            {
                RecipientId = 2, ActorId = 1,
                Type = "SESSION_INVITE", Title = "Session", Message = "Join",
                RelatedId = "session-guid-123", RelatedType = "SESSION"
            };

            var result = await _notifService.Send(dto);

            Assert.That(result.RelatedId, Is.EqualTo("session-guid-123"));
            Assert.That(result.RelatedType, Is.EqualTo("SESSION"));
        }

        // Test 8: GetByRecipient should return empty list when no notifications
        [Test]
        public async Task GetByRecipient_ShouldReturnEmpty_WhenNoNotifications()
        {
            _notifRepoMock.Setup(r => r.FindByRecipientId(99)).ReturnsAsync(new List<Notification>());

            var result = await _notifService.GetByRecipient(99);

            Assert.That(result, Is.Empty);
        }

        // Test 9: DeleteNotification calls repository DeleteByNotificationId
        [Test]
        public async Task DeleteNotification_ShouldCallRepository_DeleteMethod()
        {
            _notifRepoMock.Setup(r => r.DeleteByNotificationId(1)).Returns(Task.CompletedTask);

            await _notifService.DeleteNotification(1);

            _notifRepoMock.Verify(r => r.DeleteByNotificationId(1), Times.Once);
        }

        // Test 10: SendBulk should create notifications for all recipients
        [Test]
        public async Task SendBulk_ShouldCreateNotifications_ForAllRecipients()
        {
            var dto = new SendBulkNotificationDto
            {
                RecipientIds = new List<int> { 1, 2, 3 },
                ActorId = 0, Type = "COMMENT",
                Title = "System Update", Message = "Maintenance tonight"
            };

            var result = await _notifService.SendBulk(dto);

            Assert.That(result.Count, Is.EqualTo(3));
        }

        // Test 11: GetUnreadCount should return 0 when all read
        [Test]
        public async Task GetUnreadCount_ShouldReturnZero_WhenAllRead()
        {
            _notifRepoMock.Setup(r => r.CountByRecipientIdAndIsRead(1, false)).ReturnsAsync(0);

            var result = await _notifService.GetUnreadCount(1);

            Assert.That(result, Is.EqualTo(0));
        }
    }
}