namespace Application.DTOs.Storage;

/// <summary>
/// 库存盘点创建请求，按指定仓库和批次实盘数生成不可变的账面库存快照。
/// </summary>
public class CreateStocktakingDto
{
    /// <summary>
    /// 被盘点仓库主键；所有批次必须属于该仓库。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 盘点范围、原因或整体差异说明，最长 500 个字符。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 实际盘点的库存批次集合，至少一项且批次不得重复。
    /// </summary>
    public List<CreateStocktakingDetailDto> Details { get; set; } = [];
}
