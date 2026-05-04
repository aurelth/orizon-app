using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Integrations.Google.Query;

namespace Orizon.Tests.Unit.Application.UseCases.Integrations.Google;

public class GetGoogleAuthUrlQueryHandlerTests
{
    private readonly Mock<IGoogleOAuthService> _googleOAuthServiceMock;
    private readonly GetGoogleAuthUrlQueryHandler _handler;

    public GetGoogleAuthUrlQueryHandlerTests()
    {
        _googleOAuthServiceMock = new Mock<IGoogleOAuthService>();
        _handler = new GetGoogleAuthUrlQueryHandler(_googleOAuthServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldReturnAuthorizationUrl()
    {
        var query = new GetGoogleAuthUrlQuery("user-123", "state-abc");
        var expectedUrl = "https://accounts.google.com/o/oauth2/v2/auth?client_id=...";

        _googleOAuthServiceMock
            .Setup(s => s.GetAuthorizationUrl(query.UserId, query.State))
            .Returns(expectedUrl);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().Be(expectedUrl);
        _googleOAuthServiceMock.Verify(
            s => s.GetAuthorizationUrl(query.UserId, query.State),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldPassCorrectParameters()
    {
        var userId = "user-456";
        var state = "state-xyz";
        var query = new GetGoogleAuthUrlQuery(userId, state);

        _googleOAuthServiceMock
            .Setup(s => s.GetAuthorizationUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://accounts.google.com");

        await _handler.Handle(query, CancellationToken.None);

        _googleOAuthServiceMock.Verify(
            s => s.GetAuthorizationUrl(userId, state),
            Times.Once);
    }
}