using AICalendar.CORE.Entities;
using AICalendar.CORE.Interfaces;
using AICalendar.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AICalendarDbContext _context;

    public UserRepository(AICalendarDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetUserWithTransactionsAsync(Guid userId)
    {
        return await _context.Users
            .Include(u => u.Transactions)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
