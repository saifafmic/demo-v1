using VideoConference.Domain.Enums;

namespace VideoConference.Domain.Entities;

public class Meeting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public Guid HostUserId { get; set; }
    public User HostUser { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public bool IsPasswordProtected { get; set; }
    public bool IsWaitingRoomEnabled { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
    public DateTime? ScheduledStartAt { get; set; }
    public DateTime? ScheduledEndAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // Navigation properties
    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
    public ICollection<MeetingInvitation> Invitations { get; set; } = new List<MeetingInvitation>();
    public ICollection<MeetingLink> Links { get; set; } = new List<MeetingLink>();
    public ICollection<MeetingMessage> Messages { get; set; } = new List<MeetingMessage>();
    public ICollection<MeetingRecording> Recordings { get; set; } = new List<MeetingRecording>();
    public ICollection<MeetingHistory> History { get; set; } = new List<MeetingHistory>();
}
