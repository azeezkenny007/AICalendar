using AICalendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Domain.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Transaction> Transactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
