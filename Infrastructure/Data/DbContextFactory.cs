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
            "Host=ep-lucky-silence-anqhgkr0-pooler.c-6.us-east-1.aws.neon.tech; Database=neondb; Username=neondb_owner; Password=npg_hv6YNATa1pbF; SSL Mode=Require;Trust Server Certificate=true;";

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                "未找到连接字符串 'DefaultConnection'。请确保 appsettings.json 中包含此配置。");

        // 配置 DbContext
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(connectionString).EnableSensitiveDataLogging();

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}