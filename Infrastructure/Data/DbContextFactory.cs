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
        const string connectionString = "Host=ep-still-firefly-aotz0lef-pooler.c-2.ap-southeast-1.aws.neon.tech; Database=neondb; Username=neondb_owner; Password=npg_aYwABE7F4HxP; SSL Mode=Require; Trust Server Certificate=true;";

        // 配置 DbContext
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(connectionString).EnableSensitiveDataLogging();

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}