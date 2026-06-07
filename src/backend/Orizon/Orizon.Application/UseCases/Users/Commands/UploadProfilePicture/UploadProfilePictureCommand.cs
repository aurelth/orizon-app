using MediatR;

namespace Orizon.Application.UseCases.Users.Commands.UploadProfilePicture;

public record UploadProfilePictureCommand(
    Guid UserId,
    byte[] FileBytes,
    string FileName,
    string ContentType,
    long FileSize
) : IRequest<string>;