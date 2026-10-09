namespace VideoConference.Domain.Entities;

public class MeetingMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
