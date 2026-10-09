using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConference.Application.DTOs;
using VideoConference.Domain.Entities;
using VideoConference.Domain.Enums;
using VideoConference.Infrastructure.Services;
using Xunit;

namespace VideoConference.Tests;

public class AuthServiceTests
{
    private readonly JwtTokenService _jwtTokenService;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Issuer", "Test" },
                { "Jwt:Audience", "Test" },
                { "Jwt:Key", "Test_Super_Secret_Key_1234567890_Must_Be_Long_Enough!" }
            })
            .Build();

        _jwtTokenService = new JwtTokenService(config);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenValid()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var authService = new AuthService(db, _jwtTokenService, NullLogger<AuthService>.Instance);

        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            FirstName = "Alice",
            LastName = "Smith"
        };

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("newuser@example.com", result.Data.Email);
        Assert.Equal("Alice Smith", result.Data.DisplayName);

        var savedUser = db.Users.FirstOrDefault(u => u.Email == "newuser@example.com");
        Assert.NotNull(savedUser);
        Assert.True(BCrypt.Net.BCrypt.Verify("SecurePassword123!", savedUser.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenDuplicateEmail()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var authService = new AuthService(db, _jwtTokenService, NullLogger<AuthService>.Instance);

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "duplicate@example.com",
            PasswordHash = "hashed",
            FirstName = "First",
            LastName = "Last",
            DisplayName = "First Last"
        });
        await db.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "AnotherPassword123!",
            FirstName = "Second",
            LastName = "User"
        };

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var authService = new AuthService(db, _jwtTokenService, NullLogger<AuthService>.Instance);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "loginuser@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            FirstName = "Login",
            LastName = "User",
            DisplayName = "Login User",
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act
        var result = await authService.LoginAsync(new LoginRequest
        {
            Email = "loginuser@example.com",
            Password = "CorrectPassword123!"
        });

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenPasswordInvalid()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var authService = new AuthService(db, _jwtTokenService, NullLogger<AuthService>.Instance);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "wrongpass@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            FirstName = "Pass",
            LastName = "User",
            DisplayName = "Pass User",
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act
        var result = await authService.LoginAsync(new LoginRequest
        {
            Email = "wrongpass@example.com",
            Password = "WrongPassword999!"
        });

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid email or password", result.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenUserDeactivated()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var authService = new AuthService(db, _jwtTokenService, NullLogger<AuthService>.Instance);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "deactivated@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "Inactive",
            LastName = "User",
            DisplayName = "Inactive User",
            IsActive = false
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act
        var result = await authService.LoginAsync(new LoginRequest
        {
            Email = "deactivated@example.com",
            Password = "Password123!"
        });

        // Assert
        Assert.False(result.Success);
        Assert.Contains("deactivated", result.Message);
    }
}
