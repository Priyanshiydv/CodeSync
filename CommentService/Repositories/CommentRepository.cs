using CommentService.Data;
using CommentService.Interfaces;
using CommentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Repositories
{
    /// <summary>
    /// Handles all database operations for Comment entity.
    /// </summary>
    public class CommentRepository : ICommentRepository
    {
        private readonly CommentDbContext _context;

        public CommentRepository(CommentDbContext context)
        {
            _context = context;
        }

        public async Task<List<Comment>> FindByFileId(int fileId) =>
            await _context.Comments
                .Where(c => c.FileId == fileId)
                .OrderBy(c => c.LineNumber)
                .ToListAsync();

        public async Task<List<Comment>> FindByProjectId(int projectId) =>
            await _context.Comments
                .Where(c => c.ProjectId == projectId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

        public async Task<List<Comment>> FindByAuthorId(int authorId) =>
            await _context.Comments
                .Where(c => c.AuthorId == authorId)
                .ToListAsync();

        public async Task<List<Comment>> FindByLineNumber(
            int fileId, int lineNumber) =>
            await _context.Comments
                .Where(c => c.FileId == fileId
                    && c.LineNumber == lineNumber)
                .ToListAsync();

        public async Task<List<Comment>> FindByParentCommentId(
            int parentCommentId) =>
            await _context.Comments
                .Where(c => c.ParentCommentId == parentCommentId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

        public async Task<int> CountByFileId(int fileId) =>
            await _context.Comments
                .CountAsync(c => c.FileId == fileId);

        public async Task<List<Comment>> FindByIsResolved(bool isResolved) =>
            await _context.Comments
                .Where(c => c.IsResolved == isResolved)
                .ToListAsync();

        public async Task DeleteByCommentId(int commentId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
            }
        }
    }
}