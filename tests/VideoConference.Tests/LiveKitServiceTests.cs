using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConference.Infrastructure.Services;
using Xunit;

namespace VideoConference.Tests;

public class LiveKitServiceTests
{
    private readonly LiveKitService _liveKitService;

    public LiveKitServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "LIVEKIT_URL", "ws://localhost:7880" },
                { "LIVEKIT_API_KEY", "devkey" },
                { "LIVEKIT_API_SECRET", "secret123456789012345678901234567890" }
            })
            .Build();

        _liveKitService = new LiveKitService(config, NullLogger<LiveKitService>.Instance);
    }

    [Fact]
    public void GenerateToken_ShouldCreateValidJwtWithRoomAndIdentity()
    {
        // Act
        var token = _liveKitService.GenerateToken(
            roomName: "room_test_123",
            participantIdentity: "p_opaque_1",
            participantName: "Alice",
            isHost: false);

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("devkey", jwt.Issuer);
        Assert.Equal("p_opaque_1", jwt.Subject);
        Assert.Equal("Alice", jwt.Claims.First(c => c.Type == "name").Value);

        // Check video grant
        var videoClaim = jwt.Claims.FirstOrDefault(c => c.Type == "video");
        Assert.NotNull(videoClaim);
        Assert.Contains("room_test_123", videoClaim.Value);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeHostPrivileges_WhenHost()
    {
        // Act
        var token = _liveKitService.GenerateToken(
            roomName: "room_host_test",
            participantIdentity: "p_host_1",
            participantName: "Host User",
            isHost: true);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var videoClaim = jwt.Claims.FirstOrDefault(c => c.Type == "video");

        Assert.NotNull(videoClaim);
        Assert.Contains("\"roomAdmin\":true", videoClaim.Value);
    }
}
