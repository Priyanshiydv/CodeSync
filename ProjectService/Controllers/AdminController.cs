using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectService.Data;

namespace ProjectService.Controllers
{
    /// <summary>
    /// Admin-only endpoints for project management and platform analytics.
    /// All endpoints require ADMIN role.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN, Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ProjectDbContext _context;

        public AdminController(ProjectDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/projects
        [HttpGet("projects")]
        public async Task<IActionResult> GetAllProjects()
        {
            var projects = await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.ProjectId,
                    p.Name,
                    p.Description,
                    p.Language,
                    p.Visibility,
                    p.OwnerId,
                    p.IsArchived,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.StarCount,
                    p.ForkCount
                })
                .ToListAsync();

            // Get owner names (would require HTTP call to AuthService in production)
            // For now, return as is - frontend can fetch user details separately

            return Ok(projects);
        }

        // DELETE: api/admin/projects/{id}
        [HttpDelete("projects/{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { message = "Project not found" });

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Project deleted successfully" });
        }

        // PUT: api/admin/projects/{id}/archive
        [HttpPut("projects/{id}/archive")]
        public async Task<IActionResult> ArchiveProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { message = "Project not found" });

            project.IsArchived = true;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Project archived successfully" });
        }

        // PUT: api/admin/projects/{id}/unarchive
        [HttpPut("projects/{id}/unarchive")]
        public async Task<IActionResult> UnarchiveProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { message = "Project not found" });

            project.IsArchived = false;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Project unarchived successfully" });
        }

        // GET: api/admin/analytics
        [HttpGet("analytics")]
        public async Task<IActionResult> GetPlatformStats()
        {
            var totalProjects = await _context.Projects.CountAsync();
            var publicProjects = await _context.Projects.CountAsync(p => p.Visibility == "PUBLIC");
            var privateProjects = await _context.Projects.CountAsync(p => p.Visibility == "PRIVATE");
            var archivedProjects = await _context.Projects.CountAsync(p => p.IsArchived);
            
            var totalStars = await _context.Projects.SumAsync(p => p.StarCount);
            var totalForks = await _context.Projects.SumAsync(p => p.ForkCount);

            var projectsByLanguage = await _context.Projects
                .GroupBy(p => p.Language)
                .Select(g => new
                {
                    language = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            var recentProjects = await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new
                {
                    p.ProjectId,
                    p.Name,
                    p.Language,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalProjects,
                publicProjects,
                privateProjects,
                archivedProjects,
                totalStars,
                totalForks,
                projectsByLanguage,
                recentProjects
            });
        }

        // GET: api/admin/analytics/languages
        [HttpGet("analytics/languages")]
        public async Task<IActionResult> GetLanguageStats()
        {
            var stats = await _context.Projects
                .GroupBy(p => p.Language)
                .Select(g => new
                {
                    language = g.Key,
                    count = g.Count(),
                    stars = g.Sum(p => p.StarCount),
                    forks = g.Sum(p => p.ForkCount)
                })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            return Ok(stats);
        }
    }
}