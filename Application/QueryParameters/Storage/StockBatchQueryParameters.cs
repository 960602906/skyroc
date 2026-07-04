using Domain.Queries.Storage;

namespace Application.QueryParameters;

/// <summary>
/// 库存批次分页参数，支持按仓库、分类、商品、批次号、效期和余额筛选。
/// </summary>
public class StockBatchQueryParameters : PagedQueryParameters
{
    /// <summary>
    /// 商品名称、商品编码或批次号关键字，采用包含匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 仓库主键筛选。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 商品分类主键筛选。
    /// </summary>
    public Guid? GoodsTypeId { get; set; }

    /// <summary>
    /// 商品主键筛选。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    /// 精确批次号筛选。
    /// </summary>
    public string? BatchNo { get; set; }

    /// <summary>
    /// 到期日期起始值（含）。
    /// </summary>
    public DateOnly? ExpireDateStart { get; set; }

    /// <summary>
    /// 到期日期截止值（含）。
    /// </summary>
    public DateOnly? ExpireDateEnd { get; set; }

    /// <summary>
    /// 是否包含当前账面数量为零的批次；默认不包含。
    /// </summary>
    public bool IncludeZeroStock { get; set; }

    /// <summary>
    /// 转换为仓储层使用的规范化库存批次条件。
    /// </summary>
    /// <returns>去除文本筛选首尾空白后的查询条件。</returns>
    public StockBatchCriteria ToCriteria()
    {
        return new StockBatchCriteria
        {
            Keyword = Normalize(Keyword),
            WareId = WareId,
            GoodsTypeId = GoodsTypeId,
            GoodsId = GoodsId,
            BatchNo = Normalize(BatchNo),
            ExpireDateStart = ExpireDateStart,
            ExpireDateEnd = ExpireDateEnd,
            IncludeZeroStock = IncludeZeroStock
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
