using AICalendar.CORE.Entities;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.DAL.Data;

public class AICalendarDbContext : DbContext
{
    public AICalendarDbContext(DbContextOptions<AICalendarDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations including seed data from separate files
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AICalendarDbContext).Assembly);
    }
}
