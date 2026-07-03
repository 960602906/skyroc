namespace Application.DTOs.Purchases;

/// <summary>
/// 从已审核销售订单生成采购计划的请求 DTO。
/// </summary>
public class GeneratePurchasePlanFromOrdersDto
{
    /// <summary>
    /// 参与生成采购计划的已审核销售订单主键集合，至少一项。
    /// 每个订单生成一张采购计划，且订单必须已审核通过且尚未生成过采购计划。
    /// </summary>
    public List<Guid> OrderIds { get; set; } = [];

    /// <summary>
    /// 业务备注，写入生成的每张采购计划。
    /// </summary>
    public string? Remark { get; set; }
}
