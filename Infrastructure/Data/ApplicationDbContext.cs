using System.Reflection;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
///     应用数据库上下文
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    /// <summary>
    ///     配置模型
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        // ⭐ 应用所有配置
        // modelBuilder.ApplyConfiguration(new UserConfiguration());
        // modelBuilder.ApplyConfiguration(new RoleConfiguration());
        // modelBuilder.ApplyConfiguration(new MenuConfiguration());
        // modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        // modelBuilder.ApplyConfiguration(new RoleMenuConfiguration());

        //添加种子数据
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        //自动更新时间字段
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            switch (entry.State)
            {
                case EntityState.Modified:
                {
                    entry.Entity.UpdateTime = DateTime.UtcNow;
                    break;
                }
                case EntityState.Added:
                    entry.Entity.CreateTime = DateTime.UtcNow;
                    break;
            }

        return base.SaveChangesAsync(cancellationToken);
    }

    #region DbSets

    /// <summary>
    ///     用户表
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    ///     角色表
    /// </summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>
    ///     菜单表
    /// </summary>
    public DbSet<Menu> Menus { get; set; }

    /// <summary>
    ///     用户角色关联表
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; }

    /// <summary>
    ///     角色菜单关联表
    /// </summary>
    public DbSet<RoleMenu> RoleMenus { get; set; }

    #endregion
}