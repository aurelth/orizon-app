using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Services.External;

namespace Orizon.Tests.Unit.Application.UseCases.Integrations.Trello;

public class TrelloServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITrelloBoardConfigRepository> _boardConfigRepoMock = new();

    private TrelloService CreateService(HttpClient? httpClient = null)
    {
        return new TrelloService(
            httpClient ?? new HttpClient(),
            _userRepoMock.Object,
            _boardConfigRepoMock.Object,
            NullLogger<TrelloService>.Instance);
    }

    [Fact]
    public async Task GetActiveTasksAsync_WhenInvalidUserId_ShouldReturnEmpty()
    {
        var service = CreateService();

        var result = await service.GetActiveTasksAsync("not-a-guid");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveTasksAsync_WhenUserNotFound_ShouldReturnEmpty()
    {
        _userRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        var service = CreateService();

        var result = await service.GetActiveTasksAsync(Guid.NewGuid().ToString());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveTasksAsync_WhenTrelloDisabled_ShouldReturnEmpty()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TrelloEnabled = false,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();

        var result = await service.GetActiveTasksAsync(user.Id.ToString());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveTasksAsync_WhenNoCredentials_ShouldReturnEmpty()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TrelloEnabled = true,
            TrelloApiKey = null,
            TrelloToken = null,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();

        var result = await service.GetActiveTasksAsync(user.Id.ToString());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveTasksAsync_WhenNoBoardsConfigured_ShouldReturnEmpty()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TrelloEnabled = true,
            TrelloApiKey = "apikey",
            TrelloToken = "token",
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _boardConfigRepoMock
            .Setup(r => r.GetByUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrelloBoardConfig>());

        var service = CreateService();

        var result = await service.GetActiveTasksAsync(user.Id.ToString());

        result.Should().BeEmpty();
    }
}