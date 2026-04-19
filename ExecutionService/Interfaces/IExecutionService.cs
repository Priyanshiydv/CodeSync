using ExecutionService.DTOs;
using ExecutionService.Models;

namespace ExecutionService.Interfaces
{
    /// <summary>
    /// Defines code submission, async job tracking, cancellation,
    /// result retrieval, language registry and usage stats operations.
    /// </summary>
    public interface IExecutionService
    {
        Task<ExecutionJob> SubmitExecution(int userId, SubmitExecutionDto dto);
        Task<ExecutionJob?> GetJobById(Guid jobId);
        Task<List<ExecutionJob>> GetExecutionsByUser(int userId);
        Task<List<ExecutionJob>> GetExecutionsByProject(int projectId);
        Task CancelExecution(Guid jobId);
        Task<ExecutionJob?> GetExecutionResult(Guid jobId);
        Task<List<SupportedLanguage>> GetSupportedLanguages();
        Task<string> GetLanguageVersion(string language);
        Task<object> GetExecutionStats(int userId);
    }
}