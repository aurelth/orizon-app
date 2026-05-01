using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Domain.Entities;

namespace Orizon.Infrastructure.Data.Configurations;

public class TrelloBoardConfigConfiguration : IEntityTypeConfiguration<TrelloBoardConfig>
{
    public void Configure(EntityTypeBuilder<TrelloBoardConfig> builder)
    {
        builder.ToTable("trello_board_configs");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.BoardId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.BoardName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.BoardColor)
            .HasMaxLength(50);

        builder.Property(t => t.TodayListId)
            .HasMaxLength(100);

        builder.Property(t => t.InProgressListId)
            .HasMaxLength(100);

        builder.Property(t => t.TodayListName)
            .HasMaxLength(200);

        builder.Property(t => t.InProgressListName)
            .HasMaxLength(200);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Índice para buscar configs de um usuário rapidamente
        builder.HasIndex(t => new { t.UserId, t.BoardId })
            .IsUnique();
    }
}