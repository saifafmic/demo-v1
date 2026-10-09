using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;

namespace VideoConference.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;
    private readonly IMeetingHistoryService _historyService;

    public MeetingsController(
        IMeetingService meetingService,
        IMeetingHistoryService historyService)
    {
        _meetingService = meetingService;
        _historyService = historyService;
    }

    /// <summary>
    /// Create and schedule a new meeting. Generates a unique LiveKit room and public link.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MeetingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<MeetingDto>.FailureResult("Unauthorized"));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<MeetingDto>.FailureResult("Validation failed", ModelState));
        }

        var result = await _meetingService.CreateMeetingAsync(userId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all meetings hosted or joined by the current user.
    /// </summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MeetingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserMeetings()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<List<MeetingDto>>.FailureResult("Unauthorized"));
        }

        var result = await _meetingService.GetUserMeetingsAsync(userId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Get upcoming and active meetings for current user.
    /// </summary>
    [Authorize]
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(ApiResponse<List<MeetingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingMeetings()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<List<MeetingDto>>.FailureResult("Unauthorized"));
        }

        var result = await _meetingService.GetUpcomingMeetingsAsync(userId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Get meeting details by ID.
    /// </summary>
    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MeetingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMeetingById(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<MeetingDto>.FailureResult("Unauthorized"));
        }

        var result = await _meetingService.GetMeetingByIdAsync(id, userId.Value);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Look up meeting information by its public meeting link token (for join landing page).
    /// </summary>
    [HttpGet("token/{publicToken}")]
    [ProducesResponseType(typeof(ApiResponse<MeetingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMeetingByPublicToken(string publicToken)
    {
        var result = await _meetingService.GetMeetingByPublicTokenAsync(publicToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update meeting metadata (Host or Admin only).
    /// </summary>
    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MeetingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMeeting(Guid id, [FromBody] UpdateMeetingRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<MeetingDto>.FailureResult("Unauthorized"));
        }

        var result = await _meetingService.UpdateMeetingAsync(id, userId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a meeting (Host or Admin only).
    /// </summary>
    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteMeeting(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<bool>.FailureResult("Unauthorized"));
        }

        var result = await _meetingService.DeleteMeetingAsync(id, userId.Value);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Join a meeting. Validates status, password, and bans, then generates and returns the LiveKit participant JWT.
    /// </summary>
    [HttpPost("{id}/join")]
    [ProducesResponseType(typeof(ApiResponse<JoinMeetingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinMeeting(Guid id, [FromBody] JoinMeetingRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _meetingService.JoinMeetingAsync(id, userId, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Leave a meeting and update participant presence status.
    /// </summary>
    [Authorize]
    [HttpPost("{id}/leave")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LeaveMeeting(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<bool>.FailureResult("Unauthorized"));
        }

        var result = await _meetingService.LeaveMeetingAsync(id, userId.Value);
        return Ok(result);
    }

    /// <summary>
    /// End a meeting for all participants (Host, CoHost, or Admin only).
    /// </summary>
    [Authorize]
    [HttpPost("{id}/end")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EndMeeting(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<bool>.FailureResult("Unauthorized"));
        }

        var result = await _meetingService.EndMeetingAsync(id, userId.Value);
        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get meeting history logs for current user across meetings.
    /// </summary>
    [Authorize]
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<List<MeetingHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserHistory()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<List<MeetingHistoryDto>>.FailureResult("Unauthorized"));
        }

        var result = await _historyService.GetUserHistoryAsync(userId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Get audit history logs for a specific meeting.
    /// </summary>
    [Authorize]
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<MeetingHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMeetingHistory(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<List<MeetingHistoryDto>>.FailureResult("Unauthorized"));
        }

        var result = await _historyService.GetMeetingHistoryAsync(id, userId.Value);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(claim, out var guid))
        {
            return guid;
        }

        return null;
    }
}
