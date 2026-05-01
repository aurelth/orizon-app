using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Domain.Entities;

namespace Orizon.Infrastructure.Data.Configurations;

public class BriefingEntryConfiguration : IEntityTypeConfiguration<BriefingEntry>
{
    public void Configure(EntityTypeBuilder<BriefingEntry> builder)
    {
        builder.ToTable("briefing_entries");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .IsRequired();

        builder.Property(b => b.UserId)
            .IsRequired();

        builder.Property(b => b.Date)
            .IsRequired();

        builder.Property(b => b.EmailSummaryJson)
            .HasColumnType("jsonb");

        builder.Property(b => b.CalendarEventsJson)
            .HasColumnType("jsonb");

        builder.Property(b => b.TrelloTasksJson)
            .HasColumnType("jsonb");

        builder.Property(b => b.WeatherJson)
            .HasColumnType("jsonb");

        builder.Property(b => b.AISummary)
            .HasColumnType("text");

        builder.Property(b => b.AISuggestions)
            .HasColumnType("text");

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>();  // guarda "Pending", "Generated", "Failed"

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();
        
        builder.HasIndex(b => new { b.UserId, b.Date })
            .IsUnique();
    }
}