namespace VideoConference.Application.Interfaces;

public interface ILiveStreamingService
{
    bool IsConfigured();
    Task<string> StartStreamAsync(string roomName, string rtmpUrl);
    Task<bool> StopStreamAsync(string streamId);
}
