namespace Application.DTOs.Storage;

/// <summary>
/// 盘点批次实盘数量请求；账面数量、商品、单位和成本均由服务端读取库存快照。
/// </summary>
public class CreateStocktakingDetailDto
{
    /// <summary>
    /// 被盘点库存批次主键；必须属于盘点仓库且在同一盘点单内唯一。
    /// </summary>
    public Guid StockBatchId { get; set; }

    /// <summary>
    /// 盘点人员确认的实际库存数量，按批次商品基础单位计量且不得为负。
    /// </summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>
    /// 当前批次的差异原因或现场说明，最长 500 个字符。
    /// </summary>
    public string? Remark { get; set; }
}
