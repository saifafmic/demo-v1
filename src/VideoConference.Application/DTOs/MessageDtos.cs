using System.ComponentModel.DataAnnotations;

namespace VideoConference.Application.DTOs;

public class SendMessageRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;

    public string? SenderName { get; set; }
}

public class MeetingMessageDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid? UserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
