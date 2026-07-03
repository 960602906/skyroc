using System.Linq.Expressions;
using Domain.Entities.Orders;
using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
/// 按订单查看的分页查询参数。
/// </summary>
public class SaleOrderQueryParameters : PagedQueryParameters
{
    public OrderDateType DateType { get; set; } = OrderDateType.OrderDate;

    public DateTime? DateStart { get; set; }

    public DateTime? DateEnd { get; set; }

    public string? Keyword { get; set; }

    public SaleOrderStatus? OrderStatus { get; set; }

    public Guid? CustomerId { get; set; }

    public bool? HasOutSale { get; set; }

    public bool? UpdateStatus { get; set; }

    public List<Guid>? CustomerTagIds { get; set; }

    public OrderReturnStatus? ReturnStatus { get; set; }

    public string? GoodsKey { get; set; }

    public List<Guid>? GoodsIds { get; set; }

    public List<Guid>? GoodsTypeIds { get; set; }

    public bool? HasPurchasePlan { get; set; }

    public Guid? SupplierId { get; set; }

    public Status? Status { get; set; }

    public Expression<Func<SaleOrder, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        var goodsKey = GoodsKey?.Trim();

        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.OrderNo.Contains(keyword)
             || x.CustomerNameSnapshot.Contains(keyword)
             || x.CustomerCodeSnapshot.Contains(keyword))
            && (!DateStart.HasValue
                || (DateType == OrderDateType.OrderDate && x.OrderDate >= DateStart.Value)
                || (DateType == OrderDateType.ReceiveDate && x.ReceiveDate.HasValue && x.ReceiveDate.Value >= DateStart.Value)
                || (DateType == OrderDateType.OutDate && x.OutDate.HasValue && x.OutDate.Value >= DateStart.Value))
            && (!DateEnd.HasValue
                || (DateType == OrderDateType.OrderDate && x.OrderDate <= DateEnd.Value)
                || (DateType == OrderDateType.ReceiveDate && x.ReceiveDate.HasValue && x.ReceiveDate.Value <= DateEnd.Value)
                || (DateType == OrderDateType.OutDate && x.OutDate.HasValue && x.OutDate.Value <= DateEnd.Value))
            && (!OrderStatus.HasValue || x.OrderStatus == OrderStatus.Value)
            && (!CustomerId.HasValue || x.CustomerId == CustomerId.Value)
            && (!HasOutSale.HasValue || x.HasOutSale == HasOutSale.Value)
            && (!UpdateStatus.HasValue || x.UpdateStatus == UpdateStatus.Value)
            && (CustomerTagIds == null || CustomerTagIds.Count == 0
                || x.Customer.TagRelations.Any(relation => CustomerTagIds.Contains(relation.CustomerTagId)))
            && (!ReturnStatus.HasValue || x.ReturnStatus == ReturnStatus.Value)
            && (string.IsNullOrWhiteSpace(goodsKey)
                || x.Details.Any(detail => detail.GoodsNameSnapshot.Contains(goodsKey)
                                           || detail.GoodsCodeSnapshot.Contains(goodsKey)))
            && (GoodsIds == null || GoodsIds.Count == 0
                || x.Details.Any(detail => GoodsIds.Contains(detail.GoodsId)))
            && (GoodsTypeIds == null || GoodsTypeIds.Count == 0
                || x.Details.Any(detail => GoodsTypeIds.Contains(detail.Goods.GoodsTypeId)))
            && (!HasPurchasePlan.HasValue || x.HasPurchasePlan == HasPurchasePlan.Value)
            && (!SupplierId.HasValue
                || x.Details.Any(detail => detail.Goods.DefaultSupplierId == SupplierId.Value
                                           || detail.Goods.SupplierRelations.Any(
                                               relation => relation.SupplierId == SupplierId.Value)))
            && (!Status.HasValue || x.Status == Status.Value);
    }
}
