using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Orizon.Infrastructure.Services.External;
using Xunit;

namespace Orizon.Tests.Integration.Integrations;

public class TrelloServiceTests
{
    private readonly TrelloService _service;
    private readonly string? _apiKey;
    private readonly string? _token;

    public TrelloServiceTests()
    {
        var httpClient = new HttpClient();
        _service = new TrelloService(
            httpClient,
            NullLogger<TrelloService>.Instance);

        // Lê as credenciais das variáveis de ambiente
        // No CI essas variáveis não existem — os testes são pulados
        _apiKey = Environment.GetEnvironmentVariable("TRELLO_API_KEY");
        _token = Environment.GetEnvironmentVariable("TRELLO_TOKEN");
    }

    [Fact]
    public async Task GetBoardsAsync_WhenValidCredentials_ShouldReturnBoards()
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_token))
        {
            // Pula o teste no CI onde as credenciais não estão disponíveis
            return;
        }

        var result = await _service.GetBoardsAsync(_apiKey, _token);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(board =>
        {
            board.BoardId.Should().NotBeNullOrEmpty();
            board.Name.Should().NotBeNullOrEmpty();
            board.Lists.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task GetBoardsAsync_WhenInvalidCredentials_ShouldThrowException()
    {
        var act = async () => await _service.GetBoardsAsync(
            "invalid-key", "invalid-token");

        await act.Should().ThrowAsync<Exception>();
    }
}