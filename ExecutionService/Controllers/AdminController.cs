using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExecutionService.Models;
using ExecutionService.Data;

namespace ExecutionService.Controllers
{
    /// <summary>
    /// Admin-only endpoints for execution monitoring and language management.
    /// All endpoints require ADMIN role.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN, Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ExecutionDbContext _context;

        public AdminController(ExecutionDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/executions
        [HttpGet("executions")]
        public async Task<IActionResult> GetAllExecutions()
        {
            var executions = await _context.ExecutionJobs
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.JobId,
                    e.ProjectId,
                    e.FileId,
                    e.UserId,
                    e.Language,
                    e.Status,
                    e.Stdout,
                    e.Stderr,
                    e.ExitCode,
                    e.ExecutionTimeMs,
                    e.MemoryUsedKb,
                    e.CreatedAt,
                    e.CompletedAt
                })
                .ToListAsync();

            return Ok(executions);
        }

        // POST: api/admin/executions/{jobId}/cancel
        [HttpPost("executions/{jobId}/cancel")]
        public async Task<IActionResult> CancelExecution(Guid jobId)
        {
            var job = await _context.ExecutionJobs.FindAsync(jobId);
            if (job == null)
                return NotFound(new { message = "Job not found" });

            if (job.Status != "QUEUED" && job.Status != "RUNNING")
                return BadRequest(new { message = $"Cannot cancel job with status '{job.Status}'" });

            job.Status = "CANCELLED";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Execution cancelled successfully" });
        }

        // GET: api/admin/languages
        [HttpGet("languages")]
        public async Task<IActionResult> GetAllLanguages()
        {
            var languages = await _context.SupportedLanguages
                .OrderBy(l => l.Name)
                .ToListAsync();

            return Ok(languages);
        }

        // POST: api/admin/languages
        [HttpPost("languages")]
        public async Task<IActionResult> AddLanguage([FromBody] AddLanguageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Language name is required" });

            if (string.IsNullOrWhiteSpace(request.DockerImage))
                return BadRequest(new { message = "Docker image is required" });

            var existing = await _context.SupportedLanguages
                .FirstOrDefaultAsync(l => l.Name.ToLower() == request.Name.ToLower());

            if (existing != null)
                return BadRequest(new { message = "Language already exists" });

            var language = new SupportedLanguage
            {
                Name = request.Name,
                DockerImage = request.DockerImage,
                Version = request.Version ?? "latest",
                FileExtension = request.FileExtension ?? $".{request.Name.ToLower()}",
                IsEnabled = true
            };

            _context.SupportedLanguages.Add(language);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Language added successfully", language });
        }

        // PUT: api/admin/languages/{id}
        [HttpPut("languages/{id}")]
        public async Task<IActionResult> UpdateLanguage(int id, [FromBody] UpdateLanguageRequest request)
        {
            var language = await _context.SupportedLanguages.FindAsync(id);
            if (language == null)
                return NotFound(new { message = "Language not found" });

            if (!string.IsNullOrWhiteSpace(request.Name))
                language.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.DockerImage))
                language.DockerImage = request.DockerImage;

            if (!string.IsNullOrWhiteSpace(request.Version))
                language.Version = request.Version;

            if (!string.IsNullOrWhiteSpace(request.FileExtension))
                language.FileExtension = request.FileExtension;

            if (request.IsEnabled.HasValue)
                language.IsEnabled = request.IsEnabled.Value;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Language updated successfully", language });
        }

        // DELETE: api/admin/languages/{id}
        [HttpDelete("languages/{id}")]
        public async Task<IActionResult> DeleteLanguage(int id)
        {
            var language = await _context.SupportedLanguages.FindAsync(id);
            if (language == null)
                return NotFound(new { message = "Language not found" });

            // Check if language has executions
            var hasExecutions = await _context.ExecutionJobs
                .AnyAsync(e => e.Language.ToLower() == language.Name.ToLower());

            if (hasExecutions)
            {
                // Soft delete - just disable
                language.IsEnabled = false;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Language has executions - disabled instead of deleted" });
            }

            _context.SupportedLanguages.Remove(language);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Language deleted successfully" });
        }

        // PUT: api/admin/languages/{id}/toggle
        [HttpPut("languages/{id}/toggle")]
        public async Task<IActionResult> ToggleLanguage(int id)
        {
            var language = await _context.SupportedLanguages.FindAsync(id);
            if (language == null)
                return NotFound(new { message = "Language not found" });

            language.IsEnabled = !language.IsEnabled;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Language {(language.IsEnabled ? "enabled" : "disabled")}",
                language
            });
        }

        // GET: api/admin/analytics/executions
        [HttpGet("analytics/executions")]
        public async Task<IActionResult> GetExecutionAnalytics()
        {
            var totalExecutions = await _context.ExecutionJobs.CountAsync();
            var completed = await _context.ExecutionJobs.CountAsync(e => e.Status == "COMPLETED");
            var failed = await _context.ExecutionJobs.CountAsync(e => e.Status == "FAILED");
            var timedOut = await _context.ExecutionJobs.CountAsync(e => e.Status == "TIMED_OUT");
            var cancelled = await _context.ExecutionJobs.CountAsync(e => e.Status == "CANCELLED");

            var avgExecutionTime = await _context.ExecutionJobs
                .Where(e => e.ExecutionTimeMs.HasValue)
                .AverageAsync(e => e.ExecutionTimeMs!.Value);

            var executionsByLanguage = await _context.ExecutionJobs
                .GroupBy(e => e.Language)
                .Select(g => new
                {
                    language = g.Key,
                    count = g.Count(),
                    avgTimeMs = g.Average(e => e.ExecutionTimeMs ?? 0)
                })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            var executionsByDay = await _context.ExecutionJobs
                .GroupBy(e => e.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.date)
                .Take(30)
                .ToListAsync();

            return Ok(new
            {
                totalExecutions,
                completed,
                failed,
                timedOut,
                cancelled,
                successRate = totalExecutions > 0 ? (double)completed / totalExecutions * 100 : 0,
                avgExecutionTimeMs = avgExecutionTime,
                executionsByLanguage,
                executionsByDay
            });
        }
    }

    public class AddLanguageRequest
    {
        public string Name { get; set; } = string.Empty;
        public string DockerImage { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? FileExtension { get; set; }
    }

    public class UpdateLanguageRequest
    {
        public string? Name { get; set; }
        public string? DockerImage { get; set; }
        public string? Version { get; set; }
        public string? FileExtension { get; set; }
        public bool? IsEnabled { get; set; }
    }
}