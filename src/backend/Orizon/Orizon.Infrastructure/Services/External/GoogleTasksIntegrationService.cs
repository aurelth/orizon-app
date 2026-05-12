using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Tasks.v1;
using Microsoft.Extensions.Logging;
using Orizon.Application.DTOs.Tasks;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services.External;

public class GoogleTasksIntegrationService : IGoogleTasksService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GoogleTasksIntegrationService> _logger;

    public GoogleTasksIntegrationService(
        IUserRepository userRepository,
        ILogger<GoogleTasksIntegrationService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<GoogleTaskDto>> GetTodayTasksAsync(
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
            _logger.LogWarning("Usuário {UserId} não encontrado para buscar tasks", userId);
            return [];
        }

        if (string.IsNullOrEmpty(user.GoogleAccessToken))
        {
            _logger.LogWarning("Usuário {UserId} não possui Google Access Token", userId);
            return [];
        }

        return await GetTodayTasksWithTokenAsync(user.GoogleAccessToken, cancellationToken);
    }

    public async Task<IEnumerable<GoogleTaskDto>> GetTodayTasksWithTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando tasks do Google Tasks");

        var credential = GoogleCredential.FromAccessToken(accessToken);

        var service = new TasksService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Orizon",
        });

        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var nowBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone);
        var todayEnd = new DateTime(nowBrasilia.Year, nowBrasilia.Month, nowBrasilia.Day,
            23, 59, 59, DateTimeKind.Unspecified);
        var todayEndUtc = TimeZoneInfo.ConvertTimeToUtc(todayEnd, brasiliaZone);

        try
        {
            var taskLists = await service.Tasklists.List().ExecuteAsync(cancellationToken);

            if (taskLists.Items == null || !taskLists.Items.Any())
                return [];

            var allTasks = new List<GoogleTaskDto>();

            foreach (var taskList in taskLists.Items)
            {
                var request = service.Tasks.List(taskList.Id);
                request.ShowCompleted = false;
                request.ShowHidden = false;
                request.DueMax = todayEndUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");

                var tasks = await request.ExecuteAsync(cancellationToken);

                if (tasks.Items == null) continue;

                foreach (var task in tasks.Items.Where(t => t.Status == "needsAction"))
                {
                    DateTime? dueDate = null;
                    bool isOverdue = false;

                    if (task.Due != null && DateTime.TryParse(
                        task.Due,
                        null,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out var due))
                    {
                        dueDate = due;
                        isOverdue = CalculateIsOverdue(task.Due, nowBrasilia);
                    }

                    allTasks.Add(new GoogleTaskDto
                    {
                        Id = task.Id,
                        Title = task.Title ?? "(sem título)",
                        Notes = task.Notes,
                        DueDate = dueDate,
                        IsOverdue = isOverdue,
                        TaskListName = taskList.Title ?? "Tasks",
                    });
                }
            }

            return allTasks.OrderBy(t => t.DueDate)
               .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao buscar Google Tasks — pode ser falta de scope");
            return [];
        }
    }

    internal static bool CalculateIsOverdue(string due, DateTime nowBrasilia)
    {
        if (!DateTime.TryParse(
            due,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var parsed))
            return false;

        return parsed.ToUniversalTime().Date < nowBrasilia.Date;
    }
}