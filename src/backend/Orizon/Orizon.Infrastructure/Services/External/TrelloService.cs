using System.Text.Json;
using Microsoft.Extensions.Logging;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services.External;

public class TrelloService : ITrelloService
{
    private readonly HttpClient _httpClient;
    private readonly IUserRepository _userRepository;
    private readonly ITrelloBoardConfigRepository _boardConfigRepository;
    private readonly ILogger<TrelloService> _logger;
    private const string BaseUrl = "https://api.trello.com/1";

    public TrelloService(
        HttpClient httpClient,
        IUserRepository userRepository,
        ITrelloBoardConfigRepository boardConfigRepository,
        ILogger<TrelloService> logger)
    {
        _httpClient = httpClient;
        _userRepository = userRepository;
        _boardConfigRepository = boardConfigRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<TrelloBoardDto>> GetBoardsAsync(
        string apiKey,
        string token,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/members/me/boards" +
            $"?key={apiKey}&token={token}" +
            $"&fields=id,name,prefs,closed" +
            $"&lists=open";

        _logger.LogInformation("Buscando boards do Trello");

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var boards = JsonSerializer.Deserialize<JsonElement[]>(json);

        var result = new List<TrelloBoardDto>();

        foreach (var board in boards ?? [])
        {
            if (board.GetProperty("closed").GetBoolean()) continue;

            var lists = new List<TrelloListDto>();
            if (board.TryGetProperty("lists", out var listsElement))
            {
                foreach (var list in listsElement.EnumerateArray())
                {
                    var listName = list.GetProperty("name").GetString() ?? "";
                    lists.Add(new TrelloListDto
                    {
                        ListId = list.GetProperty("id").GetString() ?? "",
                        Name = listName,
                        DetectedType = DetectListType(listName),
                    });
                }
            }

            var color = board
                .GetProperty("prefs")
                .GetProperty("backgroundColor")
                .GetString();

            result.Add(new TrelloBoardDto
            {
                BoardId = board.GetProperty("id").GetString() ?? "",
                Name = board.GetProperty("name").GetString() ?? "",
                Color = color,
                IsActive = true,
                Lists = lists,
            });
        }

        return result;
    }

    public async Task<IEnumerable<TrelloTaskDto>> GetActiveTasksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            _logger.LogWarning("UserId inválido: {UserId}", userId);
            return [];
        }

        var user = await _userRepository.GetByIdAsync(userGuid, cancellationToken);

        if (user is null || !user.TrelloEnabled)
        {
            _logger.LogInformation("Usuário {UserId} não possui Trello habilitado", userId);
            return [];
        }

        if (string.IsNullOrEmpty(user.TrelloApiKey) || string.IsNullOrEmpty(user.TrelloToken))
        {
            _logger.LogWarning("Usuário {UserId} não possui credenciais Trello", userId);
            return [];
        }

        var configs = await _boardConfigRepository.GetByUserAsync(userGuid, cancellationToken);
        if (!configs.Any())
        {
            _logger.LogInformation("Usuário {UserId} não possui boards configurados", userId);
            return [];
        }

        _logger.LogInformation("Buscando tarefas Trello para usuário {UserId}", userId);

        return await GetTasksFromConfiguredBoardsAsync(
            user.TrelloApiKey,
            user.TrelloToken,
            configs,
            cancellationToken);
    }

    public async Task<IEnumerable<TrelloTaskDto>> GetTasksFromConfiguredBoardsAsync(
        string apiKey,
        string token,
        IEnumerable<Domain.Entities.TrelloBoardConfig> configs,
        CancellationToken cancellationToken = default)
    {
        var tasks = new List<TrelloTaskDto>();

        foreach (var config in configs.Where(c => c.IsActive))
        {
            var url = $"{BaseUrl}/boards/{config.BoardId}/cards" +
                $"?key={apiKey}&token={token}" +
                $"&fields=id,name,idList,due,dueComplete,labels,dateLastActivity,desc";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) continue;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var cards = JsonSerializer.Deserialize<JsonElement[]>(json);

            foreach (var card in cards ?? [])
            {
                var listId = card.GetProperty("idList").GetString();
                var isTodayList = listId == config.TodayListId;
                var isInProgressList = listId == config.InProgressListId;

                if (!isTodayList && !isInProgressList) continue;

                var columnType = isTodayList ? "today" : "inprogress";
                var lastActivity = DateTime.Parse(
                    card.GetProperty("dateLastActivity").GetString()!);

                int? daysInProgress = null;
                if (isInProgressList)
                    daysInProgress = (int)(DateTime.UtcNow - lastActivity).TotalDays;

                tasks.Add(new TrelloTaskDto
                {
                    CardId = card.GetProperty("id").GetString() ?? "",
                    Title = card.GetProperty("name").GetString() ?? "",
                    BoardName = config.BoardName,
                    BoardColor = config.BoardColor ?? "#6ee7b7",
                    ListName = isTodayList
                        ? config.TodayListName ?? "Today"
                        : config.InProgressListName ?? "In Progress",
                    ColumnType = columnType,
                    MovedToInProgressAt = isInProgressList ? lastActivity : null,
                    DaysInProgress = daysInProgress,
                });
            }
        }

        return tasks;
    }

    private static string? DetectListType(string listName)
    {
        var lower = listName.ToLower();
        if (lower.Contains("today") || lower.Contains("hoje")) return "today";
        if (lower.Contains("progress") || lower.Contains("fazendo") ||
            lower.Contains("doing") || lower.Contains("em andamento")) return "inprogress";
        return null;
    }
}