using Microsoft.EntityFrameworkCore;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Enums;

namespace VideoConference.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly IAppDbContext _dbContext;

    public SearchService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<SearchResultDto>> SearchAsync(string query, Guid currentUserId)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ApiResponse<SearchResultDto>.SuccessResult(new SearchResultDto());
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        var meetings = await _dbContext.Meetings
            .Include(m => m.HostUser)
            .Include(m => m.Links)
            .Where(m => m.Title.ToLower().Contains(normalizedQuery) ||
                        (m.Description != null && m.Description.ToLower().Contains(normalizedQuery)))
            .OrderByDescending(m => m.CreatedAt)
            .Take(20)
            .Select(m => new MeetingDto
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
                PublicToken = m.Links.FirstOrDefault(l => l.IsActive) != null
                    ? m.Links.FirstOrDefault(l => l.IsActive)!.PublicToken
                    : null
            })
            .ToListAsync();

        var currentUser = await _dbContext.Users.FindAsync(currentUserId);
        var users = new List<UserDto>();

        if (currentUser?.Role == UserRole.Admin)
        {
            users = await _dbContext.Users
                .Where(u => u.Email.ToLower().Contains(normalizedQuery) ||
                            u.DisplayName.ToLower().Contains(normalizedQuery) ||
                            u.FirstName.ToLower().Contains(normalizedQuery) ||
                            u.LastName.ToLower().Contains(normalizedQuery))
                .OrderBy(u => u.DisplayName)
                .Take(20)
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
        }

        return ApiResponse<SearchResultDto>.SuccessResult(new SearchResultDto
        {
            Meetings = meetings,
            Users = users
        });
    }
}
