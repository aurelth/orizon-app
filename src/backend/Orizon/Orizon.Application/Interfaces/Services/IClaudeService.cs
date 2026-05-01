using Orizon.Application.DTOs.Briefing;
using Orizon.Application.DTOs.Calendar;
using Orizon.Application.DTOs.Email;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.DTOs.Weather;

namespace Orizon.Application.Interfaces.Services;

public interface IClaudeService
{    
    Task<BriefingAISummaryDto> GenerateDailySummaryAsync(
        IEnumerable<EmailSummaryDto> emails,
        IEnumerable<CalendarEventDto> events,
        IEnumerable<TrelloTaskDto>? tasks,
        WeatherDto weather,
        string userName,
        CancellationToken cancellationToken = default);
}