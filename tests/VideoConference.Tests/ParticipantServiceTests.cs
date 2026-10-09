using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Entities;
using VideoConference.Domain.Enums;
using VideoConference.Infrastructure.Services;
using Xunit;

namespace VideoConference.Tests;

public class ParticipantServiceTests
{
    private readonly Mock<ILiveKitService> _mockLiveKitService;
    private readonly Mock<IMeetingHistoryService> _mockHistoryService;

    public ParticipantServiceTests()
    {
        _mockLiveKitService = new Mock<ILiveKitService>();
        _mockHistoryService = new Mock<IMeetingHistoryService>();
    }

    [Fact]
    public async Task ModerateParticipant_ShouldBanParticipant_WhenHostRequests()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var host = new User { Id = Guid.NewGuid(), Email = "host@test.com", DisplayName = "Host User" };
        var participant = new User { Id = Guid.NewGuid(), Email = "part@test.com", DisplayName = "Regular User" };
        db.Users.AddRange(host, participant);

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Moderated Meeting",
            RoomName = "room_mod_1",
            HostUserId = host.Id,
            Status = MeetingStatus.Live
        };
        db.Meetings.Add(meeting);

        var participantRecord = new MeetingParticipant
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            UserId = participant.Id,
            DisplayName = participant.DisplayName,
            Role = ParticipantRole.Participant,
            IsAllowed = true,
            IsBanned = false
        };
        db.Participants.Add(participantRecord);
        await db.SaveChangesAsync();

        var service = new ParticipantService(db, _mockHistoryService.Object, _mockLiveKitService.Object, NullLogger<ParticipantService>.Instance);

        // Act
        var result = await service.BanParticipantAsync(meeting.Id, participantRecord.Id, host.Id);

        // Assert
        Assert.True(result.Success);
        var updated = db.Participants.First(p => p.Id == participantRecord.Id);
        Assert.True(updated.IsBanned);
        Assert.False(updated.IsAllowed);
    }

    [Fact]
    public async Task ModerateParticipant_ShouldFail_WhenNonHostRequests()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var host = new User { Id = Guid.NewGuid(), Email = "host@test.com", DisplayName = "Host User" };
        var maliciousUser = new User { Id = Guid.NewGuid(), Email = "malicious@test.com", DisplayName = "Malicious" };
        var targetUser = new User { Id = Guid.NewGuid(), Email = "target@test.com", DisplayName = "Target" };
        db.Users.AddRange(host, maliciousUser, targetUser);

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Meeting",
            RoomName = "room_sec_1",
            HostUserId = host.Id,
            Status = MeetingStatus.Live
        };
        db.Meetings.Add(meeting);

        var targetParticipant = new MeetingParticipant
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            UserId = targetUser.Id,
            DisplayName = targetUser.DisplayName,
            Role = ParticipantRole.Participant,
            IsAllowed = true,
            IsBanned = false
        };
        db.Participants.Add(targetParticipant);
        await db.SaveChangesAsync();

        var service = new ParticipantService(db, _mockHistoryService.Object, _mockLiveKitService.Object, NullLogger<ParticipantService>.Instance);

        // Act
        var result = await service.RemoveParticipantAsync(meeting.Id, targetParticipant.Id, maliciousUser.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only host, co-host, or admin can moderate", result.Message);
    }

    [Fact]
    public async Task PromoteParticipant_ShouldPromoteToCoHost_WhenHostRequests()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryDbContext();
        var host = new User { Id = Guid.NewGuid(), Email = "host@test.com", DisplayName = "Host User" };
        var participant = new User { Id = Guid.NewGuid(), Email = "part@test.com", DisplayName = "Regular User" };
        db.Users.AddRange(host, participant);

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Promote Meeting",
            RoomName = "room_promote_1",
            HostUserId = host.Id,
            Status = MeetingStatus.Live
        };
        db.Meetings.Add(meeting);

        var participantRecord = new MeetingParticipant
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            UserId = participant.Id,
            DisplayName = participant.DisplayName,
            Role = ParticipantRole.Participant,
            IsAllowed = true,
            IsBanned = false
        };
        db.Participants.Add(participantRecord);
        await db.SaveChangesAsync();

        var service = new ParticipantService(db, _mockHistoryService.Object, _mockLiveKitService.Object, NullLogger<ParticipantService>.Instance);

        // Act
        var result = await service.PromoteParticipantAsync(meeting.Id, participantRecord.Id, host.Id);

        // Assert
        Assert.True(result.Success);
        var updated = db.Participants.First(p => p.Id == participantRecord.Id);
        Assert.Equal(ParticipantRole.CoHost, updated.Role);
    }
}
