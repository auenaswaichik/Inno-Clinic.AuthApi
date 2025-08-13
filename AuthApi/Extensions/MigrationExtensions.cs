using AuthApi.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Extensions;

public static class MigrationExtensions
{
    public static void EnsureDatabaseMigration(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthApiDbContext>();
        db.Database.Migrate();
    }
}
