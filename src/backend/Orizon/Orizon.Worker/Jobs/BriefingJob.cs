using Microsoft.AspNetCore.SignalR.Client;
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
    private readonly ITrelloService _trelloService;
    private readonly IWeatherService _weatherService;
    private readonly IClaudeService _claudeService;
    private readonly IEmailNotificationService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BriefingJob> _logger;

    public BriefingJob(
        IUserRepository userRepository,
        IBriefingRepository briefingRepository,
        IGmailService gmailService,
        ICalendarService calendarService,
        ITrelloService trelloService,
        IWeatherService weatherService,
        IClaudeService claudeService,
        IEmailNotificationService emailService,
        IConfiguration configuration,
        ILogger<BriefingJob> logger)
    {
        _userRepository = userRepository;
        _briefingRepository = briefingRepository;
        _gmailService = gmailService;
        _calendarService = calendarService;
        _trelloService = trelloService;
        _weatherService = weatherService;
        _claudeService = claudeService;
        _emailService = emailService;
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

            // ALTERADO: Task.WhenAll para execução paralela por usuário
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userId = user.Id.ToString();

        _logger.LogInformation(
            "Processando briefing para usuário {UserId}", userId);

        var briefing = new BriefingEntry
        {
            UserId = user.Id,
            Date = today,
            Status = BriefingStatus.Pending,
        };
        await _briefingRepository.AddAsync(briefing, ct);

        try
        {
            // localização: usa viagem se ativo
            var lat = user.IsTraveling && user.TravelLatitude.HasValue
                ? user.TravelLatitude.Value : user.Latitude;
            var lon = user.IsTraveling && user.TravelLongitude.HasValue
                ? user.TravelLongitude.Value : user.Longitude;
            var timezone = user.Timezone;

            // ALTERADO: Task.WhenAll para execução paralela das integrações
            var emailsTask = _gmailService.GetRecentEmailsAsync(userId, ct);
            var eventsTask = _calendarService.GetTodayEventsAsync(userId, ct);
            // CORRIGIDO: GetWeatherAsync com parâmetro timezone
            var weatherTask = _weatherService.GetWeatherAsync(lat, lon, timezone, ct);
            // CORRIGIDO: GetActiveTasksAsync em vez de GetTasksAsync
            var trelloTask = user.TrelloEnabled
                ? _trelloService.GetActiveTasksAsync(userId, ct)
                : Task.FromResult<IEnumerable<Application.DTOs.Trello.TrelloTaskDto>>(
                    Enumerable.Empty<Application.DTOs.Trello.TrelloTaskDto>());

            await Task.WhenAll(emailsTask, eventsTask, weatherTask, trelloTask);

            var emails = await emailsTask;
            var events = await eventsTask;
            var weather = await weatherTask;
            var trelloTasks = await trelloTask;

            briefing.EmailSummaryJson = JsonSerializer.Serialize(emails);
            briefing.CalendarEventsJson = JsonSerializer.Serialize(events);
            briefing.WeatherJson = JsonSerializer.Serialize(weather);
            briefing.TrelloTasksJson = user.TrelloEnabled
                ? JsonSerializer.Serialize(trelloTasks) : null;

            // gera resumo com Claude
            var aiSummary = await _claudeService.GenerateDailySummaryAsync(
                emails,
                events,
                user.TrelloEnabled ? trelloTasks : null,
                weather,
                user.DisplayName,
                ct);

            briefing.AISummary = aiSummary.Greeting;
            briefing.AISuggestions = aiSummary.Suggestions;
            briefing.Status = BriefingStatus.Generated;
            briefing.GeneratedAt = DateTime.UtcNow;

            await _briefingRepository.UpdateAsync(briefing, ct);

            // notifica via SignalR
            await NotifyUserAsync(userId, briefing.Id, ct);

            // envia email
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

    private async Task NotifyUserAsync(
        string userId, Guid briefingId, CancellationToken ct)
    {
        try
        {
            var hubUrl = _configuration["SignalR:HubUrl"]
                ?? "http://localhost:5010/hubs/briefing";

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            await connection.StartAsync(ct);
            await connection.InvokeAsync(
                "SendBriefingReady", userId, briefingId, cancellationToken: ct);
            await connection.StopAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Falha ao notificar usuário {UserId} via SignalR", userId);
        }
    }
}