using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Users.Queries.GetUserProfile;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;

namespace Orizon.Tests.Unit.Application.UseCases.Users;

public class GetUserProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly GetUserProfileQueryHandler _handler;

    public GetUserProfileQueryHandlerTests()
    {
        _handler = new GetUserProfileQueryHandler(_userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldReturnUserProfileDto()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Email = "aurel@orizonapp.io",
            DisplayName = "Aurel",
            LocationName = "Blumenau",
            Latitude = -26.9194,
            Longitude = -49.0661,
            Timezone = "America/Sao_Paulo",
            IsTraveling = false,
            ThemePreference = ThemePreference.Dark,
            GoogleAccessToken = "token-123",
            TrelloEnabled = true,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        var result = await _handler.Handle(
            new GetUserProfileQuery(userId), default);

        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("aurel@orizonapp.io");
        result.DisplayName.Should().Be("Aurel");
        result.LocationName.Should().Be("Blumenau");
        result.Timezone.Should().Be("America/Sao_Paulo");
        result.IsTraveling.Should().BeFalse();
        result.ThemePreference.Should().Be("Dark");
        result.GoogleConnected.Should().BeTrue();
        result.TrelloEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNull()
    {
        var userId = Guid.NewGuid();

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((AppUser?)null);

        var result = await _handler.Handle(
            new GetUserProfileQuery(userId), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenGoogleNotConnected_ShouldReturnGoogleConnectedFalse()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Email = "aurel@orizonapp.io",
            DisplayName = "Aurel",
            GoogleAccessToken = null,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        var result = await _handler.Handle(
            new GetUserProfileQuery(userId), default);

        result!.GoogleConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenThemeIsLight_ShouldReturnThemeLight()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Email = "aurel@orizonapp.io",
            DisplayName = "Aurel",
            ThemePreference = ThemePreference.Light,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        var result = await _handler.Handle(
            new GetUserProfileQuery(userId), default);

        result!.ThemePreference.Should().Be("Light");
    }
}