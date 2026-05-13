using ExecutionService.Data;
using ExecutionService.Interfaces;
using ExecutionService.Models;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Repositories
{
    /// <summary>
    /// Handles all database operations for ExecutionJob entity.
    /// </summary>
    public class ExecutionRepository : IExecutionRepository
    {
        private readonly ExecutionDbContext _context;

        public ExecutionRepository(ExecutionDbContext context)
        {
            _context = context;
        }

        public async Task<ExecutionJob?> FindByJobId(Guid jobId) =>
            await _context.ExecutionJobs
                .FirstOrDefaultAsync(j => j.JobId == jobId);

        public async Task<List<ExecutionJob>> FindByUserId(int userId) =>
            await _context.ExecutionJobs
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

        public async Task<List<ExecutionJob>> FindByProjectId(int projectId) =>
            await _context.ExecutionJobs
                .Where(j => j.ProjectId == projectId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

        public async Task<List<ExecutionJob>> FindByStatus(string status) =>
            await _context.ExecutionJobs
                .Where(j => j.Status == status)
                .ToListAsync();

        public async Task<List<ExecutionJob>> FindByLanguage(string language) =>
            await _context.ExecutionJobs
                .Where(j => j.Language == language)
                .ToListAsync();

        public async Task<List<ExecutionJob>> FindByCreatedAtBetween(
            DateTime start, DateTime end) =>
            await _context.ExecutionJobs
                .Where(j => j.CreatedAt >= start && j.CreatedAt <= end)
                .ToListAsync();

        public async Task<int> CountByUserId(int userId) =>
            await _context.ExecutionJobs
                .CountAsync(j => j.UserId == userId);
    }
}