using NUnit.Framework;
using Moq;
using ProjectService.Services;
using ProjectService.Interfaces;
using ProjectService.Models;
using ProjectService.DTOs;
using ProjectService.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeSync.Tests
{
    [TestFixture]
    public class ProjectServiceTests
    {
        private Mock<IProjectRepository> _projectRepoMock;
        private ProjectDbContext _context;
        private ProjectServiceImpl _projectService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ProjectDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ProjectDbContext(options);
            _projectRepoMock = new Mock<IProjectRepository>();
            _projectService = new ProjectServiceImpl(_context, _projectRepoMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // Test 1: CreateProject should return project with correct owner
        [Test]
        public async Task CreateProject_ShouldReturnProject_WithCorrectOwnerId()
        {
            var dto = new CreateProjectDto
            {
                Name = "My Project",
                Description = "Test project",
                Language = "Python",
                Visibility = "PUBLIC"
            };

            var result = await _projectService.CreateProject(1, dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.OwnerId, Is.EqualTo(1));
            Assert.That(result.Name, Is.EqualTo("My Project"));
        }

        // Test 2: CreateProject should set PUBLIC visibility by default
        [Test]
        public async Task CreateProject_ShouldSetPublicVisibility_WhenSpecified()
        {
            var dto = new CreateProjectDto
            {
                Name = "Public Project",
                Language = "Java",
                Visibility = "PUBLIC"
            };

            var result = await _projectService.CreateProject(1, dto);

            Assert.That(result.Visibility, Is.EqualTo("PUBLIC"));
        }

        // Test 3: GetProjectById should return null when project not found
        [Test]
        public async Task GetProjectById_ShouldReturnNull_WhenProjectNotFound()
        {
            _projectRepoMock.Setup(r => r.FindByProjectId(999))
                .ReturnsAsync((Project?)null);

            var result = await _projectService.GetProjectById(999);

            Assert.That(result, Is.Null);
        }

        // Test 4: GetProjectById should return project when found
        [Test]
        public async Task GetProjectById_ShouldReturnProject_WhenProjectExists()
        {
            var project = new Project
            {
                ProjectId = 1,
                Name = "Test Project",
                OwnerId = 1,
                Language = "Python",
                Visibility = "PUBLIC"
            };

            _projectRepoMock.Setup(r => r.FindByProjectId(1)).ReturnsAsync(project);

            var result = await _projectService.GetProjectById(1);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ProjectId, Is.EqualTo(1));
        }

        // Test 5: GetPublicProjects should return only public projects
        [Test]
        public async Task GetPublicProjects_ShouldReturnOnlyPublicProjects()
        {
            var publicProjects = new List<Project>
            {
                new Project { ProjectId = 1, Visibility = "PUBLIC", Name = "P1", Language = "Python" },
                new Project { ProjectId = 2, Visibility = "PUBLIC", Name = "P2", Language = "Java" }
            };

            _projectRepoMock.Setup(r => r.FindByVisibility("PUBLIC"))
                .ReturnsAsync(publicProjects);

            var result = await _projectService.GetPublicProjects();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(p => p.Visibility == "PUBLIC"), Is.True);
        }

        // Test 6: StarProject should increment star count
        [Test]
        public async Task StarProject_ShouldIncrementStarCount()
        {
            var project = new Project
            {
                ProjectId = 1,
                Name = "Test",
                Language = "Python",
                Visibility = "PUBLIC",
                StarCount = 5
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            _projectRepoMock.Setup(r => r.FindByProjectId(1)).ReturnsAsync(project);

            await _projectService.StarProject(1);

            Assert.That(project.StarCount, Is.EqualTo(6));
        }

        // Test 7: ArchiveProject should set IsArchived to true
        [Test]
        public async Task ArchiveProject_ShouldSetIsArchived_ToTrue()
        {
            var project = new Project
            {
                ProjectId = 1,
                Name = "Test",
                Language = "Python",
                Visibility = "PUBLIC",
                IsArchived = false
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            _projectRepoMock.Setup(r => r.FindByProjectId(1)).ReturnsAsync(project);

            await _projectService.ArchiveProject(1);

            Assert.That(project.IsArchived, Is.True);
        }

        // Test 8: ForkProject should create new project with incremented fork count
        [Test]
        public async Task ForkProject_ShouldIncrementForkCount_OnOriginalProject()
        {
            var original = new Project
            {
                ProjectId = 1,
                Name = "Original",
                OwnerId = 1,
                Language = "Python",
                Visibility = "PUBLIC",
                ForkCount = 0
            };

            _context.Projects.Add(original);
            await _context.SaveChangesAsync();
            _projectRepoMock.Setup(r => r.FindByProjectId(1)).ReturnsAsync(original);

            await _projectService.ForkProject(1, 2);

            Assert.That(original.ForkCount, Is.EqualTo(1));
        }

        // Test 9: GetProjectsByOwner should return owner's projects
        [Test]
        public async Task GetProjectsByOwner_ShouldReturnProjectsForOwner()
        {
            var projects = new List<Project>
            {
                new Project { ProjectId = 1, OwnerId = 1, Name = "P1", Language = "Python" },
                new Project { ProjectId = 2, OwnerId = 1, Name = "P2", Language = "Java" }
            };

            _projectRepoMock.Setup(r => r.FindByOwnerId(1)).ReturnsAsync(projects);

            var result = await _projectService.GetProjectsByOwner(1);

            Assert.That(result.Count, Is.EqualTo(2));
        }

        // Test 10: SearchProjects should return matching projects
        [Test]
        public async Task SearchProjects_ShouldReturnMatchingProjects()
        {
            var projects = new List<Project>
            {
                new Project { ProjectId = 1, Name = "Snake Game", Language = "Python" }
            };

            _projectRepoMock.Setup(r => r.SearchByName("Snake")).ReturnsAsync(projects);

            var result = await _projectService.SearchProjects("Snake");

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Snake Game"));
        }

        // Test 11: GetProjectsByLanguage should return projects with that language
        [Test]
        public async Task GetProjectsByLanguage_ShouldReturnCorrectProjects()
        {
            var projects = new List<Project>
            {
                new Project { ProjectId = 1, Language = "Python", Name = "P1" },
                new Project { ProjectId = 2, Language = "Python", Name = "P2" }
            };

            _projectRepoMock.Setup(r => r.FindByLanguage("Python")).ReturnsAsync(projects);

            var result = await _projectService.GetProjectsByLanguage("Python");

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(p => p.Language == "Python"), Is.True);
        }

        // Test 12: DeleteProject should remove project from database
        [Test]
        public async Task DeleteProject_ShouldRemoveProject_FromDatabase()
        {
            var project = new Project
            {
                ProjectId = 1,
                Name = "To Delete",
                OwnerId = 1,
                Language = "Python",
                Visibility = "PUBLIC"
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            _projectRepoMock.Setup(r => r.FindByProjectId(1)).ReturnsAsync(project);

            await _projectService.DeleteProject(1);

            var deleted = await _context.Projects.FindAsync(1);
            Assert.That(deleted, Is.Null);
        }
    }
}