using VideoConference.Application.Common;
using VideoConference.Application.DTOs;

namespace VideoConference.Application.Interfaces;

public interface IParticipantService
{
    Task<ApiResponse<List<ParticipantDto>>> GetMeetingParticipantsAsync(Guid meetingId, Guid currentUserId);
    Task<ApiResponse<bool>> RemoveParticipantAsync(Guid meetingId, Guid participantId, Guid currentUserId);
    Task<ApiResponse<bool>> BanParticipantAsync(Guid meetingId, Guid participantId, Guid currentUserId);
    Task<ApiResponse<bool>> PromoteParticipantAsync(Guid meetingId, Guid participantId, Guid currentUserId);
    Task<ApiResponse<bool>> DemoteParticipantAsync(Guid meetingId, Guid participantId, Guid currentUserId);
}
