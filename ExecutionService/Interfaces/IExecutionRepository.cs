using ExecutionService.Models;

namespace ExecutionService.Interfaces
{
    /// <summary>
    /// Repository interface for ExecutionJob data access operations.
    /// </summary>
    public interface IExecutionRepository
    {
        Task<ExecutionJob?> FindByJobId(Guid jobId);
        Task<List<ExecutionJob>> FindByUserId(int userId);
        Task<List<ExecutionJob>> FindByProjectId(int projectId);
        Task<List<ExecutionJob>> FindByStatus(string status);
        Task<List<ExecutionJob>> FindByLanguage(string language);
        Task<List<ExecutionJob>> FindByCreatedAtBetween(
            DateTime start, DateTime end);
        Task<int> CountByUserId(int userId);
    }
}