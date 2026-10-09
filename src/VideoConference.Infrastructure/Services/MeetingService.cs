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

public class MeetingService : IMeetingService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILiveKitService _liveKitService;
    private readonly IMeetingHistoryService _historyService;
    private readonly ILogger<MeetingService> _logger;
    private readonly string _clientBaseUrl;

    public MeetingService(
        IAppDbContext dbContext,
        ILiveKitService liveKitService,
        IMeetingHistoryService historyService,
        IConfiguration configuration,
        ILogger<MeetingService> logger)
    {
        _dbContext = dbContext;
        _liveKitService = liveKitService;
        _historyService = historyService;
        _logger = logger;
        _clientBaseUrl = configuration["Client:BaseUrl"] ?? "http://localhost:3000";
    }

    public async Task<ApiResponse<MeetingDto>> CreateMeetingAsync(Guid hostUserId, CreateMeetingRequest request)
    {
        var host = await _dbContext.Users.FindAsync(hostUserId);
        if (host == null)
        {
            return ApiResponse<MeetingDto>.FailureResult("Host user not found.");
        }

        var uniqueRoomName = $"room_{Guid.NewGuid():N}";
        var publicToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();

        string? passwordHash = null;
        var isPasswordProtected = !string.IsNullOrWhiteSpace(request.Password);
        if (isPasswordProtected)
        {
            passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            RoomName = uniqueRoomName,
            HostUserId = hostUserId,
            PasswordHash = passwordHash,
            IsPasswordProtected = isPasswordProtected,
            IsWaitingRoomEnabled = request.IsWaitingRoomEnabled,
            Status = request.ScheduledStartAt.HasValue && request.ScheduledStartAt > DateTime.UtcNow
                ? MeetingStatus.Scheduled
                : MeetingStatus.Live,
            ScheduledStartAt = request.ScheduledStartAt,
            ScheduledEndAt = request.ScheduledEndAt,
            CreatedAt = DateTime.UtcNow,
            StartedAt = request.ScheduledStartAt.HasValue && request.ScheduledStartAt > DateTime.UtcNow
                ? null
                : DateTime.UtcNow
        };

        var meetingLink = new MeetingLink
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            PublicToken = publicToken,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var hostParticipant = new MeetingParticipant
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            UserId = hostUserId,
            DisplayName = host.DisplayName,
            Role = ParticipantRole.Host,
            JoinedAt = DateTime.UtcNow,
            IsAllowed = true,
            IsBanned = false
        };

        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingLinks.Add(meetingLink);
        _dbContext.Participants.Add(hostParticipant);

        await _dbContext.SaveChangesAsync();

        await _historyService.LogAsync(
            meeting.Id,
            hostUserId,
            meeting.Status == MeetingStatus.Scheduled ? "Meeting scheduled" : "Meeting created",
            $"Title: {meeting.Title}, Room: {meeting.RoomName}");

        _logger.LogInformation("Meeting {MeetingId} created with room {RoomName} by user {UserId}", meeting.Id, meeting.RoomName, hostUserId);

        var dto = MapToDto(meeting, host.DisplayName, publicToken, 1);
        return ApiResponse<MeetingDto>.SuccessResult(dto, "Meeting created successfully");
    }

    public async Task<ApiResponse<List<MeetingDto>>> GetUserMeetingsAsync(Guid userId)
    {
        var meetings = await _dbContext.Meetings
            .Include(m => m.HostUser)
            .Include(m => m.Links)
            .Include(m => m.Participants)
            .Where(m => m.HostUserId == userId || m.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var dtos = meetings.Select(m => MapToDto(
            m,
            m.HostUser.DisplayName,
            m.Links.FirstOrDefault(l => l.IsActive)?.PublicToken,
            m.Participants.Count(p => p.LeftAt == null && p.IsAllowed && !p.IsBanned)
        )).ToList();

        return ApiResponse<List<MeetingDto>>.SuccessResult(dtos);
    }

    public async Task<ApiResponse<List<MeetingDto>>> GetUpcomingMeetingsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var meetings = await _dbContext.Meetings
            .Include(m => m.HostUser)
            .Include(m => m.Links)
            .Include(m => m.Participants)
            .Where(m => (m.HostUserId == userId || m.Participants.Any(p => p.UserId == userId)) &&
                        (m.Status == MeetingStatus.Scheduled || m.Status == MeetingStatus.Live) &&
                        (m.ScheduledEndAt == null || m.ScheduledEndAt >= now))
            .OrderBy(m => m.ScheduledStartAt ?? m.CreatedAt)
            .ToListAsync();

        var dtos = meetings.Select(m => MapToDto(
            m,
            m.HostUser.DisplayName,
            m.Links.FirstOrDefault(l => l.IsActive)?.PublicToken,
            m.Participants.Count(p => p.LeftAt == null && p.IsAllowed && !p.IsBanned)
        )).ToList();

        return ApiResponse<List<MeetingDto>>.SuccessResult(dtos);
    }

    public async Task<ApiResponse<MeetingDto>> GetMeetingByIdAsync(Guid meetingId, Guid currentUserId)
    {
        var meeting = await _dbContext.Meetings
            .Include(m => m.HostUser)
            .Include(m => m.Links)
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<MeetingDto>.FailureResult("Meeting not found.");
        }

        var publicToken = meeting.Links.FirstOrDefault(l => l.IsActive)?.PublicToken;
        var activeCount = meeting.Participants.Count(p => p.LeftAt == null && p.IsAllowed && !p.IsBanned);

        return ApiResponse<MeetingDto>.SuccessResult(MapToDto(meeting, meeting.HostUser.DisplayName, publicToken, activeCount));
    }

    public async Task<ApiResponse<MeetingDto>> GetMeetingByPublicTokenAsync(string publicToken)
    {
        var link = await _dbContext.MeetingLinks
            .Include(l => l.Meeting)
                .ThenInclude(m => m.HostUser)
            .Include(l => l.Meeting)
                .ThenInclude(m => m.Participants)
            .FirstOrDefaultAsync(l => l.PublicToken == publicToken && l.IsActive);

        if (link == null)
        {
            return ApiResponse<MeetingDto>.FailureResult("Invalid or expired meeting link.");
        }

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTime.UtcNow)
        {
            return ApiResponse<MeetingDto>.FailureResult("This meeting link has expired.");
        }

        var m = link.Meeting;
        var activeCount = m.Participants.Count(p => p.LeftAt == null && p.IsAllowed && !p.IsBanned);

        return ApiResponse<MeetingDto>.SuccessResult(MapToDto(m, m.HostUser.DisplayName, link.PublicToken, activeCount));
    }

    public async Task<ApiResponse<MeetingDto>> UpdateMeetingAsync(Guid meetingId, Guid currentUserId, UpdateMeetingRequest request)
    {
        var meeting = await _dbContext.Meetings
            .Include(m => m.HostUser)
            .Include(m => m.Links)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<MeetingDto>.FailureResult("Meeting not found.");
        }

        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var isAdmin = currentUser?.Role == UserRole.Admin;
        if (meeting.HostUserId != currentUserId && !isAdmin)
        {
            return ApiResponse<MeetingDto>.FailureResult("Only the host or admin can update meeting details.");
        }

        meeting.Title = request.Title.Trim();
        meeting.Description = request.Description?.Trim();
        meeting.IsWaitingRoomEnabled = request.IsWaitingRoomEnabled;
        meeting.ScheduledStartAt = request.ScheduledStartAt;
        meeting.ScheduledEndAt = request.ScheduledEndAt;

        if (request.Status.HasValue)
        {
            meeting.Status = request.Status.Value;
        }

        if (request.RemovePassword == true)
        {
            meeting.PasswordHash = null;
            meeting.IsPasswordProtected = false;
        }
        else if (!string.IsNullOrWhiteSpace(request.Password))
        {
            meeting.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            meeting.IsPasswordProtected = true;
        }

        await _dbContext.SaveChangesAsync();

        await _historyService.LogAsync(meetingId, currentUserId, "Meeting updated", $"Title: {meeting.Title}");
        _logger.LogInformation("Meeting {MeetingId} updated", meetingId);

        var publicToken = meeting.Links.FirstOrDefault(l => l.IsActive)?.PublicToken;
        return ApiResponse<MeetingDto>.SuccessResult(MapToDto(meeting, meeting.HostUser.DisplayName, publicToken, 0), "Meeting updated successfully");
    }

    public async Task<ApiResponse<bool>> DeleteMeetingAsync(Guid meetingId, Guid currentUserId)
    {
        var meeting = await _dbContext.Meetings.FindAsync(meetingId);
        if (meeting == null)
        {
            return ApiResponse<bool>.FailureResult("Meeting not found.");
        }

        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var isAdmin = currentUser?.Role == UserRole.Admin;
        if (meeting.HostUserId != currentUserId && !isAdmin)
        {
            return ApiResponse<bool>.FailureResult("Only the host or admin can delete this meeting.");
        }

        _dbContext.Meetings.Remove(meeting);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Meeting {MeetingId} deleted by user {UserId}", meetingId, currentUserId);
        return ApiResponse<bool>.SuccessResult(true, "Meeting deleted successfully");
    }

    public async Task<ApiResponse<JoinMeetingResponse>> JoinMeetingAsync(Guid meetingId, Guid? currentUserId, JoinMeetingRequest request)
    {
        var meeting = await _dbContext.Meetings
            .Include(m => m.HostUser)
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<JoinMeetingResponse>.FailureResult("Meeting not found.");
        }

        if (meeting.Status == MeetingStatus.Ended || meeting.Status == MeetingStatus.Cancelled)
        {
            return ApiResponse<JoinMeetingResponse>.FailureResult($"Cannot join meeting: status is {meeting.Status}.");
        }

        // Check if user is banned
        if (currentUserId.HasValue)
        {
            var isBanned = meeting.Participants.Any(p => p.UserId == currentUserId.Value && p.IsBanned);
            if (isBanned)
            {
                _logger.LogWarning("Banned user {UserId} attempted to join meeting {MeetingId}", currentUserId.Value, meetingId);
                return ApiResponse<JoinMeetingResponse>.FailureResult("You have been banned from this meeting.");
            }
        }

        // Validate password if meeting is password protected
        if (meeting.IsPasswordProtected)
        {
            // Hosts bypass password
            var isHost = currentUserId.HasValue && meeting.HostUserId == currentUserId.Value;
            if (!isHost)
            {
                if (string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(meeting.PasswordHash) ||
                    !BCrypt.Net.BCrypt.Verify(request.Password, meeting.PasswordHash))
                {
                    return ApiResponse<JoinMeetingResponse>.FailureResult("Invalid meeting password.");
                }
            }
        }

        // Determine user identity and role
        User? user = null;
        if (currentUserId.HasValue)
        {
            user = await _dbContext.Users.FindAsync(currentUserId.Value);
        }

        var isUserHost = user != null && meeting.HostUserId == user.Id;
        var existingParticipant = currentUserId.HasValue
            ? meeting.Participants.FirstOrDefault(p => p.UserId == currentUserId.Value)
            : null;

        var isCoHost = existingParticipant?.Role == ParticipantRole.CoHost;
        var role = isUserHost ? ParticipantRole.Host : (isCoHost ? ParticipantRole.CoHost : ParticipantRole.Participant);

        var displayName = !string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.DisplayName.Trim()
            : (user?.DisplayName ?? "Guest Participant");

        // Use an opaque identity for LiveKit
        var participantIdentity = $"p_{Guid.NewGuid():N}";

        if (existingParticipant == null)
        {
            var newParticipant = new MeetingParticipant
            {
                Id = Guid.NewGuid(),
                MeetingId = meeting.Id,
                UserId = user?.Id,
                DisplayName = displayName,
                Role = role,
                JoinedAt = DateTime.UtcNow,
                IsAllowed = true,
                IsBanned = false
            };
            _dbContext.Participants.Add(newParticipant);
        }
        else
        {
            existingParticipant.DisplayName = displayName;
            existingParticipant.JoinedAt = DateTime.UtcNow;
            existingParticipant.LeftAt = null;
            existingParticipant.IsAllowed = true;
        }

        if (meeting.Status == MeetingStatus.Scheduled || meeting.Status == MeetingStatus.Waiting)
        {
            meeting.Status = MeetingStatus.Live;
            meeting.StartedAt ??= DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        // Generate LiveKit token
        var liveKitToken = _liveKitService.GenerateToken(
            roomName: meeting.RoomName,
            participantIdentity: participantIdentity,
            participantName: displayName,
            isHost: isUserHost,
            isCoHost: isCoHost);

        await _historyService.LogAsync(meeting.Id, user?.Id, "Participant joined", $"Name: {displayName}, Role: {role}");
        _logger.LogInformation("Participant {Name} joined meeting {MeetingId} with LiveKit identity {Identity}", displayName, meetingId, participantIdentity);

        return ApiResponse<JoinMeetingResponse>.SuccessResult(new JoinMeetingResponse
        {
            ServerUrl = _liveKitService.GetServerUrl(),
            Token = liveKitToken,
            RoomName = meeting.RoomName,
            ParticipantIdentity = participantIdentity,
            ParticipantName = displayName,
            Role = role,
            IsHost = isUserHost,
            MeetingId = meeting.Id,
            Title = meeting.Title
        }, "Joined meeting successfully");
    }

    public async Task<ApiResponse<bool>> LeaveMeetingAsync(Guid meetingId, Guid currentUserId, string? participantIdentity = null)
    {
        var participant = await _dbContext.Participants
            .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == currentUserId && p.LeftAt == null);

        if (participant != null)
        {
            participant.LeftAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            await _historyService.LogAsync(meetingId, currentUserId, "Participant left", $"Name: {participant.DisplayName}");
        }

        if (!string.IsNullOrWhiteSpace(participantIdentity))
        {
            var meeting = await _dbContext.Meetings.FindAsync(meetingId);
            if (meeting != null)
            {
                await _liveKitService.RemoveParticipantAsync(meeting.RoomName, participantIdentity);
            }
        }

        return ApiResponse<bool>.SuccessResult(true, "Left meeting");
    }

    public async Task<ApiResponse<bool>> EndMeetingAsync(Guid meetingId, Guid currentUserId)
    {
        var meeting = await _dbContext.Meetings
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<bool>.FailureResult("Meeting not found.");
        }

        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var isAdmin = currentUser?.Role == UserRole.Admin;
        var isHost = meeting.HostUserId == currentUserId;
        var isCoHost = meeting.Participants.Any(p => p.UserId == currentUserId && p.Role == ParticipantRole.CoHost);

        if (!isHost && !isCoHost && !isAdmin)
        {
            return ApiResponse<bool>.FailureResult("Only host, co-host, or admin can end the meeting.");
        }

        meeting.Status = MeetingStatus.Ended;
        meeting.EndedAt = DateTime.UtcNow;

        // Mark all active participants as left
        foreach (var p in meeting.Participants.Where(p => p.LeftAt == null))
        {
            p.LeftAt = DateTime.UtcNow;
        }

        // Stop any active recordings
        var activeRecordings = await _dbContext.Recordings
            .Where(r => r.MeetingId == meetingId && r.Status == "Recording")
            .ToListAsync();

        foreach (var rec in activeRecordings)
        {
            rec.Status = "Completed";
            rec.Duration = DateTime.UtcNow - rec.CreatedAt;
        }

        await _dbContext.SaveChangesAsync();

        await _liveKitService.DeleteRoomAsync(meeting.RoomName);
        await _historyService.LogAsync(meetingId, currentUserId, "Meeting ended");

        _logger.LogInformation("Meeting {MeetingId} ended by user {UserId}", meetingId, currentUserId);
        return ApiResponse<bool>.SuccessResult(true, "Meeting ended successfully");
    }

    private MeetingDto MapToDto(Meeting m, string hostName, string? publicToken, int activeCount)
    {
        return new MeetingDto
        {
            Id = m.Id,
            Title = m.Title,
            Description = m.Description,
            RoomName = m.RoomName,
            HostUserId = m.HostUserId,
            HostDisplayName = hostName,
            IsPasswordProtected = m.IsPasswordProtected,
            IsWaitingRoomEnabled = m.IsWaitingRoomEnabled,
            Status = m.Status,
            ScheduledStartAt = m.ScheduledStartAt,
            ScheduledEndAt = m.ScheduledEndAt,
            CreatedAt = m.CreatedAt,
            StartedAt = m.StartedAt,
            EndedAt = m.EndedAt,
            PublicToken = publicToken,
            MeetingLink = publicToken != null ? $"{_clientBaseUrl}/join/{publicToken}" : null,
            ActiveParticipantCount = activeCount
        };
    }
}
