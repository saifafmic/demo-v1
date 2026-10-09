using VideoConference.Application.Common;
using VideoConference.Application.DTOs;

namespace VideoConference.Application.Interfaces;

public interface IAdminService
{
    Task<ApiResponse<List<UserDto>>> GetUsersAsync();
    Task<ApiResponse<UserDto>> ToggleUserStatusAsync(Guid userId, bool isActive);
    Task<ApiResponse<List<MeetingDto>>> GetAllMeetingsAsync();
    Task<ApiResponse<List<MeetingHistoryDto>>> GetAllHistoryAsync();
    Task<ApiResponse<AdminStatsDto>> GetStatisticsAsync();
}
