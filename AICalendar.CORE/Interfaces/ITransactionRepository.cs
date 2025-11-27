using AICalendar.CORE.Entities;

namespace AICalendar.CORE.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetTransactionsByUserIdAsync(Guid userId);
    Task<IEnumerable<Transaction>> GetTransactionsByTypeAsync(Guid userId, TransactionType type);
    Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
}
