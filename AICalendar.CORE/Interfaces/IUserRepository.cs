using AICalendar.CORE.Entities;

namespace AICalendar.CORE.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetUserWithTransactionsAsync(Guid userId);
}
