using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using VideoConference.Domain.Entities;
using VideoConference.Domain.Enums;
using VideoConference.Infrastructure.Services;
using Xunit;

namespace VideoConference.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:Key", "Test_Super_Secret_Key_1234567890_Must_Be_Long_Enough!" },
            { "Jwt:AccessTokenMinutes", "60" }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _jwtTokenService = new JwtTokenService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeRequiredClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            Role = UserRole.User
        };

        // Act
        var (token, expiresAt) = _jwtTokenService.GenerateAccessToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.True(expiresAt > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "TestAudience");
        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("User", jwt.Claims.First(c => c.Type == ClaimTypes.Role || c.Type == "role").Value);
        Assert.NotNull(jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));
    }

    [Fact]
    public void HashToken_ShouldBeDeterministic()
    {
        // Arrange
        var rawToken = _jwtTokenService.GenerateRefreshToken();

        // Act
        var hash1 = _jwtTokenService.HashToken(rawToken);
        var hash2 = _jwtTokenService.HashToken(rawToken);

        // Assert
        Assert.Equal(hash1, hash2);
    }
}
