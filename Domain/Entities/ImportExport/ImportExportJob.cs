namespace Domain.Entities.ImportExport;

/// <summary>
/// 导入导出任务，记录一次同步文件处理的类型、执行结果和可追溯摘要；文件内容不在本实体保存。
/// </summary>
public class ImportExportJob : BaseEntity
{
    /// <summary>任务业务唯一编号。</summary>
    public string JobNo { get; set; } = string.Empty;

    /// <summary>任务处理的业务对象类型。</summary>
    public ImportExportJobType JobType { get; set; }

    /// <summary>任务方向，区分导入和导出。</summary>
    public ImportExportDirection JobDirection { get; set; }

    /// <summary>执行状态：处理中、成功或失败。</summary>
    public ImportExportJobStatus JobStatus { get; set; }

    /// <summary>导入源文件或导出结果的文件名。</summary>
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>本次处理的数据行总数，不含 CSV 表头。</summary>
    public int TotalRows { get; set; }

    /// <summary>本次成功处理的数据行数。</summary>
    public int SuccessRows { get; set; }

    /// <summary>本次失败的数据行数；整文件校验失败时等于总行数。</summary>
    public int FailureRows { get; set; }

    /// <summary>失败时返回给操作人的行号和原因摘要，成功时为空。</summary>
    public string? ErrorSummary { get; set; }

    /// <summary>开始读取或生成文件的时间（UTC）。</summary>
    public DateTime JobStartedAt { get; set; }

    /// <summary>任务结束时间（UTC）；处理中时为空。</summary>
    public DateTime? JobFinishedAt { get; set; }
}
