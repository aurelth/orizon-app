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
        _logger.LogInformation("Buscando eventos e aniversários do Google Calendar");

        var credential = GoogleCredential.FromAccessToken(accessToken);

        var service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Orizon",
        });

        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var nowBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone);
        var todayStart = new DateTime(nowBrasilia.Year, nowBrasilia.Month, nowBrasilia.Day,
            0, 0, 0, DateTimeKind.Unspecified);
        var todayEnd = todayStart.AddDays(1);

        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayStart, brasiliaZone);
        var todayEndUtc = TimeZoneInfo.ConvertTimeToUtc(todayEnd, brasiliaZone);

        // busca eventos do calendário principal e aniversários em paralelo
        var primaryTask = FetchEventsFromCalendarAsync(
            service, "primary", todayStartUtc, todayEndUtc, isBirthday: false, cancellationToken);

        var birthdayTask = FetchEventsFromCalendarAsync(
            service, "#contacts@group.v.calendar.google.com",
            todayStartUtc, todayEndUtc, isBirthday: true, cancellationToken);

        await Task.WhenAll(primaryTask, birthdayTask);

        var allEvents = (await primaryTask).Concat(await birthdayTask)
            .OrderBy(e => e.StartTime)
            .ToList();

        return allEvents;
    }

    private static async Task<IEnumerable<CalendarEventDto>> FetchEventsFromCalendarAsync(
        CalendarService service,
        string calendarId,
        DateTime timeMin,
        DateTime timeMax,
        bool isBirthday,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = service.Events.List(calendarId);
            request.TimeMinDateTimeOffset = timeMin;
            request.TimeMaxDateTimeOffset = timeMax;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync(cancellationToken);

            if (events.Items == null || !events.Items.Any())
                return [];

            return events.Items.Select(e =>
            {
                var isAllDay = e.Start.DateTimeDateTimeOffset == null;
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
                    IsBirthday = isBirthday,
                    IsAllDay = isAllDay,
                };
            });
        }
        catch (Exception)
        {
            // calendário pode não existir ou não ter permissão — retorna vazio silenciosamente
            return [];
        }
    }
}