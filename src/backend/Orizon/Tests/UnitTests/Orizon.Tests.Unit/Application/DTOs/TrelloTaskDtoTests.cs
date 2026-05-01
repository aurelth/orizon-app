using FluentAssertions;
using Orizon.Application.DTOs.Trello;

namespace Orizon.Tests.Unit.Application.DTOs;

public class TrelloTaskDtoTests
{
    [Fact]
    public void IsStuck_WhenDaysInProgressIsNull_ShouldReturnFalse()
    {
        // Arrange
        var task = new TrelloTaskDto
        {
            DaysInProgress = null
        };

        // Assert
        task.IsStuck.Should().BeFalse();
    }

    [Fact]
    public void IsStuck_WhenDaysInProgressIsOne_ShouldReturnFalse()
    {
        // Arrange
        var task = new TrelloTaskDto
        {
            DaysInProgress = 1
        };

        // Assert
        // 1 dia em progresso não é considerado travado
        task.IsStuck.Should().BeFalse();
    }

    [Fact]
    public void IsStuck_WhenDaysInProgressIsMoreThanOne_ShouldReturnTrue()
    {
        // Arrange
        var task = new TrelloTaskDto
        {
            DaysInProgress = 2
        };

        // Assert
        task.IsStuck.Should().BeTrue();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    public void IsStuck_WhenDaysInProgressIsGreaterThanOne_ShouldAlwaysReturnTrue(int days)
    {
        // Arrange
        var task = new TrelloTaskDto
        {
            DaysInProgress = days
        };

        // Assert
        task.IsStuck.Should().BeTrue();
    }
}