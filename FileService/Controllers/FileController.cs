using FileService.DTOs;
using FileService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileService.Controllers
{
    /// <summary>
    /// Handles all HTTP requests for file and folder management.
    /// Exposes /api/files endpoints.
    /// </summary>
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateFile(
            [FromBody] CreateFileDto dto)
        {
            try
            {
                var file = await _fileService.CreateFile(GetUserId(), dto);
                return Ok(new { message = "File created!", file });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("createFolder")]
        [Authorize]
        public async Task<IActionResult> CreateFolder(
            [FromBody] CreateFolderDto dto)
        {
            try
            {
                var folder = await _fileService
                    .CreateFolder(GetUserId(), dto);
                return Ok(new { message = "Folder created!", folder });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{fileId}")]
        public async Task<IActionResult> GetById(int fileId)
        {
            var file = await _fileService.GetFileById(fileId);
            if (file == null) return NotFound();
            return Ok(file);
        }

        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            var files = await _fileService.GetFilesByProject(projectId);
            return Ok(files);
        }

        [HttpGet("{fileId}/content")]
        public async Task<IActionResult> GetContent(int fileId)
        {
            try
            {
                var content = await _fileService.GetFileContent(fileId);
                return Ok(new { content });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("tree/{projectId}")]
        public async Task<IActionResult> GetFileTree(int projectId)
        {
            var tree = await _fileService.GetFileTree(projectId);
            return Ok(tree);
        }

        [HttpGet("search/{projectId}")]
        public async Task<IActionResult> Search(
            int projectId, [FromQuery] string query)
        {
            var results = await _fileService
                .SearchInProject(projectId, query);
            return Ok(results);
        }

        [HttpPut("{fileId}/content")]
        [Authorize]
        public async Task<IActionResult> UpdateContent(
            int fileId, [FromBody] UpdateFileContentDto dto)
        {
            try
            {
                var file = await _fileService
                    .UpdateFileContent(fileId, dto);
                return Ok(new { message = "Content updated!", file });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{fileId}/rename")]
        [Authorize]
        public async Task<IActionResult> Rename(
            int fileId, [FromBody] RenameFileDto dto)
        {
            try
            {
                var file = await _fileService.RenameFile(fileId, dto);
                return Ok(new { message = "File renamed!", file });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{fileId}/move")]
        [Authorize]
        public async Task<IActionResult> Move(
            int fileId, [FromBody] MoveFileDto dto)
        {
            try
            {
                var file = await _fileService.MoveFile(fileId, dto);
                return Ok(new { message = "File moved!", file });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{fileId}")]
        [Authorize]
        public async Task<IActionResult> Delete(int fileId)
        {
            try
            {
                await _fileService.DeleteFile(fileId);
                return Ok(new { message = "File deleted (soft)!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{fileId}/restore")]
        [Authorize]
        public async Task<IActionResult> Restore(int fileId)
        {
            try
            {
                var file = await _fileService.RestoreFile(fileId);
                return Ok(new { message = "File restored!", file });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}