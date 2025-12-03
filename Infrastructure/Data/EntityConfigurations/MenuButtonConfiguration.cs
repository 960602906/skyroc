using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
///     Menu 实体映射配置
/// </summary>
public class MenuButtonConfiguration : IEntityTypeConfiguration<MenuButton>
{
    public void Configure(EntityTypeBuilder<MenuButton> builder)
    {
        // 表名
        builder.ToTable("sys_menu_button", "public");
        builder.HasKey(mb => mb.Id);
        builder.Property(mb => mb.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()") // PostgreSQL
            .ValueGeneratedOnAdd();
        builder.Property(mb => mb.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("按钮编码");
        builder.Property(mb => mb.Desc)
            .HasColumnName("desc")
            .IsRequired()
            .HasMaxLength(500)
            .HasComment("按钮描述");
        builder.Property(mb => mb.MenuId)
            .HasColumnName("menu_id")
            .IsRequired()
            .HasComment("所属菜单ID");
        // 公共字段
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
        // 配置与 Menu 的关系 (一对多)
        builder.HasOne(mb => mb.Menu)
            .WithMany(m => m.Buttons)
            .HasForeignKey(mb => mb.MenuId)
            .OnDelete(DeleteBehavior.Cascade); // 删除菜单时级联删除按钮
        // 创建索引
        builder.HasIndex(mb => mb.MenuId)
            .HasDatabaseName("idx_menu_buttons_menuId");
        // 同一个菜单下的按钮编码必须唯一
        builder.HasIndex(mb => new { mb.MenuId, mb.Code })
            .IsUnique()
            .HasDatabaseName("idx_menu_buttons_menu_id_code");
    }
}