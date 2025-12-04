using AICalendar.Domain.Entities;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Interfaces;

/// <summary>
/// Repository interface for User aggregate
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
}
