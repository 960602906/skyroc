using Application.DTOs;

namespace Application.DTOs.System;

/// <summary>运营服务时段响应，时间使用本地业务日内时刻。</summary>
public class ServicePeriodDto : BaseDto
{
    /// <summary>服务时段名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>服务开始时刻。</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>服务结束时刻，必须晚于开始时刻。</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>展示与匹配顺序，数值越小越靠前。</summary>
    public int SortOrder { get; set; }
}
