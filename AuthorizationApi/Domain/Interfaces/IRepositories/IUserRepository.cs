using System.Linq.Expressions;
using Domain.Entities;

namespace Domain.Interfaces.IRepositories;

public interface IUserRepository
{
    public User GetById(Guid id);
    public User Insert(User entity);
    public void Delete(User entity);
}