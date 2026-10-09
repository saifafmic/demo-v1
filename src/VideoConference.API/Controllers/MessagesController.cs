using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Entities;

namespace VideoConference.API.Controllers;

[ApiController]
[Route("api/meetings/{meetingId}/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IAppDbContext _dbContext;

    public MessagesController(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieve stored chat messages for a meeting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MeetingMessageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(Guid meetingId)
    {
        var messages = await _dbContext.Messages
            .Where(m => m.MeetingId == meetingId)
            .OrderBy(m => m.SentAt)
            .Select(m => new MeetingMessageDto
            {
                Id = m.Id,
                MeetingId = m.MeetingId,
                UserId = m.UserId,
                SenderName = m.SenderName,
                Message = m.Message,
                SentAt = m.SentAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<MeetingMessageDto>>.SuccessResult(messages));
    }

    /// <summary>
    /// Save a chat message for archival/persistence.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MeetingMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveMessage(Guid meetingId, [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(ApiResponse<MeetingMessageDto>.FailureResult("Message content cannot be empty."));
        }

        var meeting = await _dbContext.Meetings.FindAsync(meetingId);
        if (meeting == null)
        {
            return NotFound(ApiResponse<MeetingMessageDto>.FailureResult("Meeting not found."));
        }

        var userId = GetCurrentUserId();
        var senderName = request.SenderName;

        if (userId.HasValue && string.IsNullOrWhiteSpace(senderName))
        {
            var user = await _dbContext.Users.FindAsync(userId.Value);
            senderName = user?.DisplayName;
        }

        senderName = !string.IsNullOrWhiteSpace(senderName) ? senderName : "Anonymous";

        var msg = new MeetingMessage
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            UserId = userId,
            SenderName = senderName,
            Message = request.Message.Trim(),
            SentAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(msg);
        await _dbContext.SaveChangesAsync();

        return Ok(ApiResponse<MeetingMessageDto>.SuccessResult(new MeetingMessageDto
        {
            Id = msg.Id,
            MeetingId = msg.MeetingId,
            UserId = msg.UserId,
            SenderName = msg.SenderName,
            Message = msg.Message,
            SentAt = msg.SentAt
        }));
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
