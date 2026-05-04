using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Orizon.Application.DTOs.Email;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services.External;

public class GmailIntegrationService : IGmailService
{
    private readonly ILogger<GmailIntegrationService> _logger;

    public GmailIntegrationService(ILogger<GmailIntegrationService> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<EmailSummaryDto>> GetRecentEmailsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "GmailService requer accessToken do usuário — implementado na Fase 7");
    }

    public async Task<IEnumerable<EmailSummaryDto>> GetRecentEmailsWithTokenAsync(
        string accessToken,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando emails recentes do Gmail");

        var credential = GoogleCredential.FromAccessToken(accessToken);

        var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Orizon",
        });

        var request = service.Users.Messages.List("me");
        request.Q = "is:unread newer_than:1d";
        request.MaxResults = maxResults;

        var listResponse = await request.ExecuteAsync(cancellationToken);

        if (listResponse.Messages == null || !listResponse.Messages.Any())
            return [];

        var emails = new List<EmailSummaryDto>();

        foreach (var message in listResponse.Messages)
        {
            var msgRequest = service.Users.Messages.Get("me", message.Id);
            msgRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
            msgRequest.MetadataHeaders = new Google.Apis.Util.Repeatable<string>(["From", "Subject", "Date"]);

            var msg = await msgRequest.ExecuteAsync(cancellationToken);

            var subject = GetHeader(msg, "Subject") ?? "(sem assunto)";
            var from = GetHeader(msg, "From") ?? "Desconhecido";
            var dateStr = GetHeader(msg, "Date");
            var date = dateStr != null ? DateTime.Parse(dateStr) : DateTime.UtcNow;

            emails.Add(new EmailSummaryDto
            {
                From = from,
                Subject = subject,
                AISummary = msg.Snippet ?? "",
                Category = "Info",
                CategoryEmoji = "📧",
                ReceivedAt = date,
            });
        }

        return emails;
    }

    private static string? GetHeader(Message message, string name)
    {
        return message.Payload?.Headers?
            .FirstOrDefault(h => h.Name == name)?.Value;
    }
}