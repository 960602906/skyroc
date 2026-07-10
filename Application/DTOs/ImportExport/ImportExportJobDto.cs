using Application.Serialization;
using Domain.Entities.ImportExport;
using System.Text.Json.Serialization;

namespace Application.DTOs.ImportExport;

/// <summary>导入导出任务响应，返回任务执行方向、数据行统计和失败摘要。</summary>
public class ImportExportJobDto
{
    /// <summary>任务主键。</summary>
    public Guid Id { get; set; }
    /// <summary>任务业务唯一编号。</summary>
    public string JobNo { get; set; } = string.Empty;
    /// <summary>处理的业务对象类型；当前为 1 商品。</summary>
    public ImportExportJobType JobType { get; set; }
    /// <summary>任务方向；1 导入，2 导出。</summary>
    public ImportExportDirection Direction { get; set; }
    /// <summary>执行状态；1 处理中，2 成功，3 失败。</summary>
    public ImportExportJobStatus JobStatus { get; set; }
    /// <summary>导入源文件或导出结果的文件名。</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>处理的数据行总数，不含 CSV 表头。</summary>
    public int TotalRows { get; set; }
    /// <summary>成功处理的数据行数。</summary>
    public int SuccessRows { get; set; }
    /// <summary>失败处理的数据行数。</summary>
    public int FailureRows { get; set; }
    /// <summary>失败时的行号与原因摘要。</summary>
    public string? ErrorSummary { get; set; }
    /// <summary>任务开始处理时间（UTC，格式 yyyy-MM-dd HH:mm:ss）。</summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime StartedTime { get; set; }
    /// <summary>任务结束处理时间（UTC，格式 yyyy-MM-dd HH:mm:ss）。</summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? FinishedTime { get; set; }
}
