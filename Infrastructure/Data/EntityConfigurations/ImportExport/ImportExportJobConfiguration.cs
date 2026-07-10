using Domain.Entities.ImportExport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 导入导出任务的 PostgreSQL 映射配置。
/// </summary>
public class ImportExportJobConfiguration : IEntityTypeConfiguration<ImportExportJob>
{
    /// <summary>
    /// 配置任务编号唯一性、类型状态约束、执行计数非负约束及常用查询索引。
    /// </summary>
    /// <param name="builder">导入导出任务实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<ImportExportJob> builder)
    {
        builder.ToTable("import_export_job", table =>
        {
            table.HasCheckConstraint("ck_import_export_job_type", "job_type BETWEEN 1 AND 1");
            table.HasCheckConstraint("ck_import_export_job_direction", "direction BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_import_export_job_status", "job_status BETWEEN 1 AND 3");
            table.HasCheckConstraint("ck_import_export_job_rows", "total_rows >= 0 AND success_rows >= 0 AND failure_rows >= 0 AND success_rows + failure_rows <= total_rows");
        });
        builder.ConfigureBaseEntity();
        builder.Property(x => x.JobNo).HasColumnName("job_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.JobType).HasColumnName("job_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.JobDirection).HasColumnName("direction").HasColumnType("integer").IsRequired();
        builder.Property(x => x.JobStatus).HasColumnName("job_status").HasColumnType("integer").IsRequired();
        builder.Property(x => x.SourceFileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.TotalRows).HasColumnName("total_rows").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.SuccessRows).HasColumnName("success_rows").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.FailureRows).HasColumnName("failure_rows").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.ErrorSummary).HasColumnName("error_summary").HasMaxLength(4000);
        builder.Property(x => x.JobStartedAt).HasColumnName("started_time").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.JobFinishedAt).HasColumnName("finished_time").HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.JobNo).IsUnique().HasDatabaseName("idx_import_export_job_no");
        builder.HasIndex(x => new { x.CreateBy, x.CreateTime }).HasDatabaseName("idx_import_export_job_creator_time");
        builder.HasIndex(x => new { x.JobType, x.JobDirection, x.JobStatus }).HasDatabaseName("idx_import_export_job_state");
    }
}
