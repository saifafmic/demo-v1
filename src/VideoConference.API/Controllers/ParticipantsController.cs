using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;

namespace VideoConference.API.Controllers;

[Authorize]
[ApiController]
[Route("api/meetings/{meetingId}/[controller]")]
public class ParticipantsController : ControllerBase
{
    private readonly IParticipantService _participantService;

    public ParticipantsController(IParticipantService participantService)
    {
        _participantService = participantService;
    }

    /// <summary>
    /// Get the list of participants for a meeting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ParticipantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParticipants(Guid meetingId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _participantService.GetMeetingParticipantsAsync(meetingId, userId.Value);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Remove a participant from the meeting (Host, CoHost, or Admin only).
    /// </summary>
    [HttpPost("{participantId}/remove")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveParticipant(Guid meetingId, Guid participantId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _participantService.RemoveParticipantAsync(meetingId, participantId, userId.Value);
        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Ban a participant from re-entering the meeting (Host, CoHost, or Admin only).
    /// </summary>
    [HttpPost("{participantId}/ban")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BanParticipant(Guid meetingId, Guid participantId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _participantService.BanParticipantAsync(meetingId, participantId, userId.Value);
        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Promote a participant to Co-Host role (Host, CoHost, or Admin only).
    /// </summary>
    [HttpPost("{participantId}/promote")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PromoteParticipant(Guid meetingId, Guid participantId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _participantService.PromoteParticipantAsync(meetingId, participantId, userId.Value);
        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Demote a Co-Host back to regular Participant role (Host or Admin only).
    /// </summary>
    [HttpPost("{participantId}/demote")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DemoteParticipant(Guid meetingId, Guid participantId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _participantService.DemoteParticipantAsync(meetingId, participantId, userId.Value);
        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status403Forbidden, result);
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
