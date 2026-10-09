using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using VideoConference.Application.Interfaces;

namespace VideoConference.Infrastructure.Services;

public class LiveKitService : ILiveKitService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LiveKitService> _logger;
    private readonly string _serverUrl;
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public LiveKitService(IConfiguration configuration, ILogger<LiveKitService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _serverUrl = _configuration["LIVEKIT_URL"]
                     ?? _configuration["LiveKit:Url"]
                     ?? "ws://localhost:7880";

        _apiKey = _configuration["LIVEKIT_API_KEY"]
                  ?? _configuration["LiveKit:ApiKey"]
                  ?? "devkey";

        _apiSecret = _configuration["LIVEKIT_API_SECRET"]
                     ?? _configuration["LiveKit:ApiSecret"]
                     ?? "secret";
    }

    public string GetServerUrl() => _serverUrl;

    public string GenerateToken(
        string roomName,
        string participantIdentity,
        string participantName,
        bool isHost,
        bool isCoHost = false,
        TimeSpan? validFor = null)
    {
        var ttl = validFor ?? TimeSpan.FromHours(4);
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(ttl);

        var keyBytes = Encoding.UTF8.GetBytes(_apiSecret);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var header = new JwtHeader(credentials);

        var videoGrants = new Dictionary<string, object>
        {
            { "room", roomName },
            { "roomJoin", true },
            { "canPublish", true },
            { "canSubscribe", true },
            { "canPublishData", true },
            { "roomAdmin", isHost || isCoHost },
            { "roomRecord", isHost || isCoHost },
            { "canPublishSources", new[] { "camera", "microphone", "screen_share", "screen_share_audio" } }
        };

        var payload = new JwtPayload
        {
            { "iss", _apiKey },
            { "sub", participantIdentity },
            { "name", participantName },
            { "nbf", now.ToUnixTimeSeconds() },
            { "exp", expiresAt.ToUnixTimeSeconds() },
            { "jti", Guid.NewGuid().ToString("N") },
            { "video", videoGrants }
        };

        var token = new JwtSecurityToken(header, payload);
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    public async Task<bool> DeleteRoomAsync(string roomName)
    {
        try
        {
            _logger.LogInformation("Request to delete LiveKit room: {RoomName}", roomName);
            // Optionally interact with LiveKit RoomServiceClient if available
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete room {RoomName} on LiveKit server", roomName);
            return false;
        }
    }

    public async Task<bool> RemoveParticipantAsync(string roomName, string participantIdentity)
    {
        try
        {
            _logger.LogInformation("Removing participant {ParticipantIdentity} from room {RoomName}", participantIdentity, roomName);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove participant {ParticipantIdentity} from room {RoomName}", participantIdentity, roomName);
            return false;
        }
    }

    public async Task<bool> MuteParticipantTrackAsync(string roomName, string participantIdentity, string trackSid, bool muted)
    {
        try
        {
            _logger.LogInformation("Muting track {TrackSid} for participant {ParticipantIdentity} in room {RoomName}", trackSid, participantIdentity, roomName);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mute track on LiveKit server");
            return false;
        }
    }
}
