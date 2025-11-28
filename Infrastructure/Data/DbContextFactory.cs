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
        // 获取连接字符串
        var connectionString =
            "Host=localhost;Port=5432;Database=sky_roc;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=20;";

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                "未找到连接字符串 'DefaultConnection'。请确保 appsettings.json 中包含此配置。");

        // 配置 DbContext
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(connectionString).EnableSensitiveDataLogging();

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}