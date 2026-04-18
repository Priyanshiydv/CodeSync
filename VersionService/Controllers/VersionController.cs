using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VersionService.DTOs;
using VersionService.Interfaces;

namespace VersionService.Controllers
{
    /// <summary>
    /// Handles all HTTP requests for version/snapshot management.
    /// Exposes /api/versions endpoints.
    /// </summary>
    [ApiController]
    [Route("api/versions")]
    public class VersionController : ControllerBase
    {
        private readonly IVersionService _versionService;

        public VersionController(IVersionService versionService)
        {
            _versionService = versionService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSnapshot(
            [FromBody] CreateSnapshotDto dto)
        {
            try
            {
                var snapshot = await _versionService
                    .CreateSnapshot(GetUserId(), dto);
                return Ok(new { message = "Snapshot created!", snapshot });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{snapshotId}")]
        public async Task<IActionResult> GetById(int snapshotId)
        {
            var snapshot = await _versionService
                .GetSnapshotById(snapshotId);
            if (snapshot == null) return NotFound();
            return Ok(snapshot);
        }

        [HttpGet("file/{fileId}")]
        public async Task<IActionResult> GetByFile(int fileId)
        {
            var snapshots = await _versionService
                .GetSnapshotsByFile(fileId);
            return Ok(snapshots);
        }

        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            var snapshots = await _versionService
                .GetSnapshotsByProject(projectId);
            return Ok(snapshots);
        }

        [HttpGet("branch/{branch}")]
        public async Task<IActionResult> GetByBranch(string branch)
        {
            var snapshots = await _versionService
                .GetSnapshotsByBranch(branch);
            return Ok(snapshots);
        }

        [HttpGet("history/{fileId}")]
        public async Task<IActionResult> GetFileHistory(int fileId)
        {
            var history = await _versionService.GetFileHistory(fileId);
            return Ok(history);
        }

        [HttpGet("latest/{fileId}")]
        public async Task<IActionResult> GetLatest(int fileId)
        {
            var snapshot = await _versionService
                .GetLatestSnapshot(fileId);
            if (snapshot == null) return NotFound();
            return Ok(snapshot);
        }

        [HttpPost("{snapshotId}/restore")]
        [Authorize]
        public async Task<IActionResult> Restore(int snapshotId)
        {
            try
            {
                var snapshot = await _versionService
                    .RestoreSnapshot(snapshotId, GetUserId());
                return Ok(new { message = "Snapshot restored!", snapshot });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("diff/{snapshotId1}/{snapshotId2}")]
        public async Task<IActionResult> Diff(
            int snapshotId1, int snapshotId2)
        {
            try
            {
                var diff = await _versionService
                    .DiffSnapshots(snapshotId1, snapshotId2);
                return Ok(diff);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("createBranch")]
        [Authorize]
        public async Task<IActionResult> CreateBranch(
            [FromBody] CreateBranchDto dto)
        {
            try
            {
                var snapshot = await _versionService
                    .CreateBranch(GetUserId(), dto);
                return Ok(new { message = "Branch created!", snapshot });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("tag")]
        [Authorize]
        public async Task<IActionResult> TagSnapshot(
            [FromBody] TagSnapshotDto dto)
        {
            try
            {
                var snapshot = await _versionService.TagSnapshot(dto);
                return Ok(new { message = "Snapshot tagged!", snapshot });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}