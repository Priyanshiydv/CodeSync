using ExecutionService.Data;
using ExecutionService.DTOs;
using ExecutionService.Exceptions;
using ExecutionService.Interfaces;
using ExecutionService.Models;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Services
{
    /// <summary>
    /// Implements code submission, job tracking, cancellation,
    /// result retrieval, language registry and usage stats.
    /// Jobs are processed asynchronously by ExecutionWorker.
    /// </summary>
    public class ExecutionServiceImpl : IExecutionService
    {
        private readonly ExecutionDbContext _context;
        private readonly IExecutionRepository _repository;

        public ExecutionServiceImpl(
            ExecutionDbContext context,
            IExecutionRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<ExecutionJob> SubmitExecution(
            int userId, SubmitExecutionDto dto)
        {
            var language = await _context.SupportedLanguages
                .FirstOrDefaultAsync(l =>
                    l.Name.ToLower() == dto.Language.ToLower()
                    && l.IsEnabled);

            if (language == null)
                throw new LanguageNotSupportedException(
                    $"Language '{dto.Language}' is not supported!");

            var job = new ExecutionJob
            {
                ProjectId = dto.ProjectId ?? 0,
                FileId = dto.FileId ?? 0,
                UserId = userId,
                Language = dto.Language,
                SourceCode = dto.SourceCode,
                Stdin = dto.Stdin,
                Status = "QUEUED"
            };

            _context.ExecutionJobs.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<ExecutionJob?> GetJobById(Guid jobId) =>
            await _repository.FindByJobId(jobId);

        public async Task<List<ExecutionJob>> GetExecutionsByUser(
            int userId) =>
            await _repository.FindByUserId(userId);

        public async Task<List<ExecutionJob>> GetExecutionsByProject(
            int projectId) =>
            await _repository.FindByProjectId(projectId);

        public async Task CancelExecution(Guid jobId)
        {
            var job = await _repository.FindByJobId(jobId)
                ?? throw new NotFoundException("Job not found!");

            if (job.Status != "QUEUED" && job.Status != "RUNNING")
                throw new JobCancelException(
                    $"Cannot cancel job with status '{job.Status}'!");

            job.Status = "CANCELLED";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<ExecutionJob?> GetExecutionResult(Guid jobId) =>
            await _repository.FindByJobId(jobId);

        public async Task<List<SupportedLanguage>> GetSupportedLanguages() =>
            await _context.SupportedLanguages
                .Where(l => l.IsEnabled)
                .ToListAsync();

        public async Task<string> GetLanguageVersion(string language)
        {
            var lang = await _context.SupportedLanguages
                .FirstOrDefaultAsync(l =>
                    l.Name.ToLower() == language.ToLower())
                ?? throw new NotFoundException(
                    $"Language '{language}' not found!");

            return lang.Version;
        }

        public async Task<object> GetExecutionStats(int userId)
        {
            var jobs = await _repository.FindByUserId(userId);

            return new
            {
                totalJobs = jobs.Count,
                completed = jobs.Count(j => j.Status == "COMPLETED"),
                failed = jobs.Count(j => j.Status == "FAILED"),
                cancelled = jobs.Count(j => j.Status == "CANCELLED"),
                timedOut = jobs.Count(j => j.Status == "TIMED_OUT"),
                avgExecutionTimeMs = jobs
                    .Where(j => j.ExecutionTimeMs.HasValue)
                    .Select(j => j.ExecutionTimeMs!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
                languageBreakdown = jobs
                    .GroupBy(j => j.Language)
                    .Select(g => new
                    {
                        language = g.Key,
                        count = g.Count()
                    }).ToList()
            };
        }
    }
}