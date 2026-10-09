using System.ComponentModel.DataAnnotations;
using VideoConference.Domain.Enums;

namespace VideoConference.Application.DTOs;

public class CreateMeetingRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Password { get; set; }
    public bool IsWaitingRoomEnabled { get; set; }
    public DateTime? ScheduledStartAt { get; set; }
    public DateTime? ScheduledEndAt { get; set; }
}

public class UpdateMeetingRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Password { get; set; }
    public bool? RemovePassword { get; set; }
    public bool IsWaitingRoomEnabled { get; set; }
    public MeetingStatus? Status { get; set; }
    public DateTime? ScheduledStartAt { get; set; }
    public DateTime? ScheduledEndAt { get; set; }
}

public class MeetingDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public Guid HostUserId { get; set; }
    public string HostDisplayName { get; set; } = string.Empty;
    public bool IsPasswordProtected { get; set; }
    public bool IsWaitingRoomEnabled { get; set; }
    public MeetingStatus Status { get; set; }
    public DateTime? ScheduledStartAt { get; set; }
    public DateTime? ScheduledEndAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? PublicToken { get; set; }
    public string? MeetingLink { get; set; }
    public int ActiveParticipantCount { get; set; }
}

public class JoinMeetingRequest
{
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
}

public class JoinMeetingResponse
{
    public string ServerUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string ParticipantIdentity { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public ParticipantRole Role { get; set; }
    public bool IsHost { get; set; }
    public Guid MeetingId { get; set; }
    public string Title { get; set; } = string.Empty;
}
