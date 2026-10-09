using VideoConference.Application.Common;
using VideoConference.Application.DTOs;

namespace VideoConference.Application.Interfaces;

public interface IInvitationService
{
    Task<ApiResponse<InvitationDto>> CreateInvitationAsync(Guid meetingId, Guid currentUserId, CreateInvitationRequest request);
    Task<ApiResponse<List<InvitationDto>>> GetMeetingInvitationsAsync(Guid meetingId, Guid currentUserId);
    Task<ApiResponse<MeetingDto>> AcceptInvitationAsync(string token, Guid currentUserId);
}
