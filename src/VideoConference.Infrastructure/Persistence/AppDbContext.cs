using Microsoft.EntityFrameworkCore;
using VideoConference.Application.Interfaces;
using VideoConference.Domain.Entities;

namespace VideoConference.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Meeting> Meetings { get; set; } = null!;
    public DbSet<MeetingParticipant> Participants { get; set; } = null!;
    public DbSet<MeetingInvitation> Invitations { get; set; } = null!;
    public DbSet<MeetingLink> MeetingLinks { get; set; } = null!;
    public DbSet<MeetingRecording> Recordings { get; set; } = null!;
    public DbSet<MeetingMessage> Messages { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<MeetingHistory> MeetingHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PasswordHash).IsRequired();

            entity.HasMany(e => e.HostedMeetings)
                  .WithOne(m => m.HostUser)
                  .HasForeignKey(m => m.HostUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Participations)
                  .WithOne(p => p.User)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Notifications)
                  .WithOne(n => n.User)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.SentInvitations)
                  .WithOne(i => i.SenderUser)
                  .HasForeignKey(i => i.SenderUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Meeting
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RoomName).IsUnique();
            entity.HasIndex(e => e.HostUserId);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RoomName).IsRequired().HasMaxLength(100);

            entity.HasMany(e => e.Participants)
                  .WithOne(p => p.Meeting)
                  .HasForeignKey(p => p.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Invitations)
                  .WithOne(i => i.Meeting)
                  .HasForeignKey(i => i.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Links)
                  .WithOne(l => l.Meeting)
                  .HasForeignKey(l => l.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Messages)
                  .WithOne(m => m.Meeting)
                  .HasForeignKey(m => m.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Recordings)
                  .WithOne(r => r.Meeting)
                  .HasForeignKey(r => r.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.History)
                  .WithOne(h => h.Meeting)
                  .HasForeignKey(h => h.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MeetingParticipant
        modelBuilder.Entity<MeetingParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MeetingId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(150);
        });

        // MeetingInvitation
        modelBuilder.Entity<MeetingInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.MeetingId);
            entity.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
        });

        // MeetingLink
        modelBuilder.Entity<MeetingLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PublicToken).IsUnique();
            entity.HasIndex(e => e.MeetingId);
            entity.Property(e => e.PublicToken).IsRequired().HasMaxLength(100);
        });

        // MeetingMessage
        modelBuilder.Entity<MeetingMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MeetingId);
            entity.Property(e => e.SenderName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
        });

        // MeetingRecording
        modelBuilder.Entity<MeetingRecording>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MeetingId);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(500);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
        });

        // MeetingHistory
        modelBuilder.Entity<MeetingHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MeetingId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
        });
    }
}
