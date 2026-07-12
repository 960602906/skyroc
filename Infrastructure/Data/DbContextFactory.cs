using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

/// <summary>
///     DbContext 设计时工厂 (用于迁移)
/// </summary>
public class DbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    ///     创建 DbContext 实例
    /// </summary>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("__SET_IN_ENV__"))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection must be explicitly configured before running EF Core design-time commands.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
