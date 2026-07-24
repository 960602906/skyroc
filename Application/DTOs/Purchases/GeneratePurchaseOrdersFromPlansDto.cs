namespace Application.DTOs.Purchases;

/// <summary>
/// 从采购计划剩余数量批量生成采购单的请求。
/// </summary>
public class GeneratePurchaseOrdersFromPlansDto
{
    /// <summary>
    /// 待生成采购单的采购计划主键集合；重复主键会被去重。
    /// </summary>
    public List<Guid> PlanIds { get; set; } = [];

    /// <summary>
    /// 预计到货时间（UTC）；省略时每组采购单取来源计划最早交期。
    /// </summary>
    public DateTime? ReceiveTime { get; set; }

    /// <summary>
    /// 写入本次生成采购单的统一业务备注。
    /// </summary>
    public string? Remark { get; set; }
}
