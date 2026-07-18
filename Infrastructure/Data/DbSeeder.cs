using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;

namespace Infrastructure.Data;

/// <summary>
///     开发环境最小种子：仅初始化管理员账户与系统角色。
/// </summary>
public static class DbSeeder
{
    /// <summary>
    ///     执行数据库迁移，并在开发种子开启时写入管理员与角色。
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IHostEnvironment environment,
        IOptions<DevSeedOptions> devSeedOptions)
    {
        await context.Database.MigrateAsync();
        await SeedRoles(context);
        await SeedAdminUser(context, environment, devSeedOptions.Value);
        await SeedAdminUserRole(context, devSeedOptions.Value);
    }

    /// <summary>
    ///     写入系统预置角色（管理员、普通用户）。
    /// </summary>
    private static async Task SeedRoles(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync()) return;

        context.Roles.AddRange(
            new Role
            {
                Name = "管理员",
                Code = SeedConstants.AdminRoleCode,
                Desc = "系统管理员，拥有所有权限"
            },
            new Role
            {
                Name = "用户",
                Code = SeedConstants.UserRoleCode,
                Desc = "普通用户，拥有基本权限"
            });

        await context.SaveChangesAsync();
    }

    /// <summary>
    ///     写入唯一的管理员账户；非开发环境或未启用种子时跳过。
    /// </summary>
    private static async Task SeedAdminUser(
        ApplicationDbContext context,
        IHostEnvironment environment,
        DevSeedOptions devSeedOptions)
    {
        if (await context.Users.AnyAsync()) return;
        if (!environment.IsDevelopment() || !devSeedOptions.Enabled) return;
        if (string.IsNullOrWhiteSpace(devSeedOptions.AdminPassword))
            throw new InvalidOperationException("DevSeed is enabled but AdminPassword is missing.");

        context.Users.Add(new User
        {
            Username = devSeedOptions.AdminUsername,
            NickName = "系统管理员",
            Email = "960602906@qq.com",
            Gender = GenderType.Male,
            PasswordHash = PasswordHasher.Hash(devSeedOptions.AdminPassword)
        });

        await context.SaveChangesAsync();
    }

    /// <summary>
    ///     将管理员账户绑定到管理员角色。
    /// </summary>
    private static async Task SeedAdminUserRole(ApplicationDbContext context, DevSeedOptions devSeedOptions)
    {
        if (await context.UserRoles.AnyAsync()) return;

        var adminUser = await context.Users.FirstOrDefaultAsync(x => x.Username == devSeedOptions.AdminUsername);
        var adminRole = await context.Roles.FirstOrDefaultAsync(x => x.Code == SeedConstants.AdminRoleCode);
        if (adminUser is null || adminRole is null) return;

        context.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        await context.SaveChangesAsync();
    }
}
