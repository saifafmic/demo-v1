using Microsoft.EntityFrameworkCore;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Entities;

namespace VideoConference.Infrastructure.Services;

public class MeetingHistoryService : IMeetingHistoryService
{
    private readonly IAppDbContext _dbContext;

    public MeetingHistoryService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(Guid meetingId, Guid? userId, string action, string? metadata = null)
    {
        var record = new MeetingHistory
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            UserId = userId,
            Action = action,
            Timestamp = DateTime.UtcNow,
            Metadata = metadata
        };

        _dbContext.MeetingHistories.Add(record);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<ApiResponse<List<MeetingHistoryDto>>> GetMeetingHistoryAsync(Guid meetingId, Guid currentUserId)
    {
        var meeting = await _dbContext.Meetings
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<List<MeetingHistoryDto>>.FailureResult("Meeting not found.");
        }

        var isHost = meeting.HostUserId == currentUserId;
        var isParticipant = meeting.Participants.Any(p => p.UserId == currentUserId);

        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var isAdmin = currentUser?.Role == Domain.Enums.UserRole.Admin;

        if (!isHost && !isParticipant && !isAdmin)
        {
            return ApiResponse<List<MeetingHistoryDto>>.FailureResult("You do not have permission to view this meeting history.");
        }

        var history = await _dbContext.MeetingHistories
            .Include(h => h.Meeting)
            .Where(h => h.MeetingId == meetingId)
            .OrderByDescending(h => h.Timestamp)
            .Select(h => new MeetingHistoryDto
            {
                Id = h.Id,
                MeetingId = h.MeetingId,
                MeetingTitle = h.Meeting.Title,
                UserId = h.UserId,
                Action = h.Action,
                Timestamp = h.Timestamp,
                Metadata = h.Metadata
            })
            .ToListAsync();

        return ApiResponse<List<MeetingHistoryDto>>.SuccessResult(history);
    }

    public async Task<ApiResponse<List<MeetingHistoryDto>>> GetUserHistoryAsync(Guid userId)
    {
        var history = await _dbContext.MeetingHistories
            .Include(h => h.Meeting)
            .Where(h => h.UserId == userId || h.Meeting.HostUserId == userId)
            .OrderByDescending(h => h.Timestamp)
            .Take(100)
            .Select(h => new MeetingHistoryDto
            {
                Id = h.Id,
                MeetingId = h.MeetingId,
                MeetingTitle = h.Meeting.Title,
                UserId = h.UserId,
                Action = h.Action,
                Timestamp = h.Timestamp,
                Metadata = h.Metadata
            })
            .ToListAsync();

        return ApiResponse<List<MeetingHistoryDto>>.SuccessResult(history);
    }

    public async Task<ApiResponse<List<MeetingHistoryDto>>> GetAllHistoryAsync()
    {
        var history = await _dbContext.MeetingHistories
            .Include(h => h.Meeting)
            .OrderByDescending(h => h.Timestamp)
            .Take(200)
            .Select(h => new MeetingHistoryDto
            {
                Id = h.Id,
                MeetingId = h.MeetingId,
                MeetingTitle = h.Meeting.Title,
                UserId = h.UserId,
                Action = h.Action,
                Timestamp = h.Timestamp,
                Metadata = h.Metadata
            })
            .ToListAsync();

        return ApiResponse<List<MeetingHistoryDto>>.SuccessResult(history);
    }
}
