using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Users.Commands.UpdateUserProfile;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;

namespace Orizon.Tests.Unit.Application.UseCases.Users;

public class UpdateUserProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly UpdateUserProfileCommandHandler _handler;

    public UpdateUserProfileCommandHandlerTests()
    {
        _handler = new UpdateUserProfileCommandHandler(_userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldUpdateDisplayName()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Email = "aurel@orizonapp.io",
            DisplayName = "Nome Antigo",
            ThemePreference = ThemePreference.Dark,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new UpdateUserProfileCommand(
            userId, "Nome Novo", null, null), default);

        user.DisplayName.Should().Be("Nome Novo");
        _userRepoMock.Verify(
            r => r.UpdateAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldUpdateThemePreference()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Email = "aurel@orizonapp.io",
            DisplayName = "Aurel",
            ThemePreference = ThemePreference.Dark,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new UpdateUserProfileCommand(
            userId, "Aurel", null, "Light"), default);

        user.ThemePreference.Should().Be(ThemePreference.Light);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldUpdateProfilePictureUrl()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Email = "aurel@orizonapp.io",
            DisplayName = "Aurel",
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new UpdateUserProfileCommand(
            userId, "Aurel", "https://example.com/photo.jpg", null), default);

        user.ProfilePictureUrl.Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldNotCallUpdate()
    {
        var userId = Guid.NewGuid();

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((AppUser?)null);

        await _handler.Handle(new UpdateUserProfileCommand(
            userId, "Aurel", null, null), default);

        _userRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<AppUser>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvalidTheme_ShouldNotChangeTheme()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Email = "aurel@orizonapp.io",
            DisplayName = "Aurel",
            ThemePreference = ThemePreference.Dark,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new UpdateUserProfileCommand(
            userId, "Aurel", null, "InvalidTheme"), default);

        user.ThemePreference.Should().Be(ThemePreference.Dark);
    }
}