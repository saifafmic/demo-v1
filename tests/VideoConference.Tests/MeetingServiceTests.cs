using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Entities;
using VideoConference.Domain.Enums;
using VideoConference.Infrastructure.Services;
using Xunit;

namespace VideoConference.Tests;

public class MeetingServiceTests
{
    private readonly Mock<ILiveKitService> _mockLiveKitService;
    private readonly Mock<IMeetingHistoryService> _mockHistoryService;
    private readonly IConfiguration _config;

    public MeetingServiceTests()
    {
        _mockLiveKitService = new Mock<ILiveKitService>();
        _mockLiveKitService.Setup(s => s.GetServerUrl()).Returns("ws://localhost:7880");
        _mockLiveKitService.Setup(s => s.GenerateToken(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<TimeSpan?>()))
            .Returns("mocked.livekit.jwt");

        _mockHistoryService = new Mock<IMeetingHistoryService>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Client:BaseUrl", "http://localhost:3000" }
            })
            .Build();
    }

    [Fact]
    public async Task CreateMeetingAsync_ShouldCreateMeeting_WithRoomAndLink()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var host = new User
        {
            Id = Guid.NewGuid(),
            Email = "host@example.com",
            DisplayName = "Meeting Host",
            FirstName = "Meeting",
            LastName = "Host"
        };
        db.Users.Add(host);
        await db.SaveChangesAsync();

        var service = new MeetingService(db, _mockLiveKitService.Object, _mockHistoryService.Object, _config, NullLogger<MeetingService>.Instance);

        var request = new CreateMeetingRequest
        {
            Title = "Sprint Planning",
            Description = "Review upcoming tickets",
            Password = "optionalpassword"
        };

        // Act
        var result = await service.CreateMeetingAsync(host.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Sprint Planning", result.Data.Title);
        Assert.True(result.Data.IsPasswordProtected);
        Assert.NotNull(result.Data.MeetingLink);
        Assert.StartsWith("room_", result.Data.RoomName);

        var meetingInDb = db.Meetings.FirstOrDefault(m => m.Id == result.Data.Id);
        Assert.NotNull(meetingInDb);
        Assert.Equal(host.Id, meetingInDb.HostUserId);
    }

    [Fact]
    public async Task JoinMeetingAsync_ShouldFail_WhenPasswordIsIncorrect()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var host = new User { Id = Guid.NewGuid(), Email = "host@example.com", DisplayName = "Host" };
        var guest = new User { Id = Guid.NewGuid(), Email = "guest@example.com", DisplayName = "Guest" };
        db.Users.AddRange(host, guest);

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Protected Meeting",
            RoomName = "room_protected_1",
            HostUserId = host.Id,
            IsPasswordProtected = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct123!"),
            Status = MeetingStatus.Live,
            CreatedAt = DateTime.UtcNow
        };
        db.Meetings.Add(meeting);
        await db.SaveChangesAsync();

        var service = new MeetingService(db, _mockLiveKitService.Object, _mockHistoryService.Object, _config, NullLogger<MeetingService>.Instance);

        // Act
        var result = await service.JoinMeetingAsync(meeting.Id, guest.Id, new JoinMeetingRequest
        {
            Password = "WrongPassword!",
            DisplayName = "Guest"
        });

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid meeting password", result.Message);
    }

    [Fact]
    public async Task JoinMeetingAsync_ShouldFail_WhenParticipantIsBanned()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var host = new User { Id = Guid.NewGuid(), Email = "host@example.com", DisplayName = "Host" };
        var bannedUser = new User { Id = Guid.NewGuid(), Email = "banned@example.com", DisplayName = "Banned User" };
        db.Users.AddRange(host, bannedUser);

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Open Meeting",
            RoomName = "room_open_1",
            HostUserId = host.Id,
            Status = MeetingStatus.Live,
            CreatedAt = DateTime.UtcNow
        };
        db.Meetings.Add(meeting);

        db.Participants.Add(new MeetingParticipant
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            UserId = bannedUser.Id,
            DisplayName = bannedUser.DisplayName,
            Role = ParticipantRole.Participant,
            IsAllowed = false,
            IsBanned = true
        });
        await db.SaveChangesAsync();

        var service = new MeetingService(db, _mockLiveKitService.Object, _mockHistoryService.Object, _config, NullLogger<MeetingService>.Instance);

        // Act
        var result = await service.JoinMeetingAsync(meeting.Id, bannedUser.Id, new JoinMeetingRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Contains("banned", result.Message);
    }

    [Fact]
    public async Task EndMeetingAsync_ShouldSucceed_WhenHostRequests()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var host = new User { Id = Guid.NewGuid(), Email = "host@example.com", DisplayName = "Host" };
        db.Users.Add(host);

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Ending Meeting",
            RoomName = "room_end_test",
            HostUserId = host.Id,
            Status = MeetingStatus.Live,
            CreatedAt = DateTime.UtcNow
        };
        db.Meetings.Add(meeting);
        await db.SaveChangesAsync();

        var service = new MeetingService(db, _mockLiveKitService.Object, _mockHistoryService.Object, _config, NullLogger<MeetingService>.Instance);

        // Act
        var result = await service.EndMeetingAsync(meeting.Id, host.Id);

        // Assert
        Assert.True(result.Success);
        var updatedMeeting = db.Meetings.First(m => m.Id == meeting.Id);
        Assert.Equal(MeetingStatus.Ended, updatedMeeting.Status);
        Assert.NotNull(updatedMeeting.EndedAt);
    }

    [Fact]
    public async Task EndMeetingAsync_ShouldFail_WhenNonHostRequests()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var host = new User { Id = Guid.NewGuid(), Email = "host@example.com", DisplayName = "Host" };
        var randomUser = new User { Id = Guid.NewGuid(), Email = "random@example.com", DisplayName = "Random" };
        db.Users.AddRange(host, randomUser);

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Ending Meeting",
            RoomName = "room_end_test_2",
            HostUserId = host.Id,
            Status = MeetingStatus.Live,
            CreatedAt = DateTime.UtcNow
        };
        db.Meetings.Add(meeting);
        await db.SaveChangesAsync();

        var service = new MeetingService(db, _mockLiveKitService.Object, _mockHistoryService.Object, _config, NullLogger<MeetingService>.Instance);

        // Act
        var result = await service.EndMeetingAsync(meeting.Id, randomUser.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only host, co-host, or admin can end the meeting", result.Message);
    }
}
