using CommentService.Data;
using CommentService.DTOs;
using CommentService.Interfaces;
using CommentService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CommentService.Services
{
    /// <summary>
    /// Implements all comment CRUD, reply threading,
    /// resolve/unresolve lifecycle and @mention parsing operations.
    /// </summary>
    public class CommentServiceImpl : ICommentService
    {
        private readonly CommentDbContext _context;
        private readonly ICommentRepository _repository;

        public CommentServiceImpl(
            CommentDbContext context,
            ICommentRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        /// <summary>
        /// Parses @mentions from comment content
        /// e.g. "@john fixed this" → ["john"]
        /// </summary>
        private static List<string> ParseMentions(string content)
        {
            var matches = Regex.Matches(content, @"@(\w+)");
            return matches.Select(m => m.Groups[1].Value).ToList();
        }

        public async Task<Comment> AddComment(
            int authorId, AddCommentDto dto)
        {
            // Validate parent comment exists if reply
            if (dto.ParentCommentId.HasValue)
            {
                var parent = await _context.Comments
                    .FirstOrDefaultAsync(c =>
                        c.CommentId == dto.ParentCommentId.Value);
                if (parent == null)
                    throw new Exception("Parent comment not found!");
            }

            var comment = new Comment
            {
                ProjectId = dto.ProjectId,
                FileId = dto.FileId,
                AuthorId = authorId,
                Content = dto.Content,
                LineNumber = dto.LineNumber,
                ColumnNumber = dto.ColumnNumber,
                ParentCommentId = dto.ParentCommentId,
                SnapshotId = dto.SnapshotId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Parse @mentions and log them
            // In production this would call NotificationService via HttpClient
            var mentions = ParseMentions(dto.Content);
            if (mentions.Any())
            {
                Console.WriteLine(
                    $"Mentions found: {string.Join(", ", mentions)}");
            }

            return comment;
        }

        public async Task<List<Comment>> GetByFile(int fileId) =>
            await _repository.FindByFileId(fileId);

        public async Task<List<Comment>> GetByProject(int projectId) =>
            await _repository.FindByProjectId(projectId);

        public async Task<Comment?> GetCommentById(int commentId) =>
            await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

        public async Task<List<Comment>> GetReplies(int parentCommentId) =>
            await _repository.FindByParentCommentId(parentCommentId);

        public async Task<Comment> UpdateComment(
            int commentId, UpdateCommentDto dto)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId)
                ?? throw new Exception("Comment not found!");

            comment.Content = dto.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            // Re-parse mentions after update
            var mentions = ParseMentions(dto.Content);
            if (mentions.Any())
            {
                Console.WriteLine(
                    $"Updated mentions: {string.Join(", ", mentions)}");
            }

            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task DeleteComment(int commentId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId)
                ?? throw new Exception("Comment not found!");

            // Also delete all replies to this comment
            var replies = await _repository
                .FindByParentCommentId(commentId);
            _context.Comments.RemoveRange(replies);
            _context.Comments.Remove(comment);

            await _context.SaveChangesAsync();
        }

        public async Task<Comment> ResolveComment(int commentId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId)
                ?? throw new Exception("Comment not found!");

            comment.IsResolved = true;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<Comment> UnresolveComment(int commentId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId)
                ?? throw new Exception("Comment not found!");

            comment.IsResolved = false;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<List<Comment>> GetByLine(
            int fileId, int lineNumber) =>
            await _repository.FindByLineNumber(fileId, lineNumber);

        public async Task<int> GetCommentCount(int fileId) =>
            await _repository.CountByFileId(fileId);
    }
}