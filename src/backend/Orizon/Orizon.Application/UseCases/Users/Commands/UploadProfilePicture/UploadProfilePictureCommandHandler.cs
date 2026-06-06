using MediatR;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Users.Commands.UploadProfilePicture;

public class UploadProfilePictureCommandHandler
    : IRequestHandler<UploadProfilePictureCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;

    // 5 MB
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public UploadProfilePictureCommandHandler(
        IUserRepository userRepository,
        IFileStorageService fileStorageService)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<string> Handle(
        UploadProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileSize > MaxFileSizeBytes)
            throw new InvalidOperationException(
                "Arquivo muito grande. Tamanho máximo: 5MB.");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        // Remove foto anterior se for arquivo local
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl) &&
            user.ProfilePictureUrl.StartsWith("/uploads/"))
        {
            await _fileStorageService.DeleteAsync(
                user.ProfilePictureUrl, cancellationToken);
        }
        
        var relativePath = await _fileStorageService.SaveAsync(
            request.FileBytes,
            request.FileName,
            request.ContentType,
            "uploads/profile-pictures",
            cancellationToken);

        user.ProfilePictureUrl = relativePath;
        await _userRepository.UpdateAsync(user, cancellationToken);

        return relativePath;
    }
}