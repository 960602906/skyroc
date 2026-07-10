using System.Linq.Expressions;
using Domain.Entities.Traceability;
namespace Application.QueryParameters.Traceability;
/// <summary>商品溯源分页查询条件，支持订单、仓库、供应商、商品分类、商品与批次筛选。</summary>
public class TraceRecordQueryParameters : PagedQueryParameters
{
    /// <summary>销售订单主键。</summary>
    public Guid? SaleOrderId { get; set; }
    /// <summary>仓库主键。</summary>
    public Guid? WareId { get; set; }
    /// <summary>供应商主键。</summary>
    public Guid? SupplierId { get; set; }
    /// <summary>商品主键。</summary>
    public Guid? GoodsId { get; set; }
    /// <summary>商品分类名称精确筛选。</summary>
    public string? GoodsTypeName { get; set; }
    /// <summary>模糊匹配溯源编号、订单号、客户、商品、供应商、仓库或批次号。</summary>
    public string? Keyword { get; set; }
    /// <summary>构造可由 EF Core 翻译的溯源记录筛选表达式。</summary>
    public Expression<Func<TraceRecord, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        var type = GoodsTypeName?.Trim();
        return x => (!SaleOrderId.HasValue || x.SaleOrderId == SaleOrderId.Value)
                    && (!WareId.HasValue || x.WareId == WareId.Value)
                    && (!SupplierId.HasValue || x.SupplierId == SupplierId.Value)
                    && (!GoodsId.HasValue || x.GoodsId == GoodsId.Value)
                    && (string.IsNullOrWhiteSpace(type) || x.GoodsTypeNameSnapshot == type)
                    && (string.IsNullOrWhiteSpace(keyword) || x.TraceNo.Contains(keyword)
                        || x.SaleOrderNoSnapshot.Contains(keyword) || x.CustomerNameSnapshot.Contains(keyword)
                        || x.GoodsNameSnapshot.Contains(keyword) || x.GoodsCodeSnapshot.Contains(keyword)
                        || (x.SupplierNameSnapshot != null && x.SupplierNameSnapshot.Contains(keyword))
                        || (x.WareNameSnapshot != null && x.WareNameSnapshot.Contains(keyword))
                        || (x.BatchNoSnapshot != null && x.BatchNoSnapshot.Contains(keyword)));
    }
}
