using NUnit.Framework;
using Moq;
using CommentService.Services;
using CommentService.Interfaces;
using CommentService.Models;
using CommentService.DTOs;
using CommentService.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace CodeSync.Tests
{
    [TestFixture]
    public class CommentServiceTests
    {
        private Mock<ICommentRepository> _commentRepoMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private CommentDbContext _context;
        private CommentServiceImpl _commentService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<CommentDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new CommentDbContext(options);
            _commentRepoMock = new Mock<ICommentRepository>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Mock HttpClient so notification calls don't fail
            var mockHttpClient = new HttpClient(new MockHttpMessageHandler());
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient);

            _commentService = new CommentServiceImpl(
                _context,
                _commentRepoMock.Object,
                _httpClientFactoryMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // Test 1: AddComment should return comment with correct line number
        [Test]
        public async Task AddComment_ShouldReturnComment_WithCorrectLineNumber()
        {
            var dto = new AddCommentDto
            {
                ProjectId = 1,
                FileId = 1,
                Content = "This needs refactoring",
                LineNumber = 42
            };

            var result = await _commentService.AddComment(1, dto);

            Assert.That(result.LineNumber, Is.EqualTo(42));
            Assert.That(result.AuthorId, Is.EqualTo(1));
        }

        // Test 2: AddComment should set IsResolved to false by default
        [Test]
        public async Task AddComment_ShouldSetIsResolved_ToFalse()
        {
            var dto = new AddCommentDto
            {
                ProjectId = 1,
                FileId = 1,
                Content = "Check this logic",
                LineNumber = 10
            };

            var result = await _commentService.AddComment(1, dto);

            Assert.That(result.IsResolved, Is.False);
        }

        // Test 3: ResolveComment should set IsResolved to true
        [Test]
        public async Task ResolveComment_ShouldSetIsResolved_ToTrue()
        {
            var comment = new Comment
            {
                CommentId = 1,
                ProjectId = 1,
                FileId = 1,
                AuthorId = 1,
                Content = "Fix this",
                LineNumber = 5,
                IsResolved = false
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var result = await _commentService.ResolveComment(1);

            Assert.That(result.IsResolved, Is.True);
        }

        // Test 4: UnresolveComment should set IsResolved back to false
        [Test]
        public async Task UnresolveComment_ShouldSetIsResolved_ToFalse()
        {
            var comment = new Comment
            {
                CommentId = 1,
                ProjectId = 1,
                FileId = 1,
                AuthorId = 1,
                Content = "Old issue",
                LineNumber = 5,
                IsResolved = true
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var result = await _commentService.UnresolveComment(1);

            Assert.That(result.IsResolved, Is.False);
        }

        // Test 5: GetCommentById should return null when not found
        [Test]
        public async Task GetCommentById_ShouldReturnNull_WhenNotFound()
        {
            // Uses _context directly (no FindByCommentId in repo)
            var result = await _commentService.GetCommentById(999);

            Assert.That(result, Is.Null);
        }

        // Test 6: GetByLine should return comments on specific line
        [Test]
        public async Task GetByLine_ShouldReturnComments_ForSpecificLine()
        {
            var comments = new List<Comment>
            {
                new Comment { CommentId = 1, FileId = 1, LineNumber = 10, ProjectId=1, AuthorId=1, Content="c1" },
                new Comment { CommentId = 2, FileId = 1, LineNumber = 10, ProjectId=1, AuthorId=1, Content="c2" }
            };

            _commentRepoMock.Setup(r => r.FindByLineNumber(1, 10)).ReturnsAsync(comments);

            var result = await _commentService.GetByLine(1, 10);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(c => c.LineNumber == 10), Is.True);
        }

        // Test 7: AddComment should support threaded replies via ParentCommentId
        [Test]
        public async Task AddComment_ShouldSupportReplies_ViaParentCommentId()
        {
            // Parent comment must exist first
            var parent = new Comment
            {
                CommentId = 1,
                ProjectId = 1,
                FileId = 1,
                AuthorId = 1,
                Content = "Parent comment",
                LineNumber = 5
            };
            _context.Comments.Add(parent);
            await _context.SaveChangesAsync();

            var dto = new AddCommentDto
            {
                ProjectId = 1,
                FileId = 1,
                Content = "Agreed with this",
                LineNumber = 5,
                ParentCommentId = 1
            };

            var result = await _commentService.AddComment(2, dto);

            Assert.That(result.ParentCommentId, Is.EqualTo(1));
        }

        // Test 8: GetReplies should return replies for a comment
        [Test]
        public async Task GetReplies_ShouldReturnReplies_ForParentComment()
        {
            var replies = new List<Comment>
            {
                new Comment { CommentId = 2, ParentCommentId = 1, FileId=1, ProjectId=1, AuthorId=2, Content="reply", LineNumber=5 },
                new Comment { CommentId = 3, ParentCommentId = 1, FileId=1, ProjectId=1, AuthorId=3, Content="reply2", LineNumber=5 }
            };

            _commentRepoMock.Setup(r => r.FindByParentCommentId(1)).ReturnsAsync(replies);

            var result = await _commentService.GetReplies(1);

            Assert.That(result.Count, Is.EqualTo(2));
        }

        // Test 9: GetCommentCount should return correct count for file
        [Test]
        public async Task GetCommentCount_ShouldReturnCorrectCount_ForFile()
        {
            _commentRepoMock.Setup(r => r.CountByFileId(1)).ReturnsAsync(5);

            var result = await _commentService.GetCommentCount(1);

            Assert.That(result, Is.EqualTo(5));
        }

        // Test 10: GetByFile should return all comments for a file
        [Test]
        public async Task GetByFile_ShouldReturnAllComments_ForFile()
        {
            var comments = new List<Comment>
            {
                new Comment { CommentId = 1, FileId = 1, ProjectId=1, AuthorId=1, Content="c1", LineNumber=1 },
                new Comment { CommentId = 2, FileId = 1, ProjectId=1, AuthorId=1, Content="c2", LineNumber=2 },
                new Comment { CommentId = 3, FileId = 1, ProjectId=1, AuthorId=2, Content="c3", LineNumber=3 }
            };

            _commentRepoMock.Setup(r => r.FindByFileId(1)).ReturnsAsync(comments);

            var result = await _commentService.GetByFile(1);

            Assert.That(result.Count, Is.EqualTo(3));
        }

        // Test 11: UpdateComment should update content correctly
        [Test]
        public async Task UpdateComment_ShouldUpdateContent_Correctly()
        {
            var comment = new Comment
            {
                CommentId = 1,
                ProjectId = 1,
                FileId = 1,
                AuthorId = 1,
                Content = "Old content",
                LineNumber = 5
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var dto = new UpdateCommentDto { Content = "Updated content" };
            var result = await _commentService.UpdateComment(1, dto);

            Assert.That(result.Content, Is.EqualTo("Updated content"));
        }
    }

    // Helper class to mock HttpClient responses (so notification calls don't throw)
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}