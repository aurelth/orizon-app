using FluentAssertions;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;

namespace Orizon.Tests.Unit.Domain.Entities;

public class BriefingEntryTests
{
    [Fact]
    public void Constructor_WhenCreated_ShouldHavePendingStatus()
    {
        // Arrange & Act
        var briefing = new BriefingEntry();

        // Assert
        briefing.Status.Should().Be(BriefingStatus.Pending);
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldHaveNullGeneratedAt()
    {
        // Arrange & Act
        var briefing = new BriefingEntry();

        // Assert
        briefing.GeneratedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldHaveNullEmailSentAt()
    {
        // Arrange & Act
        var briefing = new BriefingEntry();

        // Assert
        briefing.EmailSentAt.Should().BeNull();
    }

    [Fact]
    public void Status_WhenSetToGenerated_ShouldBeGenerated()
    {
        // Arrange
        var briefing = new BriefingEntry();

        // Act
        briefing.Status = BriefingStatus.Generated;
        briefing.GeneratedAt = DateTime.UtcNow;

        // Assert
        briefing.Status.Should().Be(BriefingStatus.Generated);
        briefing.GeneratedAt.Should().NotBeNull();
    }

    [Fact]
    public void Status_WhenSetToFailed_ShouldAllowErrorMessage()
    {
        // Arrange
        var briefing = new BriefingEntry();

        // Act
        briefing.Status = BriefingStatus.Failed;
        briefing.ErrorMessage = "Gmail API timeout";

        // Assert
        briefing.Status.Should().Be(BriefingStatus.Failed);
        briefing.ErrorMessage.Should().Be("Gmail API timeout");
    }
}