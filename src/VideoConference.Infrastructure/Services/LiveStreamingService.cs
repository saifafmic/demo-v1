using Microsoft.Extensions.Logging;
using VideoConference.Application.Interfaces;

namespace VideoConference.Infrastructure.Services;

public class LiveStreamingService : ILiveStreamingService
{
    private readonly ILogger<LiveStreamingService> _logger;

    public LiveStreamingService(ILogger<LiveStreamingService> logger)
    {
        _logger = logger;
    }

    public bool IsConfigured() => false;

    public Task<string> StartStreamAsync(string roomName, string rtmpUrl)
    {
        _logger.LogInformation("RTMP stream requested for room {RoomName} to {RtmpUrl}", roomName, rtmpUrl);
        return Task.FromResult($"stream_{Guid.NewGuid():N}");
    }

    public Task<bool> StopStreamAsync(string streamId)
    {
        _logger.LogInformation("RTMP stream {StreamId} stopped", streamId);
        return Task.FromResult(true);
    }
}
