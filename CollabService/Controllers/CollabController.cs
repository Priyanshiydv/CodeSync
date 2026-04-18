using CollabService.DTOs;
using CollabService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CollabService.Controllers
{
    /// <summary>
    /// Handles all HTTP requests for collaboration session management.
    /// Exposes /api/sessions endpoints.
    /// </summary>
    [ApiController]
    [Route("api/sessions")]
    public class CollabController : ControllerBase
    {
        private readonly ICollabService _collabService;

        public CollabController(ICollabService collabService)
        {
            _collabService = collabService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSession(
            [FromBody] CreateSessionDto dto)
        {
            try
            {
                var session = await _collabService
                    .CreateSession(GetUserId(), dto);
                return Ok(new { message = "Session created!", session });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetById(Guid sessionId)
        {
            var session = await _collabService.GetSessionById(sessionId);
            if (session == null) return NotFound();
            return Ok(session);
        }

        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            var sessions = await _collabService
                .GetSessionsByProject(projectId);
            return Ok(sessions);
        }

        [HttpGet("active/{fileId}")]
        public async Task<IActionResult> GetActiveSession(int fileId)
        {
            var session = await _collabService.GetActiveSession(fileId);
            if (session == null) return NotFound();
            return Ok(session);
        }

        [HttpGet("{sessionId}/participants")]
        public async Task<IActionResult> GetParticipants(Guid sessionId)
        {
            var participants = await _collabService
                .GetParticipants(sessionId);
            return Ok(participants);
        }

        [HttpPost("{sessionId}/join")]
        [Authorize]
        public async Task<IActionResult> JoinSession(
            Guid sessionId, [FromBody] JoinSessionDto dto)
        {
            try
            {
                var participant = await _collabService
                    .JoinSession(sessionId, dto);
                return Ok(new { message = "Joined session!", participant });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{sessionId}/leave")]
        [Authorize]
        public async Task<IActionResult> LeaveSession(
            Guid sessionId, [FromQuery] int userId)
        {
            try
            {
                await _collabService.LeaveSession(sessionId, userId);
                return Ok(new { message = "Left session!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{sessionId}/end")]
        [Authorize]
        public async Task<IActionResult> EndSession(Guid sessionId)
        {
            try
            {
                await _collabService.EndSession(sessionId);
                return Ok(new { message = "Session ended!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{sessionId}/kick")]
        [Authorize]
        public async Task<IActionResult> KickParticipant(
            Guid sessionId, [FromQuery] int userId)
        {
            try
            {
                await _collabService.KickParticipant(sessionId, userId);
                return Ok(new { message = "Participant kicked!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{sessionId}/cursor")]
        [Authorize]
        public async Task<IActionResult> UpdateCursor(
            Guid sessionId, [FromBody] UpdateCursorDto dto)
        {
            try
            {
                var participant = await _collabService
                    .UpdateCursor(sessionId, dto);
                return Ok(new { message = "Cursor updated!", participant });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{sessionId}/broadcast")]
        [Authorize]
        public async Task<IActionResult> BroadcastChange(
            Guid sessionId, [FromBody] BroadcastChangeDto dto)
        {
            try
            {
                await _collabService.BroadcastChange(sessionId, dto);
                return Ok(new { message = "Change broadcasted!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}