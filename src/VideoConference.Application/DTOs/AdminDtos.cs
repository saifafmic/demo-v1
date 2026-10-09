namespace VideoConference.Application.DTOs;

public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalMeetings { get; set; }
    public int LiveMeetings { get; set; }
    public int ScheduledMeetings { get; set; }
    public int TotalRecordings { get; set; }
}
