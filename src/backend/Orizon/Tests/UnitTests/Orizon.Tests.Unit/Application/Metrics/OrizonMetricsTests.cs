using FluentAssertions;
using Orizon.API.Metrics;
using Orizon.Application.Interfaces.Services;
using Xunit;

namespace Orizon.Tests.Unit.Application.Metrics;

public class OrizonMetricsTests
{
    private readonly OrizonMetrics _metrics;

    public OrizonMetricsTests()
    {
        _metrics = new OrizonMetrics();
    }

    [Fact]
    public void RecordBriefingGenerated_ShouldNotThrow()
    {
        var act = () => _metrics.RecordBriefingGenerated();
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordBriefingFailed_ShouldNotThrow()
    {
        var act = () => _metrics.RecordBriefingFailed();
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEmailSent_ShouldNotThrow()
    {
        var act = () => _metrics.RecordEmailSent();
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEmailFailed_ShouldNotThrow()
    {
        var act = () => _metrics.RecordEmailFailed();
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordGoogleConnected_ShouldNotThrow()
    {
        var act = () => _metrics.RecordGoogleConnected();
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordTrelloConnected_ShouldNotThrow()
    {
        var act = () => _metrics.RecordTrelloConnected();
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordBriefingDuration_ShouldNotThrow()
    {
        var act = () => _metrics.RecordBriefingDuration(12.5);
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var act = () => _metrics.Dispose();
        act.Should().NotThrow();
    }
}