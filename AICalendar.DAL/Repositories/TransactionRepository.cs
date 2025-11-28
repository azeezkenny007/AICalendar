using AICalendar.CORE.Entities;
using AICalendar.CORE.Interfaces;
using AICalendar.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.DAL.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AICalendarDbContext _context;

    public TransactionRepository(AICalendarDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync()
    {
        return await _context.Transactions.ToListAsync();
    }

    public async Task<Transaction> AddAsync(Transaction entity)
    {
        await _context.Transactions.AddAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(Transaction entity)
    {
        _context.Transactions.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var transaction = await GetByIdAsync(id);
        if (transaction != null)
        {
            _context.Transactions.Remove(transaction);
        }
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByUserIdAsync(Guid userId)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByTypeAsync(Guid userId, TransactionType type)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId && t.Type == type)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
}
