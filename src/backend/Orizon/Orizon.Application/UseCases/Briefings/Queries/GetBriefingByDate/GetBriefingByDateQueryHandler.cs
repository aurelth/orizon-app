using MediatR;
using Orizon.Application.DTOs.Briefing;
using Orizon.Application.DTOs.Calendar;
using Orizon.Application.DTOs.Email;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.Interfaces.Repositories;
using System.Text.Json;

namespace Orizon.Application.UseCases.Briefings.Queries.GetBriefingByDate;

public class GetBriefingByDateQueryHandler
    : IRequestHandler<GetBriefingByDateQuery, BriefingResultDto?>
{
    private readonly IBriefingRepository _briefingRepository;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GetBriefingByDateQueryHandler(IBriefingRepository briefingRepository)
    {
        _briefingRepository = briefingRepository;
    }

    public async Task<BriefingResultDto?> Handle(
        GetBriefingByDateQuery request,
        CancellationToken cancellationToken)
    {
        var briefing = await _briefingRepository.GetByUserAndDateAsync(
            request.UserId,
            request.Date,
            cancellationToken);

        if (briefing is null) return null;

        var weather = briefing.WeatherJson is not null
            ? JsonSerializer.Deserialize<WeatherDto>(briefing.WeatherJson, _jsonOptions)
            : null;

        var emails = briefing.EmailSummaryJson is not null
            ? JsonSerializer.Deserialize<IEnumerable<EmailSummaryDto>>(
                briefing.EmailSummaryJson, _jsonOptions)
              ?? Enumerable.Empty<EmailSummaryDto>()
            : Enumerable.Empty<EmailSummaryDto>();

        var events = briefing.CalendarEventsJson is not null
            ? JsonSerializer.Deserialize<IEnumerable<CalendarEventDto>>(
                briefing.CalendarEventsJson, _jsonOptions)
              ?? Enumerable.Empty<CalendarEventDto>()
            : Enumerable.Empty<CalendarEventDto>();

        var tasks = briefing.TrelloTasksJson is not null
            ? JsonSerializer.Deserialize<IEnumerable<TrelloTaskDto>>(
                briefing.TrelloTasksJson, _jsonOptions)
            : null;

        BriefingAISummaryDto? aiSummary = null;
        if (briefing.AISummary is not null)
        {
            aiSummary = new BriefingAISummaryDto
            {
                Greeting = briefing.AISummary,
                WeatherSummary = string.Empty,
                Suggestions = briefing.AISuggestions ?? string.Empty,
            };
        }

        return new BriefingResultDto
        {
            BriefingId = briefing.Id,
            Date = briefing.Date,
            UserName = request.UserName,
            Weather = weather!,
            Emails = emails,
            CalendarEvents = events,
            TrelloTasks = tasks,
            AISummary = aiSummary!,
            GeneratedAt = briefing.GeneratedAt ?? DateTime.UtcNow,
        };
    }
}