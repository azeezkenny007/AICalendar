using AICalendar.Domain.Entities;
using AICalendar.Domain.Interfaces;
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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxConfiguration());

        // Seed data
        UserTransactionSeedData.SeedUserAndTransactionData(modelBuilder);
    }
}
