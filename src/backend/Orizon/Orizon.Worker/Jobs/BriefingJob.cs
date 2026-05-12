using Orizon.Application.DTOs.Briefing;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using System.Text.Json;

namespace Orizon.Worker.Jobs;

public class BriefingJob
{
    private readonly IUserRepository _userRepository;
    private readonly IBriefingRepository _briefingRepository;
    private readonly IGmailService _gmailService;
    private readonly ICalendarService _calendarService;
    private readonly IGoogleTasksService _googleTasksService;
    private readonly ITrelloService _trelloService;
    private readonly IWeatherService _weatherService;
    private readonly IClaudeService _claudeService;
    private readonly IEmailNotificationService _emailService;
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BriefingJob> _logger;

    public BriefingJob(
        IUserRepository userRepository,
        IBriefingRepository briefingRepository,
        IGmailService gmailService,
        ICalendarService calendarService,
        IGoogleTasksService googleTasksService,
        ITrelloService trelloService,
        IWeatherService weatherService,
        IClaudeService claudeService,
        IEmailNotificationService emailService,
        IGoogleOAuthService googleOAuthService,
        IConfiguration configuration,
        ILogger<BriefingJob> logger)
    {
        _userRepository = userRepository;
        _briefingRepository = briefingRepository;
        _gmailService = gmailService;
        _calendarService = calendarService;
        _googleTasksService = googleTasksService;
        _trelloService = trelloService;
        _weatherService = weatherService;
        _claudeService = claudeService;
        _emailService = emailService;
        _googleOAuthService = googleOAuthService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("BriefingJob iniciado às {Time}", DateTime.UtcNow);

        try
        {
            var users = await _userRepository.GetActiveUsersAsync(ct);
            var userList = users.ToList();

            _logger.LogInformation(
                "Gerando briefing para {Count} usuários", userList.Count);

            await Task.WhenAll(userList.Select(user =>
                ProcessUserBriefingAsync(user, ct)));

            _logger.LogInformation(
                "BriefingJob concluído com sucesso às {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BriefingJob falhou às {Time}", DateTime.UtcNow);
            throw;
        }
    }

    private async Task ProcessUserBriefingAsync(AppUser user, CancellationToken ct)
    {
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));
        var userId = user.Id.ToString();

        _logger.LogInformation("Processando briefing para usuário {UserId}", userId);

        var existing = await _briefingRepository.GetByUserAndDateAsync(userId, today, ct);
        var isNew = existing is null;

        var briefing = existing ?? new BriefingEntry
        {
            UserId = user.Id,
            Date = today,
        };

        briefing.Status = BriefingStatus.Pending;

        if (isNew)
            await _briefingRepository.AddAsync(briefing, ct);
        else
            await _briefingRepository.UpdateAsync(briefing, ct);

        try
        {
            var googleToken = await EnsureValidGoogleTokenAsync(user, ct);

            var lat = user.IsTraveling && user.TravelLatitude.HasValue
                ? user.TravelLatitude.Value : user.Latitude;
            var lon = user.IsTraveling && user.TravelLongitude.HasValue
                ? user.TravelLongitude.Value : user.Longitude;
            var timezone = user.Timezone;

            var emailsTask = !string.IsNullOrEmpty(googleToken)
                ? _gmailService.GetRecentEmailsWithTokenAsync(googleToken, cancellationToken: ct)
                : Task.FromResult<IEnumerable<Application.DTOs.Email.EmailSummaryDto>>(
                    Enumerable.Empty<Application.DTOs.Email.EmailSummaryDto>());

            var eventsTask = !string.IsNullOrEmpty(googleToken)
                ? _calendarService.GetTodayEventsWithTokenAsync(googleToken, ct)
                : Task.FromResult<IEnumerable<Application.DTOs.Calendar.CalendarEventDto>>(
                    Enumerable.Empty<Application.DTOs.Calendar.CalendarEventDto>());

            var googleTasksTask = !string.IsNullOrEmpty(googleToken)
                ? _googleTasksService.GetTodayTasksWithTokenAsync(googleToken, ct)
                : Task.FromResult<IEnumerable<Application.DTOs.Tasks.GoogleTaskDto>>(
                    Enumerable.Empty<Application.DTOs.Tasks.GoogleTaskDto>());

            var weatherTask = _weatherService.GetWeatherAsync(lat, lon, timezone, ct);

            var trelloTask = user.TrelloEnabled
                ? _trelloService.GetActiveTasksAsync(userId, ct)
                : Task.FromResult<IEnumerable<Application.DTOs.Trello.TrelloTaskDto>>(
                    Enumerable.Empty<Application.DTOs.Trello.TrelloTaskDto>());

            await Task.WhenAll(emailsTask, eventsTask, googleTasksTask, weatherTask, trelloTask);

            var emails = await emailsTask;
            var events = await eventsTask;
            var googleTasks = await googleTasksTask;
            var weather = await weatherTask;
            var trelloTasks = await trelloTask;

            briefing.EmailSummaryJson = JsonSerializer.Serialize(emails);
            briefing.CalendarEventsJson = JsonSerializer.Serialize(events);
            briefing.GoogleTasksJson = JsonSerializer.Serialize(googleTasks);
            briefing.WeatherJson = JsonSerializer.Serialize(weather);
            briefing.TrelloTasksJson = user.TrelloEnabled
                ? JsonSerializer.Serialize(trelloTasks) : null;

            var aiSummary = await _claudeService.GenerateDailySummaryAsync(
                emails,
                events,
                googleTasks,
                user.TrelloEnabled ? trelloTasks : null,
                weather,
                user.DisplayName,
                today,
                ct);

            briefing.AISummary = aiSummary.Greeting;
            briefing.AISuggestions = aiSummary.Suggestions;
            briefing.Status = BriefingStatus.Generated;
            briefing.GeneratedAt = DateTime.UtcNow;

            await _briefingRepository.UpdateAsync(briefing, ct);

            await NotifyUserAsync(userId, briefing.Id, ct);

            await _emailService.SendBriefingEmailAsync(
                user.Email,
                user.DisplayName,
                new BriefingResultDto
                {
                    BriefingId = briefing.Id,
                    Date = today,
                    UserName = user.DisplayName,
                    Weather = weather,
                    Emails = emails,
                    CalendarEvents = events,
                    GoogleTasks = googleTasks,
                    TrelloTasks = user.TrelloEnabled ? trelloTasks : null,
                    AISummary = aiSummary,
                    GeneratedAt = briefing.GeneratedAt.Value,
                },
                ct);

            _logger.LogInformation(
                "Briefing gerado com sucesso para usuário {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao gerar briefing para usuário {UserId}", userId);

            briefing.Status = BriefingStatus.Failed;
            briefing.ErrorMessage = ex.Message;
            await _briefingRepository.UpdateAsync(briefing, ct);
        }
    }

    private async Task<string> EnsureValidGoogleTokenAsync(
        AppUser user, CancellationToken ct)
    {
        var isExpired = user.GoogleTokenExpiresAt is null ||
                        user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5);

        if (!isExpired)
            return user.GoogleAccessToken ?? string.Empty;

        if (string.IsNullOrEmpty(user.GoogleRefreshToken))
        {
            _logger.LogWarning(
                "Usuário {UserId} sem refresh token — não é possível renovar", user.Id);
            return string.Empty;
        }

        _logger.LogInformation("Renovando Google Access Token para usuário {UserId}", user.Id);

        var tokens = await _googleOAuthService.RefreshAccessTokenAsync(
            user.GoogleRefreshToken, ct);

        user.GoogleAccessToken = tokens.AccessToken;
        user.GoogleTokenExpiresAt = tokens.ExpiresAt;

        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation(
            "Google Access Token renovado com sucesso para usuário {UserId}", user.Id);

        return tokens.AccessToken;
    }

    private async Task NotifyUserAsync(
        string userId, Guid briefingId, CancellationToken ct)
    {
        try
        {
            var apiUrl = _configuration["ApiUrl"] ?? "http://localhost:5010";

            var payload = new { UserId = userId, BriefingId = briefingId };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            await httpClient.PostAsync($"{apiUrl}/internal/briefing-ready", content, ct);

            _logger.LogInformation(
                "Notificação SignalR enviada para usuário {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Falha ao notificar usuário {UserId} via SignalR", userId);
        }
    }
}