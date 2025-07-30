using Domain.Entities;
using Domain.Interfaces.IRepositories;
using Infrastructure.DbContexts;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthorizationApiDbContext _context;

    public UserRepository(AuthorizationApiDbContext context)
    {
        _context = context;
    }

    public void Delete(User entity)
    {
        _context.Users.Remove(entity);
        _context.SaveChanges();
    }

    public User GetById(Guid id)
    {
        return _context.Users.FirstOrDefault(m => m.Id == id);
    }

    public User Insert(User entity)
    {
        _context.Users.Add(entity);
        _context.SaveChanges();
        return entity;
    }
}
