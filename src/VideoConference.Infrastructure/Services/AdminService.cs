using Microsoft.EntityFrameworkCore;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Enums;

namespace VideoConference.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IAppDbContext _dbContext;

    public AdminService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<UserDto>>> GetUsersAsync()
    {
        var users = await _dbContext.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                DisplayName = u.DisplayName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<UserDto>>.SuccessResult(users);
    }

    public async Task<ApiResponse<UserDto>> ToggleUserStatusAsync(Guid userId, bool isActive)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserDto>.FailureResult("User not found.");
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!isActive)
        {
            // Revoke refresh token immediately
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
        }

        await _dbContext.SaveChangesAsync();

        return ApiResponse<UserDto>.SuccessResult(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        }, $"User {(isActive ? "activated" : "deactivated")} successfully");
    }

    public async Task<ApiResponse<List<MeetingDto>>> GetAllMeetingsAsync()
    {
        var meetings = await _dbContext.Meetings
            .Include(m => m.HostUser)
            .Include(m => m.Links)
            .Include(m => m.Participants)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var dtos = meetings.Select(m => new MeetingDto
        {
            Id = m.Id,
            Title = m.Title,
            Description = m.Description,
            RoomName = m.RoomName,
            HostUserId = m.HostUserId,
            HostDisplayName = m.HostUser.DisplayName,
            IsPasswordProtected = m.IsPasswordProtected,
            IsWaitingRoomEnabled = m.IsWaitingRoomEnabled,
            Status = m.Status,
            ScheduledStartAt = m.ScheduledStartAt,
            ScheduledEndAt = m.ScheduledEndAt,
            CreatedAt = m.CreatedAt,
            StartedAt = m.StartedAt,
            EndedAt = m.EndedAt,
            PublicToken = m.Links.FirstOrDefault(l => l.IsActive)?.PublicToken,
            ActiveParticipantCount = m.Participants.Count(p => p.LeftAt == null && p.IsAllowed && !p.IsBanned)
        }).ToList();

        return ApiResponse<List<MeetingDto>>.SuccessResult(dtos);
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

    public async Task<ApiResponse<AdminStatsDto>> GetStatisticsAsync()
    {
        var totalUsers = await _dbContext.Users.CountAsync();
        var activeUsers = await _dbContext.Users.CountAsync(u => u.IsActive);
        var totalMeetings = await _dbContext.Meetings.CountAsync();
        var liveMeetings = await _dbContext.Meetings.CountAsync(m => m.Status == MeetingStatus.Live);
        var scheduledMeetings = await _dbContext.Meetings.CountAsync(m => m.Status == MeetingStatus.Scheduled);
        var totalRecordings = await _dbContext.Recordings.CountAsync();

        return ApiResponse<AdminStatsDto>.SuccessResult(new AdminStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalMeetings = totalMeetings,
            LiveMeetings = liveMeetings,
            ScheduledMeetings = scheduledMeetings,
            TotalRecordings = totalRecordings
        });
    }
}
