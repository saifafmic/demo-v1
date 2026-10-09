using VideoConference.Application.Common;
using VideoConference.Application.DTOs;

namespace VideoConference.Application.Interfaces;

public interface IMeetingHistoryService
{
    Task LogAsync(Guid meetingId, Guid? userId, string action, string? metadata = null);
    Task<ApiResponse<List<MeetingHistoryDto>>> GetMeetingHistoryAsync(Guid meetingId, Guid currentUserId);
    Task<ApiResponse<List<MeetingHistoryDto>>> GetUserHistoryAsync(Guid userId);
    Task<ApiResponse<List<MeetingHistoryDto>>> GetAllHistoryAsync();
}
