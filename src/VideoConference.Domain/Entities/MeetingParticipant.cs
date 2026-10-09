using VideoConference.Domain.Enums;

namespace VideoConference.Domain.Entities;

public class MeetingParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public ParticipantRole Role { get; set; } = ParticipantRole.Participant;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsAllowed { get; set; } = true;
    public bool IsBanned { get; set; } = false;
}
