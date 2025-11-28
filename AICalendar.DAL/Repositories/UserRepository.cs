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

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> AddAsync(User entity)
    {
        await _context.Users.AddAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(User entity)
    {
        _context.Users.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
        }
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
