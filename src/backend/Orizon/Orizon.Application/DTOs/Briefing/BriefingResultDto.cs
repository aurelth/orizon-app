using Orizon.Application.DTOs.Calendar;
using Orizon.Application.DTOs.Email;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.DTOs.Weather;

namespace Orizon.Application.DTOs.Briefing;

public class BriefingResultDto
{
    public Guid BriefingId { get; set; }
    public DateOnly Date { get; set; }
    public string UserName { get; set; } = string.Empty;
    public WeatherDto Weather { get; set; } = null!;
    public IEnumerable<EmailSummaryDto> Emails { get; set; } = new List<EmailSummaryDto>();
    public IEnumerable<CalendarEventDto> CalendarEvents { get; set; } = new List<CalendarEventDto>();
    public IEnumerable<TrelloTaskDto>? TrelloTasks { get; set; }  // null se não configurado
    public BriefingAISummaryDto AISummary { get; set; } = null!;
    public DateTime GeneratedAt { get; set; }
}