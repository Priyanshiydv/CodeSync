using CommentService.Models;

namespace CommentService.Interfaces
{
    /// <summary>
    /// Repository interface for Comment data access operations.
    /// </summary>
    public interface ICommentRepository
    {
        Task<List<Comment>> FindByFileId(int fileId);
        Task<List<Comment>> FindByProjectId(int projectId);
        Task<List<Comment>> FindByAuthorId(int authorId);
        Task<List<Comment>> FindByLineNumber(int fileId, int lineNumber);
        Task<List<Comment>> FindByParentCommentId(int parentCommentId);
        Task<int> CountByFileId(int fileId);
        Task<List<Comment>> FindByIsResolved(bool isResolved);
        Task DeleteByCommentId(int commentId);
    }
}