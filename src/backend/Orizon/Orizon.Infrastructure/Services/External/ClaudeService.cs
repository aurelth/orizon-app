using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orizon.Application.DTOs.Briefing;
using Orizon.Application.DTOs.Calendar;
using Orizon.Application.DTOs.Email;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services.External;

public class ClaudeService : IClaudeService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeService> _logger;

    public ClaudeService(
        IConfiguration configuration,
        ILogger<ClaudeService> logger)
    {
        var apiKey = configuration["Anthropic:ApiKey"]!;
        _client = new AnthropicClient(apiKey);
        _logger = logger;
    }

    public async Task<BriefingAISummaryDto> GenerateDailySummaryAsync(
        IEnumerable<EmailSummaryDto> emails,
        IEnumerable<CalendarEventDto> events,
        IEnumerable<TrelloTaskDto>? tasks,
        WeatherDto weather,
        string userName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Gerando resumo diário com Claude para {User}", userName);

        var prompt = BuildPrompt(emails, events, tasks, weather, userName);

        var message = await _client.Messages.GetClaudeMessageAsync(
            new MessageParameters
            {
                Model = "claude-sonnet-4-5",
                MaxTokens = 1024,
                Messages =
                [
                    new Message(RoleType.User, prompt)
                ],
            }, cancellationToken);

        var content = message.Content
            .OfType<TextContent>()
            .FirstOrDefault()?.Text ?? "";

        return ParseResponse(content, userName);
    }

    private static string BuildPrompt(
        IEnumerable<EmailSummaryDto> emails,
        IEnumerable<CalendarEventDto> events,
        IEnumerable<TrelloTaskDto>? tasks,
        WeatherDto weather,
        string userName)
    {
        var emailList = string.Join("\n", emails.Select(e =>
            $"- [{e.CategoryEmoji} {e.Category}] De: {e.From} | Assunto: {e.Subject}"));

        var eventList = string.Join("\n", events.Select(e =>
            $"- {e.StartTime:HH:mm} às {e.EndTime:HH:mm}: {e.Title}" +
            (e.MeetLink != null ? " (Google Meet)" : "")));

        var taskList = tasks != null
            ? string.Join("\n", tasks.Select(t =>
                $"- [{t.ColumnType}] {t.Title}" +
                (t.IsStuck ? $" ⚠️ parado há {t.DaysInProgress} dias" : "")))
            : "Trello não configurado";

        return $"""
            Você é o assistente do Orizon, um app de briefing matinal personalizado.
            Gere um resumo conciso e motivador para {userName} começar o dia.

            CLIMA:
            {weather.WeatherEmoji} {weather.Description}
            Temperatura: {weather.CurrentTemperature}°C (min {weather.MinTemperature}°C, max {weather.MaxTemperature}°C)
            {(weather.WillRain ? $"⚠️ Vai chover a partir das {weather.RainStartHour}h" : "Sem chuva prevista")}

            EMAILS NÃO LIDOS (últimas 24h):
            {(emails.Any() ? emailList : "Nenhum email não lido")}

            AGENDA DE HOJE:
            {(events.Any() ? eventList : "Nenhum evento hoje")}

            TAREFAS TRELLO:
            {taskList}

            Responda APENAS no seguinte formato JSON, sem markdown:
            {"{"}
              "greeting": "saudação personalizada e motivadora (máx 2 linhas)",
              "weatherSummary": "resumo do clima em linguagem natural (máx 1 linha)",
              "suggestions": "2-3 sugestões cruzadas baseadas nos dados acima",
              "priorityTask": "tarefa mais importante do dia ou null",
              "actionChips": ["chip1", "chip2", "chip3"]
            {"}"}
            """;
    }

    private static BriefingAISummaryDto ParseResponse(string content, string userName)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(content).RootElement;

            return new BriefingAISummaryDto
            {
                Greeting = json.GetProperty("greeting").GetString()
                    ?? $"Bom dia, {userName}!",
                WeatherSummary = json.GetProperty("weatherSummary").GetString() ?? "",
                Suggestions = json.GetProperty("suggestions").GetString() ?? "",
                PriorityTask = json.TryGetProperty("priorityTask", out var pt)
                    ? pt.GetString()
                    : null,
                ActionChips = json.GetProperty("actionChips")
                    .EnumerateArray()
                    .Select(c => c.GetString() ?? "")
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList(),
            };
        }
        catch
        {
            return new BriefingAISummaryDto
            {
                Greeting = $"Bom dia, {userName}!",
                WeatherSummary = "Não foi possível gerar o resumo do clima.",
                Suggestions = content,
            };
        }
    }
}