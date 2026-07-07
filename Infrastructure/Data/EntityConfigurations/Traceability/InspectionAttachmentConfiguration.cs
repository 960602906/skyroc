using Domain.Entities.Traceability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 检测报告附件的数据库映射配置。
/// </summary>
public class InspectionAttachmentConfiguration : IEntityTypeConfiguration<InspectionAttachment>
{
    /// <summary>
    /// 配置附件类型与文件大小约束、展示顺序和随报告级联删除的外键。
    /// </summary>
    /// <param name="builder">检测报告附件实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<InspectionAttachment> builder)
    {
        builder.ToTable("inspection_attachment", table =>
        {
            table.HasCheckConstraint("ck_inspection_attachment_type", "attachment_type IN (1, 2)");
            table.HasCheckConstraint("ck_inspection_attachment_file_size", "file_size >= 0");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.InspectionReportId).HasColumnName("inspection_report_id").IsRequired();
        builder.Property(x => x.AttachmentType).HasColumnName("attachment_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.FileUrl).HasColumnName("file_url").HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileSize).HasColumnName("file_size");
        builder.Property(x => x.Sort).HasColumnName("sort").IsRequired();

        builder.HasIndex(x => new { x.InspectionReportId, x.Sort }).HasDatabaseName("idx_inspection_attachment_report_sort");

        builder.HasOne(x => x.InspectionReport)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.InspectionReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
