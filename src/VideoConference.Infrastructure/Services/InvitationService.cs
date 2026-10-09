using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Entities;
using VideoConference.Domain.Enums;

namespace VideoConference.Infrastructure.Services;

public class InvitationService : IInvitationService
{
    private readonly IAppDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<InvitationService> _logger;
    private readonly string _clientBaseUrl;

    public InvitationService(
        IAppDbContext dbContext,
        INotificationService notificationService,
        IConfiguration configuration,
        ILogger<InvitationService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
        _clientBaseUrl = configuration["Client:BaseUrl"] ?? "http://localhost:3000";
    }

    public async Task<ApiResponse<InvitationDto>> CreateInvitationAsync(Guid meetingId, Guid currentUserId, CreateInvitationRequest request)
    {
        var meeting = await _dbContext.Meetings
            .Include(m => m.HostUser)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<InvitationDto>.FailureResult("Meeting not found.");
        }

        if (meeting.HostUserId != currentUserId)
        {
            return ApiResponse<InvitationDto>.FailureResult("Only the meeting host can send invitations.");
        }

        var normalizedEmail = request.RecipientEmail.Trim().ToLowerInvariant();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

        var invitation = new MeetingInvitation
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            SenderUserId = currentUserId,
            RecipientEmail = normalizedEmail,
            Token = token,
            Status = InvitationStatus.Pending,
            ExpiresAt = meeting.ScheduledEndAt ?? DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Invitations.Add(invitation);
        await _dbContext.SaveChangesAsync();

        // If recipient user exists, send in-app notification
        var recipientUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (recipientUser != null)
        {
            await _notificationService.CreateNotificationAsync(
                recipientUser.Id,
                "MeetingInvitation",
                "New Meeting Invitation",
                $"{meeting.HostUser.DisplayName} invited you to join '{meeting.Title}'");
        }

        _logger.LogInformation("Invitation created for meeting {MeetingId} to {RecipientEmail}", meetingId, normalizedEmail);

        return ApiResponse<InvitationDto>.SuccessResult(MapToDto(invitation, meeting.Title), "Invitation created successfully");
    }

    public async Task<ApiResponse<List<InvitationDto>>> GetMeetingInvitationsAsync(Guid meetingId, Guid currentUserId)
    {
        var meeting = await _dbContext.Meetings.FindAsync(meetingId);
        if (meeting == null)
        {
            return ApiResponse<List<InvitationDto>>.FailureResult("Meeting not found.");
        }

        if (meeting.HostUserId != currentUserId)
        {
            return ApiResponse<List<InvitationDto>>.FailureResult("Only the host can view invitations.");
        }

        var invitations = await _dbContext.Invitations
            .Where(i => i.MeetingId == meetingId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => MapToDto(i, meeting.Title))
            .ToListAsync();

        return ApiResponse<List<InvitationDto>>.SuccessResult(invitations);
    }

    public async Task<ApiResponse<MeetingDto>> AcceptInvitationAsync(string token, Guid currentUserId)
    {
        var invitation = await _dbContext.Invitations
            .Include(i => i.Meeting)
                .ThenInclude(m => m.HostUser)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation == null)
        {
            return ApiResponse<MeetingDto>.FailureResult("Invalid invitation token.");
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _dbContext.SaveChangesAsync();
            return ApiResponse<MeetingDto>.FailureResult("This invitation has expired.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return ApiResponse<MeetingDto>.FailureResult($"Invitation is already {invitation.Status}.");
        }

        invitation.Status = InvitationStatus.Accepted;

        // Ensure user is added as allowed participant
        var existingParticipant = await _dbContext.Participants
            .FirstOrDefaultAsync(p => p.MeetingId == invitation.MeetingId && p.UserId == currentUserId);

        var currentUser = await _dbContext.Users.FindAsync(currentUserId);

        if (existingParticipant == null && currentUser != null)
        {
            _dbContext.Participants.Add(new MeetingParticipant
            {
                Id = Guid.NewGuid(),
                MeetingId = invitation.MeetingId,
                UserId = currentUserId,
                DisplayName = currentUser.DisplayName,
                Role = ParticipantRole.Participant,
                JoinedAt = DateTime.UtcNow,
                IsAllowed = true,
                IsBanned = false
            });
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Invitation {Token} accepted by user {UserId}", token, currentUserId);

        return ApiResponse<MeetingDto>.SuccessResult(new MeetingDto
        {
            Id = invitation.Meeting.Id,
            Title = invitation.Meeting.Title,
            Description = invitation.Meeting.Description,
            RoomName = invitation.Meeting.RoomName,
            HostUserId = invitation.Meeting.HostUserId,
            HostDisplayName = invitation.Meeting.HostUser.DisplayName,
            IsPasswordProtected = invitation.Meeting.IsPasswordProtected,
            IsWaitingRoomEnabled = invitation.Meeting.IsWaitingRoomEnabled,
            Status = invitation.Meeting.Status,
            ScheduledStartAt = invitation.Meeting.ScheduledStartAt,
            ScheduledEndAt = invitation.Meeting.ScheduledEndAt,
            CreatedAt = invitation.Meeting.CreatedAt
        }, "Invitation accepted");
    }

    private InvitationDto MapToDto(MeetingInvitation i, string meetingTitle)
    {
        return new InvitationDto
        {
            Id = i.Id,
            MeetingId = i.MeetingId,
            MeetingTitle = meetingTitle,
            RecipientEmail = i.RecipientEmail,
            Token = i.Token,
            InvitationUrl = $"{_clientBaseUrl}/join/invitation/{i.Token}",
            Status = i.Status,
            ExpiresAt = i.ExpiresAt,
            CreatedAt = i.CreatedAt
        };
    }
}
