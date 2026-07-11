using Domain.Entities.Printing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 打印模板主表的 PostgreSQL 映射配置。
/// </summary>
public class PrintTemplateConfiguration : IEntityTypeConfiguration<PrintTemplate>
{
    /// <summary>配置模板编码唯一性、业务类型边界和设计 JSON 存储方式。</summary>
    /// <param name="builder">打印模板实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<PrintTemplate> builder)
    {
        builder.ToTable("print_template", table =>
        {
            table.HasCheckConstraint("ck_print_template_business_type", "business_type BETWEEN 1 AND 6");
        });
        builder.ConfigureBaseEntity();
        builder.Property(x => x.TemplateCode).HasColumnName("template_code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.BusinessType).HasColumnName("business_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.DesignJson).HasColumnName("design_json").HasColumnType("text").IsRequired();
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true).IsRequired();
        builder.HasIndex(x => x.TemplateCode).IsUnique().HasDatabaseName("uk_print_template_code");
        builder.HasIndex(x => new { x.BusinessType, x.IsEnabled }).HasDatabaseName("idx_print_template_type_enabled");
    }
}
