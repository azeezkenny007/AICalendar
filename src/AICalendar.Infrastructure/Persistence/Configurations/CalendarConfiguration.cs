using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICalendar.Infrastructure.Persistence.Configurations;

public class CalendarConfiguration : IEntityTypeConfiguration<Calendar>
{
    public void Configure(EntityTypeBuilder<Calendar> builder)
    {
        builder.ToTable("Calendars");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => CalendarId.Create(value))
            .ValueGeneratedNever();

        builder.Property(c => c.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value))
            .IsRequired();

        builder.HasIndex(c => c.UserId)
            .IsUnique();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        // Add performance indexes
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.UpdatedAt);

        // Owned collection for CalendarItems
        builder.OwnsMany(c => c.Items, items =>
        {
            items.ToTable("CalendarItems");

            items.WithOwner().HasForeignKey("CalendarId");

            items.HasKey("Id");

            items.Property(i => i.Id)
                .HasConversion(
                    id => id.Value,
                    value => CalendarItemId.Create(value))
                .ValueGeneratedNever();

            items.Property(i => i.PredictionItemId)
                .HasConversion(
                    id => id.Value,
                    value => PredictionItemId.Create(value))
                .IsRequired();

            items.Property(i => i.Merchant)
                .HasMaxLength(200)
                .IsRequired();

            items.Property(i => i.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            items.Property(i => i.DueDate)
                .IsRequired();

            items.Property(i => i.Account)
                .HasMaxLength(100);

            items.Property(i => i.AccountName)
                .HasMaxLength(200);

            items.Property(i => i.Description)
                .HasMaxLength(500);

            items.Property(i => i.IsPaid)
                .IsRequired();

            items.Property(i => i.PaidDate);

            items.Property(i => i.CreatedAt)
                .IsRequired();

            items.HasIndex(i => i.PredictionItemId);
            items.HasIndex(i => i.DueDate);
            items.HasIndex(i => i.IsPaid);
            items.HasIndex("CalendarId");
        });

        // Ignore domain events (handled by base class)
        builder.Ignore(c => c.DomainEvents);
    }
}
