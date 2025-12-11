using AICalendar.Domain.Aggregates.PredictionAggregate;
using AICalendar.Domain.Entities;
using AICalendar.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICalendar.Infrastructure.Persistence.Configurations;

public class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.ToTable("Predictions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                value => PredictionId.Create(value))
            .HasColumnName("PredictionId");

        builder.Property(p => p.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value))
            .IsRequired();

        builder.OwnsOne(p => p.Cycle, cycle =>
        {
            cycle.Property(c => c.StartDate).HasColumnName("CycleStartDate");
            cycle.Property(c => c.EndDate).HasColumnName("CycleEndDate");
        });

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        // Add performance indexes
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.Status);

        // Configure owned collection of PredictionItems using backing field
        builder.OwnsMany(p => p.Items, item =>
        {
            item.ToTable("PredictionItems");

            item.WithOwner().HasForeignKey("PredictionId");

            item.HasKey("Id");

            item.Property(i => i.Id)
                .HasConversion(
                    id => id.Value,
                    value => PredictionItemId.Create(value))
                .HasColumnName("PredictionItemId");

            item.Property(i => i.TransactionId).IsRequired();
            item.Property(i => i.Merchant).HasMaxLength(200).IsRequired();
            item.Property(i => i.Amount).HasPrecision(18, 2).IsRequired();
            item.Property(i => i.DueDate).IsRequired();
            item.Property(i => i.Explanation).HasMaxLength(500);

            item.OwnsOne(i => i.Confidence, confidence =>
            {
                confidence.Property(c => c.Value).HasColumnName("ConfidenceValue");
                confidence.Property(c => c.RuleConfidence).HasColumnName("RuleConfidence");
                confidence.Property(c => c.MlConfidence).HasColumnName("MlConfidence");
            });

            item.Property(i => i.Pattern)
                .HasConversion<string>()
                .IsRequired();

            item.Property(i => i.IsAccepted);

            item.Property(i => i.Account).HasMaxLength(100);
            item.Property(i => i.AccountName).HasMaxLength(200);
            item.Property(i => i.Description).HasMaxLength(500);

            item.Property(i => i.IsEdited);
            item.Property(i => i.OriginalAmount).HasPrecision(18, 2);
            item.Property(i => i.OriginalDueDate);

            // Add indexes for PredictionItems
            item.HasIndex("PredictionId");
            item.HasIndex(i => i.DueDate);
            item.HasIndex(i => i.IsAccepted);
        });

        // Use backing field for Items collection
        builder.Metadata.FindNavigation(nameof(Prediction.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Ignore domain events (not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}
