using AuthorizationApi.Entities;
using AuthorizationApi.Interfaces.IRepositories;
using AuthorizationApi.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AuthorizationApi.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthApiDbContext _context;

    public UserRepository(AuthApiDbContext context)
    {
        _context = context;
    }

    public void Delete(User entity)
    {
        _context.Users.Remove(entity);
    }

    public async Task<User> GetByIdAsync(Guid id)
    {
        return await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
    }

    public User Insert(User entity)
    {
        _context.Users.Add(entity);
        return entity;
    }
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
