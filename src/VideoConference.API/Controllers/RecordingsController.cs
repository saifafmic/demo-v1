using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;

namespace VideoConference.API.Controllers;

[Authorize]
[ApiController]
[Route("api/meetings/{id}/recording")]
public class RecordingsController : ControllerBase
{
    private readonly IRecordingService _recordingService;

    public RecordingsController(IRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    /// <summary>
    /// Start recording the meeting (Host or Admin only).
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(ApiResponse<RecordingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartRecording(Guid id, [FromBody] StartRecordingRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _recordingService.StartRecordingAsync(id, userId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stop active recording for the meeting (Host or Admin only).
    /// </summary>
    [HttpPost("stop")]
    [ProducesResponseType(typeof(ApiResponse<RecordingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StopRecording(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _recordingService.StopRecordingAsync(id, userId.Value);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// List recordings metadata for this meeting.
    /// </summary>
    [HttpGet("/api/meetings/{id}/recordings")]
    [ProducesResponseType(typeof(ApiResponse<List<RecordingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecordings(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _recordingService.GetMeetingRecordingsAsync(id, userId.Value);
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
