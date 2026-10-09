using VideoConference.Application.Common;
using VideoConference.Application.DTOs;

namespace VideoConference.Application.Interfaces;

public interface IMeetingService
{
    Task<ApiResponse<MeetingDto>> CreateMeetingAsync(Guid hostUserId, CreateMeetingRequest request);
    Task<ApiResponse<List<MeetingDto>>> GetUserMeetingsAsync(Guid userId);
    Task<ApiResponse<List<MeetingDto>>> GetUpcomingMeetingsAsync(Guid userId);
    Task<ApiResponse<MeetingDto>> GetMeetingByIdAsync(Guid meetingId, Guid currentUserId);
    Task<ApiResponse<MeetingDto>> GetMeetingByPublicTokenAsync(string publicToken);
    Task<ApiResponse<MeetingDto>> UpdateMeetingAsync(Guid meetingId, Guid currentUserId, UpdateMeetingRequest request);
    Task<ApiResponse<bool>> DeleteMeetingAsync(Guid meetingId, Guid currentUserId);
    Task<ApiResponse<JoinMeetingResponse>> JoinMeetingAsync(Guid meetingId, Guid? currentUserId, JoinMeetingRequest request);
    Task<ApiResponse<bool>> LeaveMeetingAsync(Guid meetingId, Guid currentUserId, string? participantIdentity = null);
    Task<ApiResponse<bool>> EndMeetingAsync(Guid meetingId, Guid currentUserId);
}
