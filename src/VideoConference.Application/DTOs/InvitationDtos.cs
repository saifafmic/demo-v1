using System.ComponentModel.DataAnnotations;
using VideoConference.Domain.Enums;

namespace VideoConference.Application.DTOs;

public class CreateInvitationRequest
{
    [Required]
    [EmailAddress]
    public string RecipientEmail { get; set; } = string.Empty;
}

public class InvitationDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public string MeetingTitle { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string InvitationUrl { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MeetingLinkDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public string PublicToken { get; set; } = string.Empty;
    public string JoinUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
