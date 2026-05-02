using FluentAssertions;
using Orizon.Domain.Common;

namespace Orizon.Tests.Unit.Domain.Entities;

// Não podemos instanciar BaseEntity diretamente porque é abstract, entãoriamos uma classe concreta apenas para testes.
public class TestEntity : BaseEntity { }

public class BaseEntityTests
{
    [Fact]
    public void Constructor_WhenCreated_ShouldGenerateNewGuid()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert        
        entity.Id.Should().NotBeEmpty();
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldSetCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var entity = new TestEntity();

        // Assert
        var after = DateTime.UtcNow;
        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldSetUpdatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var entity = new TestEntity();

        // Assert
        var after = DateTime.UtcNow;
        entity.UpdatedAt.Should().BeOnOrAfter(before);
        entity.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_WhenTwoEntitiesCreated_ShouldHaveDifferentIds()
    {
        // Arrange & Act
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Assert
        entity1.Id.Should().NotBe(entity2.Id);
    }
}