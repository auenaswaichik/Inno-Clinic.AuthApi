using Domain.Entities;
using Infrastructure.DbContexts.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DbContexts;

public class AuthorizationApiDbContext : DbContext
{
    public AuthorizationApiDbContext(DbContextOptions<AuthorizationApiDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }

}