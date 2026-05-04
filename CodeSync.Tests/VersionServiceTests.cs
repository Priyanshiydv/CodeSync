using NUnit.Framework;
using Moq;
using VersionService.Services;
using VersionService.Interfaces;
using VersionService.Models;
using VersionService.DTOs;
using VersionService.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CodeSync.Tests
{
    [TestFixture]
    public class VersionServiceTests
    {
        private Mock<ISnapshotRepository> _snapshotRepoMock;
        private VersionDbContext _context;
        private VersionServiceImpl _versionService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<VersionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new VersionDbContext(options);
            _snapshotRepoMock = new Mock<ISnapshotRepository>();
            _versionService = new VersionServiceImpl(_context, _snapshotRepoMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // Helper: compute SHA-256 the same way VersionService does
        private static string ComputeHash(string content)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(bytes).ToLower();
        }

        // Test 1: CreateSnapshot should generate SHA-256 hash (64 hex chars)
        [Test]
        public async Task CreateSnapshot_ShouldGenerateSHA256Hash()
        {
            var dto = new CreateSnapshotDto
            {
                ProjectId = 1, FileId = 1,
                Message = "Initial commit", Content = "print('hello')", Branch = "main"
            };

            var result = await _versionService.CreateSnapshot(1, dto);

            Assert.That(result.Hash, Is.Not.Null);
            Assert.That(result.Hash.Length, Is.EqualTo(64)); // SHA-256 = 64 hex chars
        }

        // Test 2: CreateSnapshot should set branch correctly
        [Test]
        public async Task CreateSnapshot_ShouldSetBranch_ToMain_ByDefault()
        {
            var dto = new CreateSnapshotDto
            {
                ProjectId = 1, FileId = 1, Message = "First snapshot",
                Content = "code here", Branch = "main"
            };

            var result = await _versionService.CreateSnapshot(1, dto);

            Assert.That(result.Branch, Is.EqualTo("main"));
        }

        // Test 3: CreateSnapshot should store correct author ID
        [Test]
        public async Task CreateSnapshot_ShouldStoreCorrectAuthorId()
        {
            var dto = new CreateSnapshotDto
            {
                ProjectId = 1, FileId = 1, Message = "My commit",
                Content = "some code", Branch = "main"
            };

            var result = await _versionService.CreateSnapshot(5, dto);

            Assert.That(result.AuthorId, Is.EqualTo(5));
        }

        // Test 4: GetSnapshotById should return null when not found
        [Test]
        public async Task GetSnapshotById_ShouldReturnNull_WhenNotFound()
        {
            _snapshotRepoMock.Setup(r => r.FindBySnapshotId(999)).ReturnsAsync((Snapshot?)null);

            var result = await _versionService.GetSnapshotById(999);

            Assert.That(result, Is.Null);
        }

        // Test 5: GetSnapshotsByFile should return all snapshots for file
        [Test]
        public async Task GetSnapshotsByFile_ShouldReturnAllSnapshots_ForFile()
        {
            var snapshots = new List<Snapshot>
            {
                new Snapshot { SnapshotId = 1, FileId = 1, ProjectId = 1, AuthorId = 1, Message = "v1", Content = "c1", Hash = "h1", Branch = "main" },
                new Snapshot { SnapshotId = 2, FileId = 1, ProjectId = 1, AuthorId = 1, Message = "v2", Content = "c2", Hash = "h2", Branch = "main" }
            };
            _snapshotRepoMock.Setup(r => r.FindByFileId(1)).ReturnsAsync(snapshots);

            var result = await _versionService.GetSnapshotsByFile(1);

            Assert.That(result.Count, Is.EqualTo(2));
        }

        // Test 6: TagSnapshot should assign tag to snapshot
        [Test]
        public async Task TagSnapshot_ShouldAssignTag_ToSnapshot()
        {
            var snapshot = new Snapshot
            {
                SnapshotId = 1, FileId = 1, ProjectId = 1, AuthorId = 1,
                Message = "Release", Content = "code", Hash = "abc123", Branch = "main", Tag = null
            };
            _context.Snapshots.Add(snapshot);
            await _context.SaveChangesAsync();
            _snapshotRepoMock.Setup(r => r.FindBySnapshotId(1)).ReturnsAsync(snapshot);

            var result = await _versionService.TagSnapshot(new TagSnapshotDto { SnapshotId = 1, Tag = "v1.0.0" });

            Assert.That(result.Tag, Is.EqualTo("v1.0.0"));
        }

        // Test 7: RestoreSnapshot should create new snapshot with old content
        // IMPORTANT: Hash must match SHA-256 of content for integrity check to pass
        [Test]
        public async Task RestoreSnapshot_ShouldCreateNewSnapshot_WithOldContent()
        {
            var content = "old code content";
            var correctHash = ComputeHash(content); // must match what VersionService computes

            var original = new Snapshot
            {
                SnapshotId = 1, FileId = 1, ProjectId = 1, AuthorId = 1,
                Message = "Old version", Content = content,
                Hash = correctHash, Branch = "main"
            };
            _context.Snapshots.Add(original);
            await _context.SaveChangesAsync();
            _snapshotRepoMock.Setup(r => r.FindBySnapshotId(1)).ReturnsAsync(original);

            var result = await _versionService.RestoreSnapshot(1, 1);

            Assert.That(result.Content, Is.EqualTo(content));
            Assert.That(result.SnapshotId, Is.Not.EqualTo(1));
        }

        // Test 8: GetLatestSnapshot should return most recent snapshot
        [Test]
        public async Task GetLatestSnapshot_ShouldReturnMostRecentSnapshot()
        {
            var latest = new Snapshot
            {
                SnapshotId = 3, FileId = 1, ProjectId = 1, AuthorId = 1,
                Message = "Latest", Content = "latest code", Hash = "latesth", Branch = "main"
            };
            _snapshotRepoMock.Setup(r => r.FindLatestByFileId(1)).ReturnsAsync(latest);

            var result = await _versionService.GetLatestSnapshot(1);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.SnapshotId, Is.EqualTo(3));
        }

        // Test 9: CreateSnapshot should link ParentSnapshotId to previous snapshot
        [Test]
        public async Task CreateSnapshot_ShouldLinkParentSnapshotId()
        {
            var dto1 = new CreateSnapshotDto { ProjectId = 1, FileId = 1, Message = "First", Content = "code v1", Branch = "main" };
            var first = await _versionService.CreateSnapshot(1, dto1);

            var dto2 = new CreateSnapshotDto
            {
                ProjectId = 1, FileId = 1, Message = "Second", Content = "code v2",
                Branch = "main", ParentSnapshotId = first.SnapshotId
            };
            var second = await _versionService.CreateSnapshot(1, dto2);

            Assert.That(second.ParentSnapshotId, Is.EqualTo(first.SnapshotId));
        }

        // Test 10: GetSnapshotsByBranch should return only snapshots for that branch
        [Test]
        public async Task GetSnapshotsByBranch_ShouldReturnOnlyBranchSnapshots()
        {
            var featureSnapshots = new List<Snapshot>
            {
                new Snapshot { SnapshotId = 1, Branch = "feature/login", FileId=1, ProjectId=1, AuthorId=1, Message="f1", Content="c", Hash="h" }
            };
            _snapshotRepoMock.Setup(r => r.FindByBranch("feature/login")).ReturnsAsync(featureSnapshots);

            var result = await _versionService.GetSnapshotsByBranch("feature/login");

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Branch, Is.EqualTo("feature/login"));
        }

        // Test 11: DiffSnapshots should return diff result object
        [Test]
        public async Task DiffSnapshots_ShouldReturnDiffResult_ForTwoSnapshots()
        {
            var snap1 = new Snapshot { SnapshotId = 1, Content = "line1\nline2", FileId=1, ProjectId=1, AuthorId=1, Message="v1", Hash="h1", Branch="main" };
            var snap2 = new Snapshot { SnapshotId = 2, Content = "line1\nline3", FileId=1, ProjectId=1, AuthorId=1, Message="v2", Hash="h2", Branch="main" };
            _context.Snapshots.AddRange(snap1, snap2);
            await _context.SaveChangesAsync();
            _snapshotRepoMock.Setup(r => r.FindBySnapshotId(1)).ReturnsAsync(snap1);
            _snapshotRepoMock.Setup(r => r.FindBySnapshotId(2)).ReturnsAsync(snap2);

            var result = await _versionService.DiffSnapshots(1, 2);

            Assert.That(result, Is.Not.Null);
        }

        // Test 12: CreateBranch should create snapshot on new branch
        [Test]
        public async Task CreateBranch_ShouldCreateSnapshot_OnNewBranch()
        {
            var fromSnapshot = new Snapshot
            {
                SnapshotId = 1, FileId = 1, ProjectId = 1, AuthorId = 1,
                Message = "base", Content = "base code", Hash = "basehash", Branch = "main"
            };
            _context.Snapshots.Add(fromSnapshot);
            await _context.SaveChangesAsync();
            _snapshotRepoMock.Setup(r => r.FindBySnapshotId(1)).ReturnsAsync(fromSnapshot);

            var dto = new CreateBranchDto { BranchName = "feature/new", FromSnapshotId = 1 };

            var result = await _versionService.CreateBranch(1, dto);

            Assert.That(result.Branch, Is.EqualTo("feature/new"));
        }
    }
}