using CommentService.DTOs;
using CommentService.Models;

namespace CommentService.Interfaces
{
    /// <summary>
    /// Defines all comment CRUD, reply threading,
    /// resolve/unresolve lifecycle and line-based retrieval operations.
    /// </summary>
    public interface ICommentService
    {
        Task<Comment> AddComment(int authorId, AddCommentDto dto);
        Task<List<Comment>> GetByFile(int fileId);
        Task<List<Comment>> GetByProject(int projectId);
        Task<Comment?> GetCommentById(int commentId);
        Task<List<Comment>> GetReplies(int parentCommentId);
        Task<Comment> UpdateComment(int commentId, UpdateCommentDto dto);
        Task DeleteComment(int commentId);
        Task<Comment> ResolveComment(int commentId);
        Task<Comment> UnresolveComment(int commentId);
        Task<List<Comment>> GetByLine(int fileId, int lineNumber);
        Task<int> GetCommentCount(int fileId);
    }
}