namespace VideoConference.Application.DTOs;

public class StartRecordingRequest
{
    public string? OutputFileName { get; set; }
}

public class RecordingDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public TimeSpan? Duration { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
