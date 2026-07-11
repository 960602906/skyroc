namespace Application.DTOs.System;

/// <summary>新增或修改运营服务时段的请求。</summary>
public class UpsertServicePeriodDto
{
    /// <summary>服务时段名称，去除首尾空白后不得为空且全局唯一。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>日内服务开始时刻。</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>日内服务结束时刻，必须晚于开始时刻。</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>展示和匹配顺序，不能为负数。</summary>
    public int SortOrder { get; set; }

    /// <summary>是否启用该服务时段。</summary>
    public bool IsEnabled { get; set; } = true;
}
