using VideoConference.Application.Common;
using VideoConference.Application.DTOs;

namespace VideoConference.Application.Interfaces;

public interface IRecordingService
{
    Task<ApiResponse<RecordingDto>> StartRecordingAsync(Guid meetingId, Guid currentUserId, StartRecordingRequest request);
    Task<ApiResponse<RecordingDto>> StopRecordingAsync(Guid meetingId, Guid currentUserId);
    Task<ApiResponse<List<RecordingDto>>> GetMeetingRecordingsAsync(Guid meetingId, Guid currentUserId);
}
