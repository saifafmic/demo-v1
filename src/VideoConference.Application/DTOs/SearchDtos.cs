namespace VideoConference.Application.DTOs;

public class SearchResultDto
{
    public List<MeetingDto> Meetings { get; set; } = new();
    public List<UserDto> Users { get; set; } = new();
}
