using ExecutionService.DTOs;
using ExecutionService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExecutionService.Controllers
{
    /// <summary>
    /// Handles all HTTP requests for code execution management.
    /// Exposes /api/executions endpoints.
    /// </summary>
    [ApiController]
    [Route("api/executions")]
    public class ExecutionController : ControllerBase
    {
        private readonly IExecutionService _executionService;

        public ExecutionController(IExecutionService executionService)
        {
            _executionService = executionService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> Submit(
            [FromBody] SubmitExecutionDto dto)
        {
            try
            {
                var job = await _executionService
                    .SubmitExecution(GetUserId(), dto);
                return Ok(new
                {
                    message = "Job submitted!",
                    jobId = job.JobId,
                    status = job.Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{jobId}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid jobId)
        {
            var job = await _executionService.GetJobById(jobId);
            if (job == null) return NotFound();
            return Ok(job);
        }

        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var jobs = await _executionService
                .GetExecutionsByUser(userId);
            return Ok(jobs);
        }

        [HttpGet("project/{projectId}")]
        [Authorize]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            var jobs = await _executionService
                .GetExecutionsByProject(projectId);
            return Ok(jobs);
        }

        [HttpPost("{jobId}/cancel")]
        [Authorize]
        public async Task<IActionResult> Cancel(Guid jobId)
        {
            try
            {
                await _executionService.CancelExecution(jobId);
                return Ok(new { message = "Job cancelled!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{jobId}/result")]
        [Authorize]
        public async Task<IActionResult> GetResult(Guid jobId)
        {
            var job = await _executionService.GetExecutionResult(jobId);
            if (job == null) return NotFound();
            return Ok(job);
        }

        [HttpGet("languages")]
        public async Task<IActionResult> GetSupportedLanguages()
        {
            var languages = await _executionService
                .GetSupportedLanguages();
            return Ok(languages);
        }

        [HttpGet("languages/{language}/version")]
        public async Task<IActionResult> GetLanguageVersion(string language)
        {
            try
            {
                var version = await _executionService
                    .GetLanguageVersion(language);
                return Ok(new { language, version });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _executionService
                .GetExecutionStats(GetUserId());
            return Ok(stats);
        }
    }
}