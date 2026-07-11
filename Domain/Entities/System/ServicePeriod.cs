namespace Domain.Entities.System;

/// <summary>
/// 运营服务时段，限定平台可接受服务或下单的日内时间窗口。
/// </summary>
public class ServicePeriod : BaseEntity
{
    /// <summary>管理员识别时段用途的名称，同一名称仅允许一条记录。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>服务窗口起始时刻（本地业务时间，精确到秒）。</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>服务窗口结束时刻（本地业务时间，必须晚于起始时刻）。</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>同类时段的展示和匹配顺序，数值越小越靠前。</summary>
    public int SortOrder { get; set; }
}
