using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Briefings.Commands.GenerateBriefing;
using Orizon.Domain.Entities;

namespace Orizon.Tests.Unit.Application.UseCases.Briefings;

public class GenerateBriefingCommandHandlerTests
{
    private readonly Mock<IJobScheduler> _jobSchedulerMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly GenerateBriefingCommandHandler _handler;

    public GenerateBriefingCommandHandlerTests()
    {
        _handler = new GenerateBriefingCommandHandler(
            _jobSchedulerMock.Object,
            _userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenGoogleConnected_ShouldEnqueueJob()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            GoogleAccessToken = "valid-token",
            TrelloEnabled = false,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _jobSchedulerMock
            .Setup(s => s.EnqueueBriefingGenerationAsync(
                userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("job-123");

        var result = await _handler.Handle(
            new GenerateBriefingCommand(userId.ToString()), default);

        result.JobId.Should().Be("job-123");
        _jobSchedulerMock.Verify(
            s => s.EnqueueBriefingGenerationAsync(
                userId.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTrelloConnected_ShouldEnqueueJob()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            GoogleAccessToken = null,
            TrelloEnabled = true,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _jobSchedulerMock
            .Setup(s => s.EnqueueBriefingGenerationAsync(
                userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("job-456");

        var result = await _handler.Handle(
            new GenerateBriefingCommand(userId.ToString()), default);

        result.JobId.Should().Be("job-456");
    }

    [Fact]
    public async Task Handle_WhenNoIntegration_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            GoogleAccessToken = null,
            TrelloEnabled = false,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        var act = async () => await _handler.Handle(
            new GenerateBriefingCommand(userId.ToString()), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*integração*");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrow()
    {
        var userId = Guid.NewGuid();

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((AppUser?)null);

        var act = async () => await _handler.Handle(
            new GenerateBriefingCommand(userId.ToString()), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Usuário não encontrado.");
    }

    [Fact]
    public async Task Handle_WhenInvalidUserId_ShouldThrow()
    {
        var act = async () => await _handler.Handle(
            new GenerateBriefingCommand("invalid-guid"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Usuário inválido.");
    }

    [Fact]
    public async Task Handle_WhenGoogleConnected_ShouldPassUserIdToScheduler()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            GoogleAccessToken = "valid-token",
            TrelloEnabled = false,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _jobSchedulerMock
            .Setup(s => s.EnqueueBriefingGenerationAsync(
                userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("job-123");

        await _handler.Handle(
            new GenerateBriefingCommand(userId.ToString()), default);

        _jobSchedulerMock.Verify(
            s => s.EnqueueBriefingGenerationAsync(
                userId.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}