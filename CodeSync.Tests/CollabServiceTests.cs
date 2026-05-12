using NUnit.Framework;
using Moq;
using CollabService.Services;
using CollabService.Interfaces;
using CollabService.Models;
using CollabService.DTOs;
using CollabService.Data;
using CollabService.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace CodeSync.Tests
{
    [TestFixture]
    public class CollabServiceTests
    {
        private Mock<ICollabRepository> _collabRepoMock;
        private Mock<IHubContext<CollabHub>> _hubContextMock;
        private OTService _otService;
        private CollabDbContext _context;
        private CollabServiceImpl _collabService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<CollabDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new CollabDbContext(options);
            _collabRepoMock = new Mock<ICollabRepository>();

            // OTService has no interface so use real instance (no constructor args)
            _otService = new OTService();

            // Mock full SignalR hub chain to avoid NullReferenceException
            _hubContextMock = new Mock<IHubContext<CollabHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClientProxy.Setup(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _collabService = new CollabServiceImpl(
                _context,
                _collabRepoMock.Object,
                _hubContextMock.Object,
                _otService);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // Test 1: CreateSession should return session with ACTIVE status
        [Test]
        public async Task CreateSession_ShouldReturnSession_WithActiveStatus()
        {
            var dto = new CreateSessionDto { ProjectId = 1, FileId = 1, Language = "Python", MaxParticipants = 5 };

            var result = await _collabService.CreateSession(1, dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("ACTIVE"));
            Assert.That(result.OwnerId, Is.EqualTo(1));
        }

        // Test 2: CreateSession should generate a unique GUID session ID
        [Test]
        public async Task CreateSession_ShouldGenerateUniqueSessionId()
        {
            var dto = new CreateSessionDto { ProjectId = 1, FileId = 1, Language = "Python" };

            var session1 = await _collabService.CreateSession(1, dto);
            var session2 = await _collabService.CreateSession(2, dto);

            Assert.That(session1.SessionId, Is.Not.EqualTo(session2.SessionId));
        }

        // Test 3: GetSessionById should return null when session not found
        [Test]
        public async Task GetSessionById_ShouldReturnNull_WhenSessionNotFound()
        {
            var fakeId = Guid.NewGuid();
            _collabRepoMock.Setup(r => r.FindBySessionId(fakeId)).ReturnsAsync((CollabSession?)null);

            var result = await _collabService.GetSessionById(fakeId);

            Assert.That(result, Is.Null);
        }

        // Test 4: EndSession should set status to ENDED
        [Test]
        public async Task EndSession_ShouldSetStatus_ToEnded()
        {
            var session = new CollabSession
            {
                SessionId = Guid.NewGuid(), ProjectId = 1, FileId = 1,
                OwnerId = 1, Language = "Python", Status = "ACTIVE"
            };
            _context.CollabSessions.Add(session);
            await _context.SaveChangesAsync();
            _collabRepoMock.Setup(r => r.FindBySessionId(session.SessionId)).ReturnsAsync(session);

            await _collabService.EndSession(session.SessionId);

            Assert.That(session.Status, Is.EqualTo("ENDED"));
        }

        // Test 5: JoinSession should throw when session not found
        [Test]
        public async Task JoinSession_ShouldThrow_WhenSessionNotFound()
        {
            var fakeId = Guid.NewGuid();
            _collabRepoMock.Setup(r => r.FindBySessionId(fakeId)).ReturnsAsync((CollabSession?)null);

            Assert.ThrowsAsync<Exception>(() => _collabService.JoinSession(fakeId, new JoinSessionDto { UserId = 1 }));
        }

        // Test 6: JoinSession should throw when session is ENDED
        [Test]
        public async Task JoinSession_ShouldThrow_WhenSessionIsEnded()
        {
            var session = new CollabSession
            {
                SessionId = Guid.NewGuid(), ProjectId = 1, FileId = 1,
                OwnerId = 1, Language = "Python", Status = "ENDED"
            };
            _context.CollabSessions.Add(session);
            await _context.SaveChangesAsync();
            _collabRepoMock.Setup(r => r.FindBySessionId(session.SessionId)).ReturnsAsync(session);

            Assert.ThrowsAsync<Exception>(() => _collabService.JoinSession(session.SessionId, new JoinSessionDto { UserId = 2 }));
        }

        // Test 7: CreateSession with password protection should store password
        [Test]
        public async Task CreateSession_ShouldStorePassword_WhenPasswordProtected()
        {
            var dto = new CreateSessionDto
            {
                ProjectId = 1, FileId = 1, Language = "Python",
                IsPasswordProtected = true, SessionPassword = "secret123"
            };

            var result = await _collabService.CreateSession(1, dto);

            Assert.That(result.IsPasswordProtected, Is.True);
            Assert.That(result.SessionPassword, Is.EqualTo("secret123"));
        }

        // Test 8: GetSessionsByProject should return sessions for project
        [Test]
        public async Task GetSessionsByProject_ShouldReturnSessions_ForProject()
        {
            var sessions = new List<CollabSession>
            {
                new CollabSession { SessionId = Guid.NewGuid(), ProjectId = 1, FileId = 1, OwnerId = 1, Language = "Python" },
                new CollabSession { SessionId = Guid.NewGuid(), ProjectId = 1, FileId = 2, OwnerId = 1, Language = "Python" }
            };
            _collabRepoMock.Setup(r => r.FindByProjectId(1)).ReturnsAsync(sessions);

            var result = await _collabService.GetSessionsByProject(1);

            Assert.That(result.Count, Is.EqualTo(2));
        }

        // Test 9: GetParticipants should return empty list when no participants
        [Test]
        public async Task GetParticipants_ShouldReturnEmptyList_WhenNoParticipants()
        {
            var sessionId = Guid.NewGuid();
            _collabRepoMock.Setup(r => r.FindParticipantsBySessionId(sessionId))
                .ReturnsAsync(new List<Participant>());

            var result = await _collabService.GetParticipants(sessionId);

            Assert.That(result, Is.Empty);
        }

        // Test 10: CreateSession should set MaxParticipants correctly
        [Test]
        public async Task CreateSession_ShouldSetMaxParticipants_Correctly()
        {
            var dto = new CreateSessionDto { ProjectId = 1, FileId = 1, Language = "Python", MaxParticipants = 8 };

            var result = await _collabService.CreateSession(1, dto);

            Assert.That(result.MaxParticipants, Is.EqualTo(8));
        }

        // Test 11: GetActiveSession should return null when no active session
        [Test]
        public async Task GetActiveSession_ShouldReturnNull_WhenNoActiveSession()
        {
            _collabRepoMock.Setup(r => r.FindActiveByProjectId(1)).ReturnsAsync(new List<CollabSession>());

            var result = await _collabService.GetActiveSession(1);

            Assert.That(result, Is.Null);
        }
    }
}