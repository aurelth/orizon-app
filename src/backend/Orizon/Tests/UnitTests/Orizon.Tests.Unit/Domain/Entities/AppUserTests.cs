using FluentAssertions;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;

namespace Orizon.Tests.Unit.Domain.Entities;

public class AppUserTests
{
    [Fact]
    public void Constructor_WhenCreated_ShouldHaveDarkThemeByDefault()
    {
        // Arrange & Act
        var user = new AppUser();

        // Assert
        user.ThemePreference.Should().Be(ThemePreference.Dark);
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldHaveTrelloDisabledByDefault()
    {
        // Arrange & Act
        var user = new AppUser();

        // Assert
        user.TrelloEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldHaveTravelModeDisabledByDefault()
    {
        // Arrange & Act
        var user = new AppUser();

        // Assert
        user.IsTraveling.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldHaveDefaultTimezone()
    {
        // Arrange & Act
        var user = new AppUser();

        // Assert
        user.Timezone.Should().Be("America/Sao_Paulo");
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldHaveEmptyBriefingEntries()
    {
        // Arrange & Act
        var user = new AppUser();

        // Assert
        user.BriefingEntries.Should().NotBeNull();
        user.BriefingEntries.Should().BeEmpty();
    }

    [Fact]
    public void IsTraveling_WhenSetToTrue_ShouldAllowTravelLocation()
    {
        // Arrange
        var user = new AppUser();

        // Act
        user.IsTraveling = true;
        user.TravelLocationName = "São Paulo, SP";
        user.TravelLatitude = -23.5505;
        user.TravelLongitude = -46.6333;

        // Assert
        user.IsTraveling.Should().BeTrue();
        user.TravelLocationName.Should().Be("São Paulo, SP");
        user.TravelLatitude.Should().Be(-23.5505);
        user.TravelLongitude.Should().Be(-46.6333);
    }
}