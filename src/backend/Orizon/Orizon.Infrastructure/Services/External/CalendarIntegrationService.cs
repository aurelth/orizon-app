using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Orizon.Application.DTOs.Calendar;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services.External;

public class CalendarIntegrationService : ICalendarService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CalendarIntegrationService> _logger;

    public CalendarIntegrationService(
        IUserRepository userRepository,
        ILogger<CalendarIntegrationService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<CalendarEventDto>> GetTodayEventsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            _logger.LogWarning("UserId inválido: {UserId}", userId);
            return [];
        }

        var user = await _userRepository.GetByIdAsync(userGuid, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Usuário {UserId} não encontrado para buscar eventos", userId);
            return [];
        }

        if (string.IsNullOrEmpty(user.GoogleAccessToken))
        {
            _logger.LogWarning("Usuário {UserId} não possui Google Access Token", userId);
            return [];
        }

        return await GetTodayEventsWithTokenAsync(
            user.GoogleAccessToken, cancellationToken);
    }

    public async Task<IEnumerable<CalendarEventDto>> GetTodayEventsWithTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando eventos do Google Calendar");

        var credential = GoogleCredential.FromAccessToken(accessToken);

        var service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Orizon",
        });

        var today = DateTime.UtcNow.Date;
        var request = service.Events.List("primary");
        request.TimeMinDateTimeOffset = today;
        request.TimeMaxDateTimeOffset = today.AddDays(1);
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync(cancellationToken);

        if (events.Items == null || !events.Items.Any())
            return [];

        return events.Items.Select(e =>
        {
            var start = e.Start.DateTimeDateTimeOffset?.DateTime
                ?? DateTime.Parse(e.Start.Date);
            var end = e.End.DateTimeDateTimeOffset?.DateTime
                ?? DateTime.Parse(e.End.Date);

            var attendees = e.Attendees?
                .Select(a => a.Email)
                .Where(email => email != null)
                .Cast<string>()
                .ToList() ?? [];

            var meetLink = e.ConferenceData?.EntryPoints?
                .FirstOrDefault(ep => ep.EntryPointType == "video")?.Uri;

            return new CalendarEventDto
            {
                Title = e.Summary ?? "(sem título)",
                StartTime = start,
                EndTime = end,
                Participants = attendees,
                MeetLink = meetLink,
                Description = e.Description,
            };
        });
    }
}