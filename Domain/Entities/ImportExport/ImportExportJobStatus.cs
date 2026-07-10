namespace Domain.Entities.ImportExport;

/// <summary>导入导出任务执行状态。</summary>
public enum ImportExportJobStatus
{
    /// <summary>任务正在读取或生成文件。</summary>
    Processing = 1,

    /// <summary>任务已成功完成。</summary>
    Succeeded = 2,

    /// <summary>任务因格式或业务校验失败而结束。</summary>
    Failed = 3
}
