using CommentService.Data;
using CommentService.DTOs;
using CommentService.Interfaces;
using CommentService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;

namespace CommentService.Services
{
    public class CommentServiceImpl : ICommentService
    {
        private readonly CommentDbContext _context;
        private readonly ICommentRepository _repository;
        // ADD — HttpClient to call NotificationService
        private readonly IHttpClientFactory _httpClientFactory;
        // ADD — NotificationService base URL
        private const string NotificationServiceUrl = "http://localhost:5857/api/notifications";

        public CommentServiceImpl(
            CommentDbContext context,
            ICommentRepository repository,
            // ADD — inject IHttpClientFactory
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _repository = repository;
            _httpClientFactory = httpClientFactory;
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

        /// <summary>
        /// ADD — Calls NotificationService to send a MENTION notification
        /// for each @username found in the comment content.
        /// Case study §4.7 — @mentions trigger HttpClient notification calls
        /// </summary>
        private async Task SendMentionNotificationsAsync(
            Comment comment, List<string> mentionedUsernames)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                foreach (var username in mentionedUsernames)
                {
                    var payload = new
                    {
                        // Who triggered the mention
                        actorId = comment.AuthorId,
                        // Type matches Notification model §4.8
                        type = "MENTION",
                        title = $"@{username} mentioned you in a comment",
                        message = comment.Content,
                        // RelatedId = CommentId as required by case study §2.8
                        relatedId = comment.CommentId.ToString(),
                        relatedType = "Comment",
                        // Deep-link URL so recipient can jump to the comment
                        deepLinkUrl = $"/editor/{comment.ProjectId}?file={comment.FileId}&comment={comment.CommentId}",
                        // Username so NotificationService can resolve recipientId
                        recipientUsername = username
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(
                        json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(
                        $"{NotificationServiceUrl}/mention", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(
                            $"Failed to send mention notification " +
                            $"to @{username}: {response.StatusCode}");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Mention notification sent to @{username}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't fail the comment if notification fails
                Console.WriteLine(
                    $"Notification error: {ex.Message}");
            }
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

            // ADD — parse @mentions and call NotificationService
            // replaces the old Console.WriteLine stub
            var mentions = ParseMentions(dto.Content);
            if (mentions.Any())
            {
                await SendMentionNotificationsAsync(comment, mentions);
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

            // ADD — re-parse and send mention notifications on edit too
            var mentions = ParseMentions(dto.Content);
            if (mentions.Any())
            {
                await SendMentionNotificationsAsync(comment, mentions);
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