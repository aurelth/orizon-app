using Orizon.Application.DTOs.Briefing;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using System.Diagnostics;
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
    private readonly IOrizonMetrics _metrics;
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
        IOrizonMetrics metrics,
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
        _metrics = metrics;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Executa o job para todos os usuários cujo BriefingHour bate com a hora atual
    /// ou nos horários fixos de 12h e 18h. Usado pelo cron.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById(
            "E. South America Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone);
        var currentHour = now.Hour;

        _logger.LogInformation("BriefingJob iniciado às {Time}", DateTime.UtcNow);

        try
        {
            var allUsers = await _userRepository.GetActiveUsersAsync(ct);

            var users = allUsers
                .Where(u =>
                    u.BriefingHour == currentHour ||
                    currentHour == 12 ||
                    currentHour == 18)
                .ToList();

            if (!users.Any())
            {
                _logger.LogInformation(
                    "Nenhum usuário para processar às {Hour}h Brasília", currentHour);
                return;
            }

            _logger.LogInformation(
                "Gerando briefing para {Count} usuários às {Hour}h Brasília",
                users.Count, currentHour);

            await Task.WhenAll(users.Select(user =>
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

    /// <summary>
    /// Executa o job para um usuário específico, ignorando o filtro de hora.
    /// Usado pelo botão "Atualizar" do dashboard.
    /// </summary>
    public async Task ExecuteForUserAsync(
        string userId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "BriefingJob acionado manualmente para usuário {UserId}", userId);

        if (!Guid.TryParse(userId, out var userGuid))
        {
            _logger.LogWarning("UserId inválido recebido: {UserId}", userId);
            return;
        }

        var user = await _userRepository.GetByIdAsync(userGuid, ct);
        if (user is null)
        {
            _logger.LogWarning(
                "Usuário {UserId} não encontrado para geração manual", userId);
            return;
        }

        await ProcessUserBriefingAsync(user, ct);

        _logger.LogInformation(
            "BriefingJob manual concluído para usuário {UserId}", userId);
    }

    private async Task ProcessUserBriefingAsync(AppUser user, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById(
            "E. South America Standard Time");
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));
        var userId = user.Id.ToString();

        _logger.LogInformation("Processando briefing para usuário {UserId}", userId);

        var existing = await _briefingRepository
            .GetByUserAndDateAsync(userId, today, ct);
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

        // Armazena o DTO para envio de email — separado do try de geração
        BriefingResultDto? briefingResult = null;

        try
        {
            var googleToken = await EnsureValidGoogleTokenAsync(user, ct);

            var lat = user.IsTraveling && user.TravelLatitude.HasValue
                ? user.TravelLatitude.Value : user.Latitude;
            var lon = user.IsTraveling && user.TravelLongitude.HasValue
                ? user.TravelLongitude.Value : user.Longitude;
            var timezone = user.Timezone;

            var emailsTask = user.EmailSectionEnabled && !string.IsNullOrEmpty(googleToken)
                ? _gmailService.GetRecentEmailsWithTokenAsync(googleToken, cancellationToken: ct)
                : Task.FromResult<IEnumerable<Application.DTOs.Email.EmailSummaryDto>>(
                    Enumerable.Empty<Application.DTOs.Email.EmailSummaryDto>());

            var eventsTask = user.CalendarSectionEnabled && !string.IsNullOrEmpty(googleToken)
                ? _calendarService.GetTodayEventsWithTokenAsync(googleToken, ct)
                : Task.FromResult<IEnumerable<Application.DTOs.Calendar.CalendarEventDto>>(
                    Enumerable.Empty<Application.DTOs.Calendar.CalendarEventDto>());

            var googleTasksTask = user.TasksSectionEnabled && !string.IsNullOrEmpty(googleToken)
                ? _googleTasksService.GetTodayTasksWithTokenAsync(googleToken, ct)
                : Task.FromResult<IEnumerable<Application.DTOs.Tasks.GoogleTaskDto>>(
                    Enumerable.Empty<Application.DTOs.Tasks.GoogleTaskDto>());

            var weatherTask = user.WeatherSectionEnabled
                ? _weatherService.GetWeatherAsync(lat, lon, timezone, ct)
                : Task.FromResult<Application.DTOs.Weather.WeatherDto?>(null);

            var trelloTask = user.TrelloSectionEnabled && user.TrelloEnabled
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
            briefing.WeatherJson = weather is not null
                ? JsonSerializer.Serialize(weather) : null;
            briefing.TrelloTasksJson = user.TrelloSectionEnabled && user.TrelloEnabled
                ? JsonSerializer.Serialize(trelloTasks) : null;

            var aiSummary = await _claudeService.GenerateDailySummaryAsync(
                emails, events, googleTasks,
                user.TrelloSectionEnabled && user.TrelloEnabled ? trelloTasks : null,
                weather, user.DisplayName, today, ct);

            briefing.AISummary = aiSummary.Greeting;
            briefing.AISuggestions = aiSummary.Suggestions;
            briefing.Status = BriefingStatus.Generated;
            briefing.GeneratedAt = DateTime.UtcNow;
            await _briefingRepository.UpdateAsync(briefing, ct);

            sw.Stop();
            _metrics.RecordBriefingGenerated();
            _metrics.RecordBriefingDuration(sw.Elapsed.TotalSeconds);

            await NotifyUserAsync(userId, briefing.Id, ct);

            // DTO pronto para o email — construído após salvar como Generated
            briefingResult = new BriefingResultDto
            {
                BriefingId = briefing.Id,
                Date = today,
                UserName = user.DisplayName,
                Weather = weather!,
                Emails = emails,
                CalendarEvents = events,
                GoogleTasks = googleTasks,
                TrelloTasks = user.TrelloSectionEnabled && user.TrelloEnabled
                    ? trelloTasks : null,
                AISummary = aiSummary,
                GeneratedAt = briefing.GeneratedAt.Value,
            };

            _logger.LogInformation(
                "Briefing gerado com sucesso para usuário {UserId} em {Duration}s",
                userId, sw.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _metrics.RecordBriefingFailed();
            _logger.LogError(ex,
                "Falha ao gerar briefing para usuário {UserId}", userId);
            briefing.Status = BriefingStatus.Failed;
            briefing.ErrorMessage = ex.Message;
            await _briefingRepository.UpdateAsync(briefing, ct);
        }

        // Email isolado: falha não altera o status do briefing de Generated para Failed
        if (briefingResult is not null)
        {
            try
            {
                await _emailService.SendBriefingEmailAsync(
                    user.Email, user.DisplayName, briefingResult, ct);

                briefing.EmailSentAt = DateTime.UtcNow;
                await _briefingRepository.UpdateAsync(briefing, ct);

                _logger.LogInformation(
                    "Email de briefing enviado com sucesso para usuário {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Falha ao enviar email para usuário {UserId} — briefing permanece como Generated",
                    userId);
            }
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

        _logger.LogInformation(
            "Renovando Google Access Token para usuário {UserId}", user.Id);

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
            var json = JsonSerializer.Serialize(payload);
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