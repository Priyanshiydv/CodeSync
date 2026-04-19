using CommentService.DTOs;
using CommentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommentService.Controllers
{
    /// <summary>
    /// Handles all HTTP requests for code review comments.
    /// Exposes /api/comments endpoints.
    /// </summary>
    [ApiController]
    [Route("api/comments")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(
            [FromBody] AddCommentDto dto)
        {
            try
            {
                var comment = await _commentService
                    .AddComment(GetUserId(), dto);
                return Ok(new { message = "Comment added!", comment });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("file/{fileId}")]
        public async Task<IActionResult> GetByFile(int fileId)
        {
            var comments = await _commentService.GetByFile(fileId);
            return Ok(comments);
        }

        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            var comments = await _commentService.GetByProject(projectId);
            return Ok(comments);
        }

        [HttpGet("{commentId}")]
        public async Task<IActionResult> GetById(int commentId)
        {
            var comment = await _commentService
                .GetCommentById(commentId);
            if (comment == null) return NotFound();
            return Ok(comment);
        }

        [HttpGet("{commentId}/replies")]
        public async Task<IActionResult> GetReplies(int commentId)
        {
            var replies = await _commentService.GetReplies(commentId);
            return Ok(replies);
        }

        [HttpGet("file/{fileId}/line/{lineNumber}")]
        public async Task<IActionResult> GetByLine(
            int fileId, int lineNumber)
        {
            var comments = await _commentService
                .GetByLine(fileId, lineNumber);
            return Ok(comments);
        }

        [HttpGet("file/{fileId}/count")]
        public async Task<IActionResult> GetCount(int fileId)
        {
            var count = await _commentService.GetCommentCount(fileId);
            return Ok(new { fileId, count });
        }

        [HttpPut("{commentId}")]
        [Authorize]
        public async Task<IActionResult> Update(
            int commentId, [FromBody] UpdateCommentDto dto)
        {
            try
            {
                var comment = await _commentService
                    .UpdateComment(commentId, dto);
                return Ok(new { message = "Comment updated!", comment });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{commentId}/resolve")]
        [Authorize]
        public async Task<IActionResult> Resolve(int commentId)
        {
            try
            {
                var comment = await _commentService
                    .ResolveComment(commentId);
                return Ok(new { message = "Comment resolved!", comment });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{commentId}/unresolve")]
        [Authorize]
        public async Task<IActionResult> Unresolve(int commentId)
        {
            try
            {
                var comment = await _commentService
                    .UnresolveComment(commentId);
                return Ok(new { message = "Comment unresolved!", comment });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{commentId}")]
        [Authorize]
        public async Task<IActionResult> Delete(int commentId)
        {
            try
            {
                await _commentService.DeleteComment(commentId);
                return Ok(new { message = "Comment deleted!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}