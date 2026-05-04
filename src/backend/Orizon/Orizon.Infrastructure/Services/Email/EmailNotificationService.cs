using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orizon.Application.DTOs.Briefing;
using Orizon.Application.Interfaces.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Orizon.Infrastructure.Services.Email;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        ISendGridClient sendGridClient,
        IConfiguration configuration,
        ILogger<EmailNotificationService> logger)
    {
        _sendGridClient = sendGridClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendBriefingEmailAsync(
        string toEmail,
        string userName,
        BriefingResultDto briefing,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enviando briefing por email para {Email}", toEmail);

        var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@orizonapp.io";
        var fromName = _configuration["Email:FromName"] ?? "Orizon";

        var msg = new SendGridMessage
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = $"☀️ Seu briefing de {briefing.Date:dd/MM} está pronto, {userName}!",
            HtmlContent = BuildEmailHtml(briefing, userName),
        };

        msg.AddTo(new EmailAddress(toEmail, userName));

        var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Falha ao enviar email para {Email} — Status: {Status}",
                toEmail, response.StatusCode);
        }
        else
        {
            _logger.LogInformation("Email enviado com sucesso para {Email}", toEmail);
        }
    }

    private static string BuildEmailHtml(BriefingResultDto briefing, string userName)
    {
        var css = """
            <style>
            body { font-family: Inter, sans-serif; background: #0f0d0d; color: #fafaf9; margin: 0; padding: 0; }
            .container { max-width: 600px; margin: 0 auto; padding: 2rem; }
            .header { text-align: center; margin-bottom: 2rem; }
            .brand { font-size: 1.5rem; font-weight: 700; color: #fb923c; }
            .greeting { font-size: 1.2rem; color: #fafaf9; margin: 1rem 0; }
            .section { background: #1c1917; border-radius: 8px; padding: 1rem; margin: 1rem 0; }
            .section-title { font-size: 0.875rem; color: #78716c; text-transform: uppercase; margin-bottom: 0.5rem; }
            .chip { display: inline-block; background: #292524; border-radius: 999px; padding: 0.25rem 0.75rem; font-size: 0.8rem; margin: 0.25rem; color: #fb923c; }
            .footer { text-align: center; color: #44403c; font-size: 0.75rem; margin-top: 2rem; }
            </style>
            """;

        var prioritySection = briefing.AISummary.PriorityTask != null
            ? $"""
              <div class="section">
                <div class="section-title">🎯 Tarefa prioritária</div>
                <p>{briefing.AISummary.PriorityTask}</p>
              </div>
              """
            : "";

        var chips = string.Join("",
            briefing.AISummary.ActionChips.Select(c =>
                $"<span class=\"chip\">{c}</span>"));

        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8">{css}</head>
            <body>
              <div class="container">
                <div class="header">
                  <div class="brand">🌅 Orizon</div>
                  <p style="color:#78716c;font-size:0.8rem;">Your day, before it begins</p>
                </div>
                <p class="greeting">{briefing.AISummary.Greeting}</p>
                <div class="section">
                  <div class="section-title">☁️ Clima</div>
                  <p>{briefing.Weather.WeatherEmoji} {briefing.Weather.Description} — {briefing.Weather.CurrentTemperature}°C</p>
                  <p style="color:#78716c;">{briefing.AISummary.WeatherSummary}</p>
                </div>
                <div class="section">
                  <div class="section-title">💡 Sugestões do dia</div>
                  <p>{briefing.AISummary.Suggestions}</p>
                  <div>{chips}</div>
                </div>
                {prioritySection}
                <p class="footer">Gerado por Orizon em {briefing.GeneratedAt:dd/MM/yyyy HH:mm}</p>
              </div>
            </body>
            </html>
            """;
    }
}