using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Worker.Jobs;

public class BriefingJob
{
    private readonly IUserRepository _userRepository;
    private readonly IBriefingRepository _briefingRepository;
    private readonly ILogger<BriefingJob> _logger;

    public BriefingJob(
        IUserRepository userRepository,
        IBriefingRepository briefingRepository,
        ILogger<BriefingJob> logger)
    {
        _userRepository = userRepository;
        _briefingRepository = briefingRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation(
            "BriefingJob iniciado às {Time}",
            DateTime.UtcNow);

        try
        {
            // Busca todos os usuários com integrações configuradas
            var users = await _userRepository.GetActiveUsersAsync(ct);
            var userList = users.ToList();

            _logger.LogInformation(
                "Gerando briefing para {Count} usuários",
                userList.Count);

            foreach (var user in userList)
            {
                _logger.LogInformation(
                    "Processando briefing para usuário {UserId}",
                    user.Id);

                // TODO Fase 7: implementar pipeline completo
                // 1. GetEmailsAsync
                // 2. GetCalendarEventsAsync
                // 3. GetTrelloTasksAsync (se habilitado)
                // 4. GetWeatherAsync
                // 5. GenerateSummaryAsync (Claude)
                // 6. SaveBriefingAsync
                // 7. NotifyViaSignalR
                // 8. SendEmailAsync
            }

            _logger.LogInformation(
                "BriefingJob concluído com sucesso às {Time}",
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "BriefingJob falhou às {Time}",
                DateTime.UtcNow);
            throw;
        }
    }
}