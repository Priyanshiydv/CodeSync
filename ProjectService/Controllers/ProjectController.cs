using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectService.DTOs;
using ProjectService.Interfaces;
using System.Security.Claims;

namespace ProjectService.Controllers
{
    /// <summary>
    /// Handles all HTTP requests for project management.
    /// Exposes /api/projects endpoints.
    /// </summary>
    [ApiController]
    [Route("api/projects")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        // Helper to get current user ID from JWT token
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateProject(
            [FromBody] CreateProjectDto dto)
        {
            try
            {
                var project = await _projectService.CreateProject(
                    GetUserId(), dto);
                return Ok(new { message = "Project created!", project });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetById(int projectId)
        {
            var project = await _projectService.GetProjectById(projectId);
            if (project == null) return NotFound();
            return Ok(project);
        }

        [HttpGet("owner/{ownerId}")]
        public async Task<IActionResult> GetByOwner(int ownerId)
        {
            var projects = await _projectService.GetProjectsByOwner(ownerId);
            return Ok(projects);
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicProjects()
        {
            var projects = await _projectService.GetPublicProjects();
            return Ok(projects);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var projects = await _projectService.SearchProjects(query);
            return Ok(projects);
        }

        [HttpGet("member")]
        [Authorize]
        public async Task<IActionResult> GetByMember()
        {
            var projects = await _projectService
                .GetProjectsByMember(GetUserId());
            return Ok(projects);
        }

        [HttpGet("language/{language}")]
        public async Task<IActionResult> GetByLanguage(string language)
        {
            var projects = await _projectService
                .GetProjectsByLanguage(language);
            return Ok(projects);
        }

        [HttpPut("{projectId}")]
        [Authorize]
        public async Task<IActionResult> Update(
            int projectId, [FromBody] UpdateProjectDto dto)
        {
            try
            {
                var project = await _projectService
                    .UpdateProject(projectId, dto);
                return Ok(new { message = "Project updated!", project });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{projectId}/archive")]
        [Authorize]
        public async Task<IActionResult> Archive(int projectId)
        {
            try
            {
                await _projectService.ArchiveProject(projectId);
                return Ok(new { message = "Project archived!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{projectId}/star")]
        [Authorize]
        public async Task<IActionResult> Star(int projectId)
        {
            try
            {
                await _projectService.StarProject(projectId);
                return Ok(new { message = "Project starred!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{projectId}")]
        [Authorize]
        public async Task<IActionResult> Delete(int projectId)
        {
            try
            {
                await _projectService.DeleteProject(projectId);
                return Ok(new { message = "Project deleted!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{projectId}/fork")]
        [Authorize]
        public async Task<IActionResult> Fork(int projectId)
        {
            try
            {
                var forked = await _projectService
                    .ForkProject(projectId, GetUserId());
                return Ok(new { message = "Project forked!", project = forked });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{projectId}/members")]
        public async Task<IActionResult> GetMembers(int projectId)
        {
            var members = await _projectService.GetMembers(projectId);
            return Ok(members);
        }

        [HttpPost("{projectId}/members")]
        [Authorize]
        public async Task<IActionResult> AddMember(
            int projectId, [FromBody] AddMemberDto dto)
        {
            try
            {
                await _projectService.AddMember(projectId, dto);
                return Ok(new { message = "Member added!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{projectId}/members/{userId}")]
        [Authorize]
        public async Task<IActionResult> RemoveMember(
            int projectId, int userId)
        {
            try
            {
                await _projectService.RemoveMember(projectId, userId);
                return Ok(new { message = "Member removed!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}