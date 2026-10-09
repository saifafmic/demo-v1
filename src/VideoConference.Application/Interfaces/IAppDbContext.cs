using Microsoft.EntityFrameworkCore;
using VideoConference.Domain.Entities;

namespace VideoConference.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Meeting> Meetings { get; set; }
    DbSet<MeetingParticipant> Participants { get; set; }
    DbSet<MeetingInvitation> Invitations { get; set; }
    DbSet<MeetingLink> MeetingLinks { get; set; }
    DbSet<MeetingRecording> Recordings { get; set; }
    DbSet<MeetingMessage> Messages { get; set; }
    DbSet<Notification> Notifications { get; set; }
    DbSet<MeetingHistory> MeetingHistories { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
