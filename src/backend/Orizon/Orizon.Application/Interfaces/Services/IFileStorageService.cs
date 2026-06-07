namespace Orizon.Application.Interfaces.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        byte[] fileBytes,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    Task DeleteAsync(
        string relativePath,
        CancellationToken ct = default);
}