using Domain.Entities.Printing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 打印模板字段定义的 PostgreSQL 映射配置。
/// </summary>
public class PrintTemplateFieldConfiguration : IEntityTypeConfiguration<PrintTemplateField>
{
    /// <summary>配置字段路径唯一性、显示顺序约束和模板级级联删除关系。</summary>
    /// <param name="builder">打印模板字段实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<PrintTemplateField> builder)
    {
        builder.ToTable("print_template_field", table =>
        {
            table.HasCheckConstraint("ck_print_template_field_order", "display_order >= 0");
        });
        builder.ConfigureBaseEntity();
        builder.Property(x => x.PrintTemplateId).HasColumnName("print_template_id").IsRequired();
        builder.Property(x => x.FieldKey).HasColumnName("field_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayOrder).HasColumnName("display_order").IsRequired();
        builder.Property(x => x.Format).HasColumnName("format").HasMaxLength(100);
        builder.HasIndex(x => new { x.PrintTemplateId, x.FieldKey }).IsUnique()
            .HasDatabaseName("uk_print_template_field_key");
        builder.HasIndex(x => new { x.PrintTemplateId, x.DisplayOrder }).IsUnique()
            .HasDatabaseName("uk_print_template_field_order");
        builder.HasOne(x => x.PrintTemplate)
            .WithMany(x => x.Fields)
            .HasForeignKey(x => x.PrintTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
