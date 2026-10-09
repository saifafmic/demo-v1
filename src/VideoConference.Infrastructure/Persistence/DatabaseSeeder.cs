using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoConference.Domain.Entities;
using VideoConference.Domain.Enums;

namespace VideoConference.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            if (await context.Users.AnyAsync())
            {
                return; // Database already seeded
            }

            logger.LogInformation("Seeding initial development users...");

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@example.com",
                FirstName = "System",
                LastName = "Administrator",
                DisplayName = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var regularUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                DisplayName = "John Doe",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                Role = UserRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(adminUser, regularUser);
            await context.SaveChangesAsync();

            // Seed a sample welcome meeting hosted by admin
            var sampleMeeting = new Meeting
            {
                Id = Guid.NewGuid(),
                Title = "Welcome to VideoConference!",
                Description = "Sample orientation and demo room for testing video, audio, screen sharing, and chat.",
                RoomName = $"welcome_room_{Guid.NewGuid():N}",
                HostUserId = adminUser.Id,
                IsPasswordProtected = false,
                IsWaitingRoomEnabled = false,
                Status = MeetingStatus.Live,
                CreatedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow
            };

            var sampleLink = new MeetingLink
            {
                Id = Guid.NewGuid(),
                MeetingId = sampleMeeting.Id,
                PublicToken = "welcome-room",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var adminParticipant = new MeetingParticipant
            {
                Id = Guid.NewGuid(),
                MeetingId = sampleMeeting.Id,
                UserId = adminUser.Id,
                DisplayName = adminUser.DisplayName,
                Role = ParticipantRole.Host,
                JoinedAt = DateTime.UtcNow,
                IsAllowed = true,
                IsBanned = false
            };

            context.Meetings.Add(sampleMeeting);
            context.MeetingLinks.Add(sampleLink);
            context.Participants.Add(adminParticipant);

            await context.SaveChangesAsync();

            logger.LogInformation("Development seed completed successfully. Admin: admin@example.com, User: user@example.com");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }
}
