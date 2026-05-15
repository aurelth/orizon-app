using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Users.Commands.CompleteOnboarding;
using Orizon.Domain.Entities;

namespace Orizon.Tests.Unit.Application.UseCases.Users;

public class CompleteOnboardingCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly CompleteOnboardingCommandHandler _handler;

    public CompleteOnboardingCommandHandlerTests()
    {
        _handler = new CompleteOnboardingCommandHandler(_userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldSetHasCompletedOnboardingTrue()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            HasCompletedOnboarding = false,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CompleteOnboardingCommand(userId), default);

        user.HasCompletedOnboarding.Should().BeTrue();
        _userRepoMock.Verify(
            r => r.UpdateAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldNotCallUpdate()
    {
        var userId = Guid.NewGuid();

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((AppUser?)null);

        await _handler.Handle(new CompleteOnboardingCommand(userId), default);

        _userRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<AppUser>(), default),
            Times.Never);
    }
}