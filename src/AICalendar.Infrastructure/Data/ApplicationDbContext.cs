using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.Aggregates.PredictionAggregate;
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Entities;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Infrastructure.Data.SeedData;
using AICalendar.Infrastructure.Outbox;
using AICalendar.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<Calendar> Calendars => Set<Calendar>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<UserFeedback> UserFeedbacks => Set<UserFeedback>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore Value Objects (they're not entities)
        modelBuilder.Ignore<PredictionId>();
        modelBuilder.Ignore<PredictionItemId>();
        modelBuilder.Ignore<UserId>();
        modelBuilder.Ignore<PredictionCycle>();
        modelBuilder.Ignore<ConfidenceScore>();
        modelBuilder.Ignore<CalendarId>();
        modelBuilder.Ignore<CalendarItemId>();

        // Apply configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new PredictionConfiguration());
        modelBuilder.ApplyConfiguration(new CalendarConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxConfiguration());
        modelBuilder.ApplyConfiguration(new UserFeedbackConfiguration());


        // Seed data
        UserTransactionSeedData.SeedUserAndTransactionData(modelBuilder);
    }
}
