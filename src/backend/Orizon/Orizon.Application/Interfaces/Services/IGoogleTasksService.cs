using Orizon.Application.DTOs.Tasks;

namespace Orizon.Application.Interfaces.Services;

public interface IGoogleTasksService
{
    Task<IEnumerable<GoogleTaskDto>> GetTodayTasksAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<GoogleTaskDto>> GetTodayTasksWithTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}