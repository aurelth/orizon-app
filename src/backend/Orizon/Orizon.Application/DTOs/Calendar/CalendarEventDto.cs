namespace Orizon.Application.DTOs.Calendar;

public class CalendarEventDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<string> Participants { get; set; } = new();
    public string? MeetLink { get; set; }
    public string? Description { get; set; }
    public bool ConflictsWithRain { get; set; } = false;
}