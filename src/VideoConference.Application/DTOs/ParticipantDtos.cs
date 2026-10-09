using VideoConference.Domain.Enums;

namespace VideoConference.Application.DTOs;

public class ParticipantDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public ParticipantRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool IsAllowed { get; set; }
    public bool IsBanned { get; set; }
}

public class ModerateParticipantRequest
{
    public string Action { get; set; } = string.Empty; // "remove", "ban", "promote", "demote", "admit", "reject"
}
