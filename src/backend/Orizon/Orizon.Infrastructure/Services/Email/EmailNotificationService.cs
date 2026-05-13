using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orizon.Application.DTOs.Briefing;
using Orizon.Application.Interfaces.Services;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Globalization;

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
            HtmlContent = BuildEmailHtml(briefing),
        };

        msg.AddTo(new EmailAddress(toEmail, userName));

        var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

        if (!response.IsSuccessStatusCode)
            _logger.LogError("Falha ao enviar email para {Email} — Status: {Status}",
                toEmail, response.StatusCode);
        else
            _logger.LogInformation("Email enviado com sucesso para {Email}", toEmail);
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string userName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Enviando email de redefinição de senha para {Email}", toEmail);

        var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@orizonapp.io";
        var fromName = _configuration["Email:FromName"] ?? "Orizon";
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:4200";

        var resetLink =
            $"{frontendUrl}/auth/reset-password?email={Uri.EscapeDataString(toEmail)}&token={resetToken}";

        var msg = new SendGridMessage
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = "🔐 Redefinição de senha — Orizon",
            HtmlContent = BuildPasswordResetEmailHtml(userName, resetLink),
        };

        msg.AddTo(new EmailAddress(toEmail, userName));

        var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

        if (!response.IsSuccessStatusCode)
            _logger.LogError(
                "Falha ao enviar email de redefinição para {Email} — Status: {Status}",
                toEmail, response.StatusCode);
        else
            _logger.LogInformation(
                "Email de redefinição enviado com sucesso para {Email}", toEmail);
    }

    private static string BuildPasswordResetEmailHtml(string userName, string resetLink)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>Redefinição de senha — Orizon</title>
            </head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="background:#f3f4f6;padding:32px 16px;">
                <tr>
                  <td align="center">
                    <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="max-width:560px;">

                      <!-- Header -->
                      <tr>
                        <td align="center" style="padding-bottom:28px;">
                          <div style="font-size:28px;font-weight:800;color:#ea580c;letter-spacing:-1px;">🌅 Orizon</div>
                          <div style="font-size:12px;color:#9ca3af;margin-top:4px;letter-spacing:0.5px;">Your day, before it begins</div>
                        </td>
                      </tr>

                      <!-- Card -->
                      <tr>
                        <td style="padding-bottom:12px;">
                          <div style="background:#ffffff;border-radius:12px;padding:32px 28px;box-shadow:0 1px 3px rgba(0,0,0,0.06);">
                            <div style="font-size:11px;font-weight:700;color:#9ca3af;text-transform:uppercase;letter-spacing:1.5px;margin-bottom:16px;">🔐 Redefinição de senha</div>
                            <div style="font-size:20px;font-weight:700;color:#111827;margin-bottom:12px;">Olá, {userName}!</div>
                            <div style="font-size:14px;color:#6b7280;line-height:1.7;margin-bottom:24px;">
                              Recebemos uma solicitação para redefinir a senha da sua conta Orizon. Clique no botão abaixo para criar uma nova senha.
                            </div>
                            <div style="text-align:center;margin-bottom:24px;">
                              <a href="{resetLink}"
                                style="display:inline-block;background:#ea580c;color:#ffffff;font-size:15px;font-weight:700;text-decoration:none;padding:14px 32px;border-radius:8px;">
                                Redefinir senha
                              </a>
                            </div>
                            <div style="font-size:12px;color:#9ca3af;line-height:1.6;border-top:1px solid #f3f4f6;padding-top:16px;">
                              Este link expira em <strong>1 hora</strong>. Se você não solicitou a redefinição de senha, ignore este email — sua conta está segura.
                            </div>
                          </div>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td align="center" style="padding-top:16px;">
                          <div style="font-size:11px;color:#9ca3af;">Orizon — seu briefing diário personalizado</div>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string BuildEmailHtml(BriefingResultDto briefing)
    {
        var ptBR = CultureInfo.GetCultureInfo("pt-BR");
        var dateFormatted = briefing.Date.ToString("dddd, dd 'de' MMMM", ptBR);
        var dateCapitalized = char.ToUpper(dateFormatted[0]) + dateFormatted[1..];

        var eventsHtml = briefing.CalendarEvents.Any()
            ? string.Join("", briefing.CalendarEvents.Take(3).Select(e =>
            {
                var start = DateTime.Parse(e.StartTime.ToString());
                return $"""
                    <tr>
                      <td style="padding:6px 0;color:#6b7280;font-size:13px;width:48px;vertical-align:top;font-family:monospace;">{start:HH:mm}</td>
                      <td style="padding:6px 8px;font-size:13px;color:#111827;vertical-align:top;border-left:2px solid #fb923c;">{e.Title}</td>
                    </tr>
                    """;
            }))
            : "<tr><td colspan='2' style='color:#9ca3af;font-size:13px;padding:6px 0;'>Sem eventos hoje</td></tr>";

        var emailsHtml = briefing.Emails.Any()
            ? string.Join("", briefing.Emails.Take(3).Select(e => $"""
                <div style="padding:10px 0;border-bottom:1px solid #f3f4f6;">
                  <div style="font-size:11px;color:#9ca3af;margin-bottom:2px;">{e.From}</div>
                  <div style="font-size:13px;color:#111827;font-weight:600;">{e.Subject}</div>
                </div>
                """))
            : "<div style='color:#9ca3af;font-size:13px;padding:6px 0;'>Nenhum email não lido</div>";

        var chips = string.Join("", briefing.AISummary.ActionChips.Select(c =>
            $"""<span style="display:inline-block;background:#fff7ed;color:#ea580c;border:1px solid #fed7aa;border-radius:999px;padding:4px 12px;font-size:12px;margin:3px 3px 3px 0;">{c}</span>"""));

        var chipsSection = chips.Length > 0
            ? $"<div style='margin-top:14px;'>{chips}</div>"
            : "";

        var priorityHtml = briefing.AISummary.PriorityTask != null
            ? $"""
              <tr>
                <td style="padding-bottom:12px;">
                  <div style="background:#fff7ed;border:1px solid #fed7aa;border-left:4px solid #ea580c;border-radius:8px;padding:14px 16px;">
                    <div style="font-size:11px;font-weight:700;color:#ea580c;text-transform:uppercase;letter-spacing:1px;margin-bottom:6px;">🎯 Tarefa prioritária</div>
                    <div style="font-size:14px;color:#111827;font-weight:500;">{briefing.AISummary.PriorityTask}</div>
                  </div>
                </td>
              </tr>
              """
            : "";

        return $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>Orizon Briefing</title>
            </head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="background:#f3f4f6;padding:32px 16px;">
                <tr>
                  <td align="center">
                    <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="max-width:560px;">

                      <!-- Header -->
                      <tr>
                        <td align="center" style="padding-bottom:28px;">
                          <div style="font-size:28px;font-weight:800;color:#ea580c;letter-spacing:-1px;">🌅 Orizon</div>
                          <div style="font-size:12px;color:#9ca3af;margin-top:4px;letter-spacing:0.5px;">Your day, before it begins</div>
                        </td>
                      </tr>

                      <!-- Greeting -->
                      <tr>
                        <td style="padding-bottom:20px;">
                          <div style="font-size:22px;font-weight:700;color:#111827;line-height:1.4;">{briefing.AISummary.Greeting}</div>
                          <div style="font-size:14px;color:#6b7280;margin-top:6px;">{dateCapitalized}</div>
                        </td>
                      </tr>

                      <!-- Clima -->
                      <tr>
                        <td style="padding-bottom:12px;">
                          <div style="background:#ffffff;border-radius:12px;padding:18px 20px;box-shadow:0 1px 3px rgba(0,0,0,0.06);">
                            <div style="font-size:11px;font-weight:700;color:#9ca3af;text-transform:uppercase;letter-spacing:1.5px;margin-bottom:12px;">☁️ Clima</div>
                            <div style="font-size:28px;font-weight:800;color:#111827;">{briefing.Weather.WeatherEmoji} {briefing.Weather.CurrentTemperature}°C</div>
                            <div style="font-size:13px;color:#6b7280;margin-top:4px;">{briefing.Weather.Description} &nbsp;·&nbsp; mín {briefing.Weather.MinTemperature}° / máx {briefing.Weather.MaxTemperature}°</div>
                            <div style="font-size:13px;color:#374151;margin-top:10px;line-height:1.5;">{briefing.AISummary.WeatherSummary}</div>
                          </div>
                        </td>
                      </tr>

                      <!-- Sugestões -->
                      <tr>
                        <td style="padding-bottom:12px;">
                          <div style="background:#ffffff;border-radius:12px;padding:18px 20px;box-shadow:0 1px 3px rgba(0,0,0,0.06);">
                            <div style="font-size:11px;font-weight:700;color:#9ca3af;text-transform:uppercase;letter-spacing:1.5px;margin-bottom:12px;">💡 Sugestões do dia</div>
                            <div style="font-size:14px;color:#111827;line-height:1.7;">{briefing.AISummary.Suggestions}</div>
                            {chipsSection}
                          </div>
                        </td>
                      </tr>

                      <!-- Tarefa prioritária -->
                      {priorityHtml}

                      <!-- Agenda -->
                      <tr>
                        <td style="padding-bottom:12px;">
                          <div style="background:#ffffff;border-radius:12px;padding:18px 20px;box-shadow:0 1px 3px rgba(0,0,0,0.06);">
                            <div style="font-size:11px;font-weight:700;color:#9ca3af;text-transform:uppercase;letter-spacing:1.5px;margin-bottom:12px;">📅 Agenda de hoje</div>
                            <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
                              {eventsHtml}
                            </table>
                          </div>
                        </td>
                      </tr>

                      <!-- Emails -->
                      <tr>
                        <td style="padding-bottom:12px;">
                          <div style="background:#ffffff;border-radius:12px;padding:18px 20px;box-shadow:0 1px 3px rgba(0,0,0,0.06);">
                            <div style="font-size:11px;font-weight:700;color:#9ca3af;text-transform:uppercase;letter-spacing:1.5px;margin-bottom:12px;">✉️ Emails importantes</div>
                            {emailsHtml}
                          </div>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td align="center" style="padding-top:16px;padding-bottom:8px;">
                          <div style="font-size:11px;color:#9ca3af;">Gerado por Orizon em {briefing.GeneratedAt:dd/MM/yyyy HH:mm}</div>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}