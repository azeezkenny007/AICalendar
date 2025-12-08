using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICalendar.Infrastructure.Persistence.Configurations;

public class UserFeedbackConfiguration : IEntityTypeConfiguration<UserFeedback>
{
    public void Configure(EntityTypeBuilder<UserFeedback> builder)
    {
        builder.ToTable("UserFeedbacks");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasConversion(
                id => id.Value,
                value => UserFeedbackId.Create(value))
            .ValueGeneratedNever();

        builder.Property(f => f.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value))
            .IsRequired();

        builder.Property(f => f.PredictionId)
            .HasConversion(
                id => id.Value,
                value => PredictionId.Create(value))
            .IsRequired();

        builder.Property(f => f.PredictionItemId)
            .HasConversion(
                id => id.Value,
                value => PredictionItemId.Create(value))
            .IsRequired();

        builder.Property(f => f.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(f => f.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(f => f.Merchant)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(f => f.DueDate)
            .IsRequired();

        builder.Property(f => f.WasEdited)
            .IsRequired();

        builder.Property(f => f.OriginalAmount)
            .HasPrecision(18, 2);

        builder.Property(f => f.OriginalDueDate);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.SentToAI)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(f => f.SentToAIAt);

        // Indexes for common queries
        builder.HasIndex(f => f.UserId);
        builder.HasIndex(f => f.PredictionId);
        builder.HasIndex(f => f.Type);
        builder.HasIndex(f => f.CreatedAt);

        // Ignore domain events
        builder.Ignore(f => f.DomainEvents);
    }
}