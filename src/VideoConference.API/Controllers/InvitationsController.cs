using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;

namespace VideoConference.API.Controllers;

[Authorize]
[ApiController]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitationService;

    public InvitationsController(IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    /// <summary>
    /// Send an invitation to a meeting by recipient email.
    /// </summary>
    [HttpPost("api/meetings/{meetingId}/invitations")]
    [ProducesResponseType(typeof(ApiResponse<InvitationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateInvitation(Guid meetingId, [FromBody] CreateInvitationRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _invitationService.CreateInvitationAsync(meetingId, userId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// List invitations sent for a meeting.
    /// </summary>
    [HttpGet("api/meetings/{meetingId}/invitations")]
    [ProducesResponseType(typeof(ApiResponse<List<InvitationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvitations(Guid meetingId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _invitationService.GetMeetingInvitationsAsync(meetingId, userId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Accept a meeting invitation using the invitation token.
    /// </summary>
    [HttpPost("api/invitations/{token}/accept")]
    [ProducesResponseType(typeof(ApiResponse<MeetingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptInvitation(string token)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _invitationService.AcceptInvitationAsync(token, userId.Value);
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
