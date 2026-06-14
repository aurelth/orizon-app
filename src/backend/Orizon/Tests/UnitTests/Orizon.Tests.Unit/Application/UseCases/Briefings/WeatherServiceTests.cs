using FluentAssertions;
using Orizon.Infrastructure.Services.External;

namespace Orizon.Tests.Unit.Infrastructure.Services;

public class WeatherServiceTests
{
    [Theory]
    [InlineData("Santa Catarina", "SC")]
    [InlineData("São Paulo", "SP")]
    [InlineData("Rio de Janeiro", "RJ")]
    [InlineData("Minas Gerais", "MG")]
    [InlineData("Paraná", "PR")]
    [InlineData("Rio Grande do Sul", "RS")]
    [InlineData("Bahia", "BA")]
    [InlineData("Distrito Federal", "DF")]
    [InlineData(null, null)]
    [InlineData("Unknown State", null)]
    public void GetBrazilianStateAbbreviation_ShouldReturnCorrectAbbreviation(
        string? stateName, string? expected)
    {
        var result = WeatherService.GetBrazilianStateAbbreviation(stateName);
        result.Should().Be(expected);
    }

    [Fact]
    public void GetBrazilianStateAbbreviation_ShouldCoverAll27BrazilianStates()
    {
        var states = new Dictionary<string, string>
        {
            { "Acre", "AC" },
            { "Alagoas", "AL" },
            { "Amapá", "AP" },
            { "Amazonas", "AM" },
            { "Bahia", "BA" },
            { "Ceará", "CE" },
            { "Distrito Federal", "DF" },
            { "Espírito Santo", "ES" },
            { "Goiás", "GO" },
            { "Maranhão", "MA" },
            { "Mato Grosso", "MT" },
            { "Mato Grosso do Sul", "MS" },
            { "Minas Gerais", "MG" },
            { "Pará", "PA" },
            { "Paraíba", "PB" },
            { "Paraná", "PR" },
            { "Pernambuco", "PE" },
            { "Piauí", "PI" },
            { "Rio de Janeiro", "RJ" },
            { "Rio Grande do Norte", "RN" },
            { "Rio Grande do Sul", "RS" },
            { "Rondônia", "RO" },
            { "Roraima", "RR" },
            { "Santa Catarina", "SC" },
            { "São Paulo", "SP" },
            { "Sergipe", "SE" },
            { "Tocantins", "TO" },
        };

        foreach (var (state, expected) in states)
        {
            var result = WeatherService.GetBrazilianStateAbbreviation(state);
            result.Should().Be(expected,
                $"estado '{state}' deveria retornar '{expected}'");
        }

        states.Should().HaveCount(27);
    }
}