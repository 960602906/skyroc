namespace Domain.Entities.ImportExport;

/// <summary>导入导出任务方向。</summary>
public enum ImportExportDirection
{
    /// <summary>从 CSV 文件写入业务数据。</summary>
    Import = 1,

    /// <summary>从业务数据生成 CSV 文件。</summary>
    Export = 2
}
