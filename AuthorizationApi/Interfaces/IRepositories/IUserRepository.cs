using AuthorizationApi.Entities;

namespace AuthorizationApi.Interfaces.IRepositories;

public interface IUserRepository
{
    public Task<User> GetByIdAsync(Guid id);
    public User Insert(User entity);
    public void Delete(User entity);
    public Task SaveChangesAsync();
}