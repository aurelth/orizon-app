using Microsoft.Extensions.Logging;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    ];

    private static readonly Dictionary<string, string> ContentTypeExtensions = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
    }

    public async Task<string> SaveAsync(
        byte[] fileBytes,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        var normalizedContentType = contentType.ToLowerInvariant();

        if (!AllowedContentTypes.Contains(normalizedContentType))
            throw new InvalidOperationException(
                "Tipo de arquivo não permitido. Use JPG, PNG ou WebP.");

        var wwwroot = GetWwwRootPath();
        var uploadsPath = Path.Combine(wwwroot, folder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(uploadsPath);

        var extension = ContentTypeExtensions.GetValueOrDefault(normalizedContentType, ".jpg");
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        await File.WriteAllBytesAsync(filePath, fileBytes, ct);

        _logger.LogInformation("Arquivo salvo em {FilePath}", filePath);

        return $"/{folder}/{uniqueFileName}";
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(relativePath)) return Task.CompletedTask;

        try
        {
            var wwwroot = GetWwwRootPath();
            var fullPath = Path.Combine(
                wwwroot,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Arquivo removido: {FilePath}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao remover arquivo: {RelativePath}", relativePath);
        }

        return Task.CompletedTask;
    }

    private static string GetWwwRootPath()
        => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
}