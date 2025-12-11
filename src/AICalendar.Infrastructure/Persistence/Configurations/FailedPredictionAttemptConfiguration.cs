using AICalendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICalendar.Infrastructure.Persistence.Configurations;

public class FailedPredictionAttemptConfiguration : IEntityTypeConfiguration<FailedPredictionAttempt>
{
    public void Configure(EntityTypeBuilder<FailedPredictionAttempt> builder)
    {
        builder.ToTable("FailedPredictionAttempts");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasConversion(
                id => id.Value,
                value => new FailedPredictionAttemptId(value))
            .IsRequired();

        builder.Property(f => f.UserId)
            .HasConversion(
                id => id.Value,
                value => new Domain.ValueObjects.UserId(value))
            .IsRequired();

        builder.Property(f => f.TargetMonth)
            .HasMaxLength(7) // Format: "YYYY-MM"
            .IsRequired();

        builder.Property(f => f.ErrorMessage)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(f => f.FailedAt)
            .IsRequired();

        builder.Property(f => f.RetryCount)
            .IsRequired();

        builder.Property(f => f.LastRetryAt);

        builder.Property(f => f.SucceededAt);

        builder.Property(f => f.IsResolved)
            .IsRequired();

        // Index for querying unresolved attempts
        builder.HasIndex(f => f.IsResolved);
        builder.HasIndex(f => f.FailedAt);
    }
}
