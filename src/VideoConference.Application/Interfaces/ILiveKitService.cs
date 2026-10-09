namespace VideoConference.Application.Interfaces;

public interface ILiveKitService
{
    string GenerateToken(
        string roomName,
        string participantIdentity,
        string participantName,
        bool isHost,
        bool isCoHost = false,
        TimeSpan? validFor = null);

    Task<bool> DeleteRoomAsync(string roomName);
    Task<bool> RemoveParticipantAsync(string roomName, string participantIdentity);
    Task<bool> MuteParticipantTrackAsync(string roomName, string participantIdentity, string trackSid, bool muted);
    string GetServerUrl();
}
