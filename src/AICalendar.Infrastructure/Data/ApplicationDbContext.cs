using AICalendar.Domain.Entities;
using AICalendar.Infrastructure.Data.SeedData;
using AICalendar.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        // Seed data
        UserTransactionSeedData.SeedUserAndTransactionData(modelBuilder);
    }
}
