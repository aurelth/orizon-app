using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Users.Commands.UploadProfilePicture;
using Orizon.Domain.Entities;

namespace Orizon.Tests.Unit.Application.UseCases.Users;

public class UploadProfilePictureCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IFileStorageService> _fileStorageMock = new();
    private readonly UploadProfilePictureCommandHandler _handler;

    private readonly AppUser _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "aurel@orizonapp.io",
        DisplayName = "Aurel",
        ProfilePictureUrl = null,
    };

    private readonly byte[] _validImageBytes = [0xFF, 0xD8, 0xFF];

    public UploadProfilePictureCommandHandlerTests()
    {
        _handler = new UploadProfilePictureCommandHandler(
            _userRepoMock.Object,
            _fileStorageMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidFile_ShouldReturnUrl()
    {
        // Arrange
        var command = new UploadProfilePictureCommand(
            _testUser.Id,
            _validImageBytes,
            "photo.jpg",
            "image/jpeg",
            _validImageBytes.Length);

        _userRepoMock
            .Setup(r => r.GetByIdAsync(_testUser.Id, default))
            .ReturnsAsync(_testUser);

        _fileStorageMock
            .Setup(s => s.SaveAsync(
                _validImageBytes,
                "photo.jpg",
                "image/jpeg",
                "uploads/profile-pictures",
                default))
            .ReturnsAsync("/uploads/profile-pictures/test.jpg");

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.Should().Be("/uploads/profile-pictures/test.jpg");
    }

    [Fact]
    public async Task Handle_WhenFileTooLarge_ShouldThrowInvalidOperationException()
    {
        // Arrange — 6MB > 5MB limite
        var largeBytes = new byte[6 * 1024 * 1024];
        var command = new UploadProfilePictureCommand(
            _testUser.Id,
            largeBytes,
            "photo.jpg",
            "image/jpeg",
            largeBytes.Length);

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*5MB*");
    }

    [Fact]
    public async Task Handle_WhenFileTooLarge_ShouldNotCallFileStorage()
    {
        // Arrange
        var largeBytes = new byte[6 * 1024 * 1024];
        var command = new UploadProfilePictureCommand(
            _testUser.Id,
            largeBytes,
            "photo.jpg",
            "image/jpeg",
            largeBytes.Length);

        // Act
        try { await _handler.Handle(command, default); } catch { }

        // Assert
        _fileStorageMock.Verify(
            s => s.SaveAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new UploadProfilePictureCommand(
            Guid.NewGuid(),
            _validImageBytes,
            "photo.jpg",
            "image/jpeg",
            _validImageBytes.Length);

        _userRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((AppUser?)null);

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Usuário não encontrado.");
    }

    [Fact]
    public async Task Handle_WhenUserHasLocalPicture_ShouldDeleteOldPicture()
    {
        // Arrange
        _testUser.ProfilePictureUrl = "/uploads/profile-pictures/old-photo.jpg";

        var command = new UploadProfilePictureCommand(
            _testUser.Id,
            _validImageBytes,
            "photo.jpg",
            "image/jpeg",
            _validImageBytes.Length);

        _userRepoMock
            .Setup(r => r.GetByIdAsync(_testUser.Id, default))
            .ReturnsAsync(_testUser);

        _fileStorageMock
            .Setup(s => s.DeleteAsync(
                "/uploads/profile-pictures/old-photo.jpg", default))
            .Returns(Task.CompletedTask);

        _fileStorageMock
            .Setup(s => s.SaveAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                default))
            .ReturnsAsync("/uploads/profile-pictures/new-photo.jpg");

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, default);

        // Assert
        _fileStorageMock.Verify(
            s => s.DeleteAsync("/uploads/profile-pictures/old-photo.jpg", default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserHasExternalPictureUrl_ShouldNotDeleteOldPicture()
    {
        // Arrange — URL externa (Google OAuth photo, por exemplo)
        _testUser.ProfilePictureUrl = "https://lh3.googleusercontent.com/photo.jpg";

        var command = new UploadProfilePictureCommand(
            _testUser.Id,
            _validImageBytes,
            "photo.jpg",
            "image/jpeg",
            _validImageBytes.Length);

        _userRepoMock
            .Setup(r => r.GetByIdAsync(_testUser.Id, default))
            .ReturnsAsync(_testUser);

        _fileStorageMock
            .Setup(s => s.SaveAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                default))
            .ReturnsAsync("/uploads/profile-pictures/new-photo.jpg");

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, default);

        // Assert — URL externa não é deletada
        _fileStorageMock.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateUserProfilePictureUrl()
    {
        // Arrange
        var command = new UploadProfilePictureCommand(
            _testUser.Id,
            _validImageBytes,
            "photo.jpg",
            "image/jpeg",
            _validImageBytes.Length);

        _userRepoMock
            .Setup(r => r.GetByIdAsync(_testUser.Id, default))
            .ReturnsAsync(_testUser);

        _fileStorageMock
            .Setup(s => s.SaveAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                default))
            .ReturnsAsync("/uploads/profile-pictures/new-photo.jpg");

        AppUser? updatedUser = null;
        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Callback<AppUser, CancellationToken>((u, _) => updatedUser = u)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, default);

        // Assert
        updatedUser.Should().NotBeNull();
        updatedUser!.ProfilePictureUrl.Should().Be("/uploads/profile-pictures/new-photo.jpg");
    }
}