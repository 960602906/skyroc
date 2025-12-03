using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
///     Menu 实体映射配置
/// </summary>
public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        // 表名
        builder.ToTable("sys_menu", "public");

        // 主键
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()") // PostgreSQL
            .ValueGeneratedOnAdd();

        // 属性配置
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.Path)
            .HasColumnName("path")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(x => x.MenuType)
            .HasColumnName("menu_type")
            .HasColumnType("integer");

        builder.Property(x => x.Layout)
            .HasColumnName("layout")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.Redirect)
            .HasColumnName("redirect")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.Component)
            .HasColumnName("component")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(x => x.I18NKey)
            .HasColumnName("i18nKey")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.Icon)
            .HasColumnName("icon")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.LocalIcon)
            .HasColumnName("local_icon")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.IconType)
            .HasColumnName("icon_type");
        // .HasColumnType("integer");

        builder.Property(x => x.Order)
            .HasColumnName("order")
            .HasColumnType("integer");


        builder.Property(x => x.KeepAlive)
            .HasColumnName("keep_alive")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Constant)
            .HasColumnName("constant")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Href)
            .HasColumnName("href")
            .HasColumnType("varchar(200)");

        builder.Property(x => x.HideInMenu)
            .HasColumnName("hide_in_menu")
            .HasColumnType("boolean");

        builder.Property(x => x.ActiveMenu)
            .HasColumnName("active_menu")
            .HasColumnType("varchar(200)");

        builder.Property(x => x.MultiTab)
            .HasColumnName("multi_tab")
            .HasColumnType("boolean");

        builder.Property(x => x.FixedIndexInTab)
            .HasColumnName("fixed_index_in_tab")
            .HasColumnType("integer");

        builder.Property(x => x.ParentId)
            .HasColumnName("parent_id")
            .HasColumnType("uuid");

        // 审计字段
        builder.Property(x => x.CreateTime)
            .HasColumnName("create_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreateBy)
            .HasColumnName("create_by")
            .HasColumnType("uuid");

        builder.Property(x => x.CreateName)
            .HasColumnName("create_name")
            .HasColumnType("varchar(50)");

        builder.Property(x => x.UpdateTime)
            .HasColumnName("update_time")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdateBy)
            .HasColumnName("update_by")
            .HasColumnType("uuid");

        builder.Property(x => x.UpdateName)
            .HasColumnName("update_name")
            .HasColumnType("varchar(50)");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("integer")
            .IsRequired();

        // 索引
        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("idx_menu_parent_id");

        // 关系配置 - 自引用 (父子菜单)
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // 
        builder.HasMany(x => x.RoleMenus)
            .WithOne(x => x.Menu)
            .HasForeignKey(x => x.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置与 MenuButton 的一对多关系
        builder.HasMany(m => m.Buttons)
            .WithOne(mb => mb.Menu)
            .HasForeignKey(mb => mb.MenuId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}