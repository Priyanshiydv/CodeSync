using NUnit.Framework;
using Moq;
using ExecutionService.Services;
using ExecutionService.Interfaces;
using ExecutionService.Models;
using ExecutionService.DTOs;
using ExecutionService.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeSync.Tests
{
    [TestFixture]
    public class ExecutionServiceTests
    {
        private Mock<IExecutionRepository> _executionRepoMock;
        private ExecutionDbContext _context;
        private ExecutionServiceImpl _executionService;

        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<ExecutionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ExecutionDbContext(options);

            // Seed supported languages into in-memory DB
            // (ExecutionService checks SupportedLanguages table before accepting jobs)
            _context.SupportedLanguages.AddRange(
                new SupportedLanguage { LanguageId = 1, Name = "Python", DockerImage = "python:3.11-slim", Version = "3.11", FileExtension = ".py", IsEnabled = true },
                new SupportedLanguage { LanguageId = 2, Name = "Java", DockerImage = "openjdk:17-slim", Version = "17.0", FileExtension = ".java", IsEnabled = true },
                new SupportedLanguage { LanguageId = 3, Name = "CSharp", DockerImage = "mcr.microsoft.com/dotnet/sdk:8.0", Version = "8.0", FileExtension = ".cs", IsEnabled = true },
                new SupportedLanguage { LanguageId = 4, Name = "C", DockerImage = "gcc:13-slim", Version = "13.0", FileExtension = ".c", IsEnabled = true },
                new SupportedLanguage { LanguageId = 5, Name = "C++", DockerImage = "gcc:13-slim", Version = "13.0", FileExtension = ".cpp", IsEnabled = true }
            );
            await _context.SaveChangesAsync();

            _executionRepoMock = new Mock<IExecutionRepository>();
            _executionService = new ExecutionServiceImpl(
                _context,
                _executionRepoMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // Test 1: SubmitExecution should create job with QUEUED status
        [Test]
        public async Task SubmitExecution_ShouldCreateJob_WithQueuedStatus()
        {
            var dto = new SubmitExecutionDto
            {
                ProjectId = 1,
                FileId = 1,
                Language = "Python",
                SourceCode = "print('hello')",
                Stdin = null
            };

            var result = await _executionService.SubmitExecution(1, dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("QUEUED"));
        }

        // Test 2: SubmitExecution should generate unique job GUID
        [Test]
        public async Task SubmitExecution_ShouldGenerateUniqueJobId()
        {
            var dto = new SubmitExecutionDto
            {
                ProjectId = 1, FileId = 1,
                Language = "Python", SourceCode = "print('a')"
            };

            var job1 = await _executionService.SubmitExecution(1, dto);
            var job2 = await _executionService.SubmitExecution(1, dto);

            Assert.That(job1.JobId, Is.Not.EqualTo(job2.JobId));
        }

        // Test 3: SubmitExecution should store correct language
        [Test]
        public async Task SubmitExecution_ShouldStoreCorrectLanguage()
        {
            var dto = new SubmitExecutionDto
            {
                ProjectId = 1, FileId = 1,
                Language = "Java",
                SourceCode = "System.out.println(\"hi\");"
            };

            var result = await _executionService.SubmitExecution(1, dto);

            Assert.That(result.Language, Is.EqualTo("Java"));
        }

        // Test 4: GetJobById should return null when job not found
        [Test]
        public async Task GetJobById_ShouldReturnNull_WhenJobNotFound()
        {
            var fakeId = Guid.NewGuid();
            _executionRepoMock.Setup(r => r.FindByJobId(fakeId))
                .ReturnsAsync((ExecutionJob?)null);

            var result = await _executionService.GetJobById(fakeId);

            Assert.That(result, Is.Null);
        }

        // Test 5: CancelExecution should set status to CANCELLED
        [Test]
        public async Task CancelExecution_ShouldSetStatus_ToCancelled()
        {
            var job = new ExecutionJob
            {
                JobId = Guid.NewGuid(),
                ProjectId = 1,
                FileId = 1,
                UserId = 1,
                Language = "Python",
                SourceCode = "while True: pass",
                Status = "RUNNING"
            };

            _context.ExecutionJobs.Add(job);
            await _context.SaveChangesAsync();
            _executionRepoMock.Setup(r => r.FindByJobId(job.JobId)).ReturnsAsync(job);

            await _executionService.CancelExecution(job.JobId);

            Assert.That(job.Status, Is.EqualTo("CANCELLED"));
        }

        // Test 6: GetExecutionsByUser should return jobs for that user
        [Test]
        public async Task GetExecutionsByUser_ShouldReturnJobs_ForUser()
        {
            var jobs = new List<ExecutionJob>
            {
                new ExecutionJob { JobId = Guid.NewGuid(), UserId = 1, ProjectId=1, FileId=1, Language="Python", SourceCode="p1", Status="COMPLETED" },
                new ExecutionJob { JobId = Guid.NewGuid(), UserId = 1, ProjectId=1, FileId=1, Language="Java", SourceCode="p2", Status="FAILED" }
            };

            _executionRepoMock.Setup(r => r.FindByUserId(1)).ReturnsAsync(jobs);

            var result = await _executionService.GetExecutionsByUser(1);

            Assert.That(result.Count, Is.EqualTo(2));
        }

        // Test 7: SubmitExecution should store stdin when provided
        [Test]
        public async Task SubmitExecution_ShouldStoreStdin_WhenProvided()
        {
            var dto = new SubmitExecutionDto
            {
                ProjectId = 1, FileId = 1,
                Language = "Python",
                SourceCode = "x = input()\nprint(x)",
                Stdin = "Hello World"
            };

            var result = await _executionService.SubmitExecution(1, dto);

            Assert.That(result.Stdin, Is.EqualTo("Hello World"));
        }

        // Test 8: GetSupportedLanguages should return non-empty list (seeded in setup)
        [Test]
        public async Task GetSupportedLanguages_ShouldReturnNonEmptyList()
        {
            var result = await _executionService.GetSupportedLanguages();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        // Test 9: GetExecutionsByProject should return jobs for project
        [Test]
        public async Task GetExecutionsByProject_ShouldReturnJobs_ForProject()
        {
            var jobs = new List<ExecutionJob>
            {
                new ExecutionJob { JobId = Guid.NewGuid(), UserId=1, ProjectId = 1, FileId=1, Language="Python", SourceCode="p1", Status="COMPLETED" }
            };

            _executionRepoMock.Setup(r => r.FindByProjectId(1)).ReturnsAsync(jobs);

            var result = await _executionService.GetExecutionsByProject(1);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ProjectId, Is.EqualTo(1));
        }

        // Test 10: SubmitExecution should store correct userId
        [Test]
        public async Task SubmitExecution_ShouldStoreCorrectUserId()
        {
            var dto = new SubmitExecutionDto
            {
                ProjectId = 1, FileId = 1,
                Language = "C",
                SourceCode = "#include<stdio.h>\nvoid main(){printf(\"hi\");}"
            };

            var result = await _executionService.SubmitExecution(7, dto);

            Assert.That(result.UserId, Is.EqualTo(7));
        }
    }
}