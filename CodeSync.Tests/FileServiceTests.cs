using NUnit.Framework;
using Moq;
using FileService.Services;
using FileService.Interfaces;
using FileService.Models;
using FileService.DTOs;
using FileService.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeSync.Tests
{
    [TestFixture]
    public class FileServiceTests
    {
        private Mock<IFileRepository> _fileRepoMock;
        private FileDbContext _context;
        private FileServiceImpl _fileService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<FileDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FileDbContext(options);
            _fileRepoMock = new Mock<IFileRepository>();
            _fileService = new FileServiceImpl(_context, _fileRepoMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // Test 1: CreateFile should return file with correct projectId
        [Test]
        public async Task CreateFile_ShouldReturnFile_WithCorrectProjectId()
        {
            var dto = new CreateFileDto
            {
                ProjectId = 1,
                Name = "main.py",
                Path = "main.py",
                Language = "Python",
                Content = "print('Hello')"
            };

            var result = await _fileService.CreateFile(1, dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ProjectId, Is.EqualTo(1));
            Assert.That(result.Name, Is.EqualTo("main.py"));
        }

        // Test 2: CreateFile should set IsDeleted to false by default
        [Test]
        public async Task CreateFile_ShouldSetIsDeleted_ToFalse()
        {
            var dto = new CreateFileDto
            {
                ProjectId = 1,
                Name = "App.cs",
                Path = "App.cs",
                Language = "CSharp",
                Content = ""
            };

            var result = await _fileService.CreateFile(1, dto);

            Assert.That(result.IsDeleted, Is.False);
        }

        // Test 3: GetFileById should return null when file not found
        [Test]
        public async Task GetFileById_ShouldReturnNull_WhenFileNotFound()
        {
            _fileRepoMock.Setup(r => r.FindByFileId(999))
                .ReturnsAsync((CodeFile?)null);

            var result = await _fileService.GetFileById(999);

            Assert.That(result, Is.Null);
        }

        // Test 4: GetFileContent should return correct content
        [Test]
        public async Task GetFileContent_ShouldReturnCorrectContent()
        {
            var file = new CodeFile
            {
                FileId = 1,
                ProjectId = 1,
                Name = "main.py",
                Path = "main.py",
                Language = "Python",
                Content = "print('Hello World')"
            };

            _fileRepoMock.Setup(r => r.FindByFileId(1)).ReturnsAsync(file);

            var result = await _fileService.GetFileContent(1);

            Assert.That(result, Is.EqualTo("print('Hello World')"));
        }

        // Test 5: DeleteFile should soft delete (set IsDeleted to true)
        [Test]
        public async Task DeleteFile_ShouldSetIsDeleted_ToTrue()
        {
            var file = new CodeFile
            {
                FileId = 1,
                ProjectId = 1,
                Name = "main.py",
                Path = "main.py",
                Language = "Python",
                Content = "",
                IsDeleted = false
            };

            _context.CodeFiles.Add(file);
            await _context.SaveChangesAsync();
            _fileRepoMock.Setup(r => r.FindByFileId(1)).ReturnsAsync(file);

            await _fileService.DeleteFile(1);

            Assert.That(file.IsDeleted, Is.True);
        }

        // Test 6: RestoreFile should set IsDeleted back to false
        [Test]
        public async Task RestoreFile_ShouldSetIsDeleted_ToFalse()
        {
            var file = new CodeFile
            {
                FileId = 1,
                ProjectId = 1,
                Name = "main.py",
                Path = "main.py",
                Language = "Python",
                Content = "",
                IsDeleted = true
            };

            _context.CodeFiles.Add(file);
            await _context.SaveChangesAsync();
            _fileRepoMock.Setup(r => r.FindByFileId(1)).ReturnsAsync(file);

            var result = await _fileService.RestoreFile(1);

            Assert.That(result.IsDeleted, Is.False);
        }

        // Test 7: CreateFolder should set IsFolder to true
        [Test]
        public async Task CreateFolder_ShouldSetIsFolder_ToTrue()
        {
            var dto = new CreateFolderDto
            {
                ProjectId = 1,
                Name = "src",
                Path = "src"
            };

            var result = await _fileService.CreateFolder(1, dto);

            Assert.That(result.IsFolder, Is.True);
        }

        // Test 8: RenameFile should update file name correctly
        [Test]
        public async Task RenameFile_ShouldUpdateFileName()
        {
            var file = new CodeFile
            {
                FileId = 1,
                ProjectId = 1,
                Name = "old_name.py",
                Path = "old_name.py",
                Language = "Python",
                Content = ""
            };

            _context.CodeFiles.Add(file);
            await _context.SaveChangesAsync();
            _fileRepoMock.Setup(r => r.FindByFileId(1)).ReturnsAsync(file);

            var dto = new RenameFileDto { NewName = "new_name.py" };
            var result = await _fileService.RenameFile(1, dto);

            Assert.That(result.Name, Is.EqualTo("new_name.py"));
        }

        // Test 9: UpdateFileContent should update content and LastEditedBy
        [Test]
        public async Task UpdateFileContent_ShouldUpdateContent_AndLastEditedBy()
        {
            var file = new CodeFile
            {
                FileId = 1,
                ProjectId = 1,
                Name = "main.py",
                Path = "main.py",
                Language = "Python",
                Content = "old content",
                LastEditedBy = 0
            };

            _context.CodeFiles.Add(file);
            await _context.SaveChangesAsync();
            _fileRepoMock.Setup(r => r.FindByFileId(1)).ReturnsAsync(file);

            var dto = new UpdateFileContentDto
            {
                Content = "new content",
                EditedByUserId = 5
            };

            var result = await _fileService.UpdateFileContent(1, dto);

            Assert.That(result.Content, Is.EqualTo("new content"));
            Assert.That(result.LastEditedBy, Is.EqualTo(5));
        }

        // Test 10: GetFilesByProject should return all files for a project
        [Test]
        public async Task GetFilesByProject_ShouldReturnAllFilesForProject()
        {
            var files = new List<CodeFile>
            {
                new CodeFile { FileId = 1, ProjectId = 1, Name = "main.py", Path="main.py", Language="Python" },
                new CodeFile { FileId = 2, ProjectId = 1, Name = "utils.py", Path="utils.py", Language="Python" }
            };

            _fileRepoMock.Setup(r => r.FindByProjectId(1)).ReturnsAsync(files);

            var result = await _fileService.GetFilesByProject(1);

            Assert.That(result.Count, Is.EqualTo(2));
        }

        // Test 11: SearchInProject should return files matching query - FIXED
        [Test]
        public async Task SearchInProject_ShouldReturnMatchingFiles()
        {
            // Seed the actual database context with test data
            var file1 = new CodeFile 
            { 
                FileId = 1, 
                ProjectId = 1, 
                Name = "main.py", 
                Path = "main.py", 
                Language = "Python", 
                Content = "print hello",
                IsDeleted = false
            };
            
            var file2 = new CodeFile 
            { 
                FileId = 2, 
                ProjectId = 1, 
                Name = "test.py", 
                Path = "test.py", 
                Language = "Python", 
                Content = "test content",
                IsDeleted = false
            };
            
            var file3 = new CodeFile 
            { 
                FileId = 3, 
                ProjectId = 1, 
                Name = "utils.py", 
                Path = "utils.py", 
                Language = "Python", 
                Content = "helper functions",
                IsDeleted = false
            };

            _context.CodeFiles.AddRange(file1, file2, file3);
            await _context.SaveChangesAsync();

            // Search for files containing "main" in name
            var result = await _fileService.SearchInProject(1, "main");

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("main.py"));
        }
    }
}