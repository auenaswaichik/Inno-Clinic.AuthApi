using AuthApi.Entities;
using AuthApi.DbContexts.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.DbContexts;

public class AuthApiDbContext : DbContext
{
    public AuthApiDbContext(DbContextOptions<AuthApiDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }

}