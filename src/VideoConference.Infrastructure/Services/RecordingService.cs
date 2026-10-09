using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Entities;

namespace VideoConference.Infrastructure.Services;

public class RecordingService : IRecordingService
{
    private readonly IAppDbContext _dbContext;
    private readonly IMeetingHistoryService _historyService;
    private readonly ILogger<RecordingService> _logger;

    public RecordingService(
        IAppDbContext dbContext,
        IMeetingHistoryService historyService,
        ILogger<RecordingService> logger)
    {
        _dbContext = dbContext;
        _historyService = historyService;
        _logger = logger;
    }

    public async Task<ApiResponse<RecordingDto>> StartRecordingAsync(Guid meetingId, Guid currentUserId, StartRecordingRequest request)
    {
        var meeting = await _dbContext.Meetings.FindAsync(meetingId);
        if (meeting == null)
        {
            return ApiResponse<RecordingDto>.FailureResult("Meeting not found.");
        }

        var isHost = meeting.HostUserId == currentUserId;
        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var isAdmin = currentUser?.Role == Domain.Enums.UserRole.Admin;

        if (!isHost && !isAdmin)
        {
            return ApiResponse<RecordingDto>.FailureResult("Only the host or admin can start recording.");
        }

        var activeRecording = await _dbContext.Recordings
            .FirstOrDefaultAsync(r => r.MeetingId == meetingId && r.Status == "Recording");

        if (activeRecording != null)
        {
            return ApiResponse<RecordingDto>.FailureResult("A recording is already active for this meeting.");
        }

        var fileName = request.OutputFileName ?? $"recording_{meeting.RoomName}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp4";
        var storagePath = Path.Combine("storage", "recordings", fileName);

        var recording = new MeetingRecording
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            FileName = fileName,
            StoragePath = storagePath,
            Status = "Recording",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Recordings.Add(recording);
        await _dbContext.SaveChangesAsync();

        await _historyService.LogAsync(meetingId, currentUserId, "Recording started", $"FileName: {fileName}");
        _logger.LogInformation("Recording started for meeting {MeetingId} by user {UserId}", meetingId, currentUserId);

        return ApiResponse<RecordingDto>.SuccessResult(MapToDto(recording), "Recording started");
    }

    public async Task<ApiResponse<RecordingDto>> StopRecordingAsync(Guid meetingId, Guid currentUserId)
    {
        var meeting = await _dbContext.Meetings.FindAsync(meetingId);
        if (meeting == null)
        {
            return ApiResponse<RecordingDto>.FailureResult("Meeting not found.");
        }

        var isHost = meeting.HostUserId == currentUserId;
        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var isAdmin = currentUser?.Role == Domain.Enums.UserRole.Admin;

        if (!isHost && !isAdmin)
        {
            return ApiResponse<RecordingDto>.FailureResult("Only the host or admin can stop recording.");
        }

        var activeRecording = await _dbContext.Recordings
            .FirstOrDefaultAsync(r => r.MeetingId == meetingId && r.Status == "Recording");

        if (activeRecording == null)
        {
            return ApiResponse<RecordingDto>.FailureResult("No active recording found for this meeting.");
        }

        activeRecording.Status = "Completed";
        activeRecording.Duration = DateTime.UtcNow - activeRecording.CreatedAt;

        await _dbContext.SaveChangesAsync();

        await _historyService.LogAsync(meetingId, currentUserId, "Recording stopped", $"Duration: {activeRecording.Duration}");
        _logger.LogInformation("Recording stopped for meeting {MeetingId}", meetingId);

        return ApiResponse<RecordingDto>.SuccessResult(MapToDto(activeRecording), "Recording stopped successfully");
    }

    public async Task<ApiResponse<List<RecordingDto>>> GetMeetingRecordingsAsync(Guid meetingId, Guid currentUserId)
    {
        var meeting = await _dbContext.Meetings
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<List<RecordingDto>>.FailureResult("Meeting not found.");
        }

        var isHost = meeting.HostUserId == currentUserId;
        var isParticipant = meeting.Participants.Any(p => p.UserId == currentUserId);
        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var isAdmin = currentUser?.Role == Domain.Enums.UserRole.Admin;

        if (!isHost && !isParticipant && !isAdmin)
        {
            return ApiResponse<List<RecordingDto>>.FailureResult("You do not have access to recordings for this meeting.");
        }

        var recordings = await _dbContext.Recordings
            .Where(r => r.MeetingId == meetingId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();

        return ApiResponse<List<RecordingDto>>.SuccessResult(recordings);
    }

    private static RecordingDto MapToDto(MeetingRecording r)
    {
        return new RecordingDto
        {
            Id = r.Id,
            MeetingId = r.MeetingId,
            FileName = r.FileName,
            StoragePath = r.StoragePath,
            Duration = r.Duration,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        };
    }
}
