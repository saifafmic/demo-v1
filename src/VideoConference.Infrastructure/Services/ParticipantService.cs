using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Enums;

namespace VideoConference.Infrastructure.Services;

public class ParticipantService : IParticipantService
{
    private readonly IAppDbContext _dbContext;
    private readonly IMeetingHistoryService _historyService;
    private readonly ILiveKitService _liveKitService;
    private readonly ILogger<ParticipantService> _logger;

    public ParticipantService(
        IAppDbContext dbContext,
        IMeetingHistoryService historyService,
        ILiveKitService liveKitService,
        ILogger<ParticipantService> logger)
    {
        _dbContext = dbContext;
        _historyService = historyService;
        _liveKitService = liveKitService;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ParticipantDto>>> GetMeetingParticipantsAsync(Guid meetingId, Guid currentUserId)
    {
        var meeting = await _dbContext.Meetings
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<List<ParticipantDto>>.FailureResult("Meeting not found.");
        }

        var isHost = meeting.HostUserId == currentUserId;
        var isParticipant = meeting.Participants.Any(p => p.UserId == currentUserId);
        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var isAdmin = currentUser?.Role == UserRole.Admin;

        if (!isHost && !isParticipant && !isAdmin)
        {
            return ApiResponse<List<ParticipantDto>>.FailureResult("You do not have access to this meeting's participant list.");
        }

        var participants = await _dbContext.Participants
            .Where(p => p.MeetingId == meetingId)
            .OrderBy(p => p.Role == ParticipantRole.Host ? 0 : p.Role == ParticipantRole.CoHost ? 1 : 2)
            .ThenBy(p => p.JoinedAt)
            .Select(p => new ParticipantDto
            {
                Id = p.Id,
                MeetingId = p.MeetingId,
                UserId = p.UserId,
                DisplayName = p.DisplayName,
                Role = p.Role,
                JoinedAt = p.JoinedAt,
                LeftAt = p.LeftAt,
                IsAllowed = p.IsAllowed,
                IsBanned = p.IsBanned
            })
            .ToListAsync();

        return ApiResponse<List<ParticipantDto>>.SuccessResult(participants);
    }

    public async Task<ApiResponse<bool>> RemoveParticipantAsync(Guid meetingId, Guid participantId, Guid currentUserId)
    {
        var authCheck = await AuthorizeModeratorAsync(meetingId, currentUserId);
        if (!authCheck.Success)
        {
            return ApiResponse<bool>.FailureResult(authCheck.Message);
        }

        var participant = await _dbContext.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.MeetingId == meetingId);
        if (participant == null)
        {
            return ApiResponse<bool>.FailureResult("Participant not found.");
        }

        if (participant.Role == ParticipantRole.Host)
        {
            return ApiResponse<bool>.FailureResult("Cannot remove the meeting host.");
        }

        participant.LeftAt = DateTime.UtcNow;
        participant.IsAllowed = false;

        await _dbContext.SaveChangesAsync();

        await _historyService.LogAsync(meetingId, currentUserId, "Participant removed", $"Removed: {participant.DisplayName}");
        _logger.LogInformation("Participant {ParticipantId} removed from meeting {MeetingId} by user {UserId}", participantId, meetingId, currentUserId);

        return ApiResponse<bool>.SuccessResult(true, "Participant removed successfully");
    }

    public async Task<ApiResponse<bool>> BanParticipantAsync(Guid meetingId, Guid participantId, Guid currentUserId)
    {
        var authCheck = await AuthorizeModeratorAsync(meetingId, currentUserId);
        if (!authCheck.Success)
        {
            return ApiResponse<bool>.FailureResult(authCheck.Message);
        }

        var participant = await _dbContext.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.MeetingId == meetingId);
        if (participant == null)
        {
            return ApiResponse<bool>.FailureResult("Participant not found.");
        }

        if (participant.Role == ParticipantRole.Host)
        {
            return ApiResponse<bool>.FailureResult("Cannot ban the meeting host.");
        }

        participant.IsBanned = true;
        participant.IsAllowed = false;
        participant.LeftAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _historyService.LogAsync(meetingId, currentUserId, "Participant banned", $"Banned: {participant.DisplayName}");
        _logger.LogInformation("Participant {ParticipantId} banned from meeting {MeetingId}", participantId, meetingId);

        return ApiResponse<bool>.SuccessResult(true, "Participant banned successfully");
    }

    public async Task<ApiResponse<bool>> PromoteParticipantAsync(Guid meetingId, Guid participantId, Guid currentUserId)
    {
        var authCheck = await AuthorizeModeratorAsync(meetingId, currentUserId);
        if (!authCheck.Success)
        {
            return ApiResponse<bool>.FailureResult(authCheck.Message);
        }

        var participant = await _dbContext.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.MeetingId == meetingId);
        if (participant == null)
        {
            return ApiResponse<bool>.FailureResult("Participant not found.");
        }

        participant.Role = ParticipantRole.CoHost;
        await _dbContext.SaveChangesAsync();

        await _historyService.LogAsync(meetingId, currentUserId, "Participant promoted", $"Promoted: {participant.DisplayName} to CoHost");
        _logger.LogInformation("Participant {ParticipantId} promoted to CoHost in meeting {MeetingId}", participantId, meetingId);

        return ApiResponse<bool>.SuccessResult(true, "Participant promoted to Co-Host");
    }

    public async Task<ApiResponse<bool>> DemoteParticipantAsync(Guid meetingId, Guid participantId, Guid currentUserId)
    {
        var authCheck = await AuthorizeModeratorAsync(meetingId, currentUserId);
        if (!authCheck.Success)
        {
            return ApiResponse<bool>.FailureResult(authCheck.Message);
        }

        var participant = await _dbContext.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.MeetingId == meetingId);
        if (participant == null)
        {
            return ApiResponse<bool>.FailureResult("Participant not found.");
        }

        if (participant.Role == ParticipantRole.Host)
        {
            return ApiResponse<bool>.FailureResult("Cannot demote the primary host.");
        }

        participant.Role = ParticipantRole.Participant;
        await _dbContext.SaveChangesAsync();

        await _historyService.LogAsync(meetingId, currentUserId, "Participant demoted", $"Demoted: {participant.DisplayName} to Participant");
        _logger.LogInformation("Participant {ParticipantId} demoted to Participant in meeting {MeetingId}", participantId, meetingId);

        return ApiResponse<bool>.SuccessResult(true, "Participant demoted to regular participant");
    }

    private async Task<(bool Success, string Message)> AuthorizeModeratorAsync(Guid meetingId, Guid currentUserId)
    {
        var meeting = await _dbContext.Meetings.FindAsync(meetingId);
        if (meeting == null)
        {
            return (false, "Meeting not found.");
        }

        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var isAdmin = currentUser?.Role == UserRole.Admin;
        var isHost = meeting.HostUserId == currentUserId;

        var coHost = await _dbContext.Participants
            .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == currentUserId && p.Role == ParticipantRole.CoHost);

        if (!isHost && coHost == null && !isAdmin)
        {
            return (false, "Only host, co-host, or admin can moderate participants.");
        }

        return (true, string.Empty);
    }
}
