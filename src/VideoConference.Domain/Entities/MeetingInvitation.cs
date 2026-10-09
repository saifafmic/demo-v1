using VideoConference.Domain.Enums;

namespace VideoConference.Domain.Entities;

public class MeetingInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public Guid SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
