using MediatR;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.Interfaces.Repositories;
using System.Text.Json;

namespace Orizon.Application.UseCases.Briefings.Queries.GetBriefingHistory;

public class GetBriefingHistoryQueryHandler
    : IRequestHandler<GetBriefingHistoryQuery, GetBriefingHistoryResult>
{
    private readonly IBriefingRepository _briefingRepository;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GetBriefingHistoryQueryHandler(IBriefingRepository briefingRepository)
    {
        _briefingRepository = briefingRepository;
    }

    public async Task<GetBriefingHistoryResult> Handle(
        GetBriefingHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var (briefings, total) = await _briefingRepository.GetByUserAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            request.DateFrom,
            request.DateTo,
            cancellationToken);

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / request.PageSize);

        var items = briefings.Select(b =>
        {            
            string? weatherEmoji = null;
            if (b.WeatherJson is not null)
            {
                try
                {
                    var weather = JsonSerializer.Deserialize<WeatherDto>(b.WeatherJson, _jsonOptions);
                    weatherEmoji = weather?.WeatherEmoji;
                }
                catch { /* ignora erros de desserialização */ }
            }

            return new BriefingHistoryItemDto(
                b.Id,
                b.Date,
                b.Status.ToString(),
                b.AISummary,
                weatherEmoji,
                b.GeneratedAt);
        });

        return new GetBriefingHistoryResult(items, request.Page, request.PageSize, total, totalPages);
    }
}