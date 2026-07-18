using System.Linq.Expressions;
using Application.Serialization;
using Domain.Entities.Orders;
using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
/// 按商品查看的订单明细分页查询参数。
/// </summary>
public class SaleOrderDetailQueryParameters : PagedQueryParameters
{
    public Guid? SaleOrderId { get; set; }

    public OrderDateType DateType { get; set; } = OrderDateType.OrderDate;

    public DateTime? DateStart { get; set; }

    public DateTime? DateEnd { get; set; }

    public SaleOrderStatus? OrderStatus { get; set; }

    public Guid? CustomerId { get; set; }

    public string? GoodsKey { get; set; }

    public List<Guid>? GoodsIds { get; set; }

    public List<Guid>? GoodsTypeIds { get; set; }

    public Guid? SupplierId { get; set; }

    public bool? HasPurchasePlan { get; set; }

    public Status? Status { get; set; }

    public Expression<Func<SaleOrderDetail, bool>> QueryBuild()
    {
        var goodsKey = GoodsKey?.Trim();
        var dateStart = DateTimeJsonFormats.AsUtcQueryStart(DateStart);
        var dateEnd = DateTimeJsonFormats.AsUtcQueryEndInclusive(DateEnd);
        var dateType = DateType;

        return x =>
            (!SaleOrderId.HasValue || x.SaleOrderId == SaleOrderId.Value)
            && (!dateStart.HasValue
                || (dateType == OrderDateType.OrderDate && x.SaleOrder.OrderDate >= dateStart.Value)
                || (dateType == OrderDateType.ReceiveDate && x.SaleOrder.ReceiveDate.HasValue && x.SaleOrder.ReceiveDate.Value >= dateStart.Value)
                || (dateType == OrderDateType.OutDate && x.SaleOrder.OutDate.HasValue && x.SaleOrder.OutDate.Value >= dateStart.Value))
            && (!dateEnd.HasValue
                || (dateType == OrderDateType.OrderDate && x.SaleOrder.OrderDate <= dateEnd.Value)
                || (dateType == OrderDateType.ReceiveDate && x.SaleOrder.ReceiveDate.HasValue && x.SaleOrder.ReceiveDate.Value <= dateEnd.Value)
                || (dateType == OrderDateType.OutDate && x.SaleOrder.OutDate.HasValue && x.SaleOrder.OutDate.Value <= dateEnd.Value))
            && (!OrderStatus.HasValue || x.SaleOrder.OrderStatus == OrderStatus.Value)
            && (!CustomerId.HasValue || x.SaleOrder.CustomerId == CustomerId.Value)
            && (string.IsNullOrWhiteSpace(goodsKey)
                || x.GoodsNameSnapshot.Contains(goodsKey)
                || x.GoodsCodeSnapshot.Contains(goodsKey))
            && (GoodsIds == null || GoodsIds.Count == 0 || GoodsIds.Contains(x.GoodsId))
            && (GoodsTypeIds == null || GoodsTypeIds.Count == 0 || GoodsTypeIds.Contains(x.Goods.GoodsTypeId))
            && (!SupplierId.HasValue
                || x.Goods.DefaultSupplierId == SupplierId.Value
                || x.Goods.SupplierRelations.Any(relation => relation.SupplierId == SupplierId.Value))
            && (!HasPurchasePlan.HasValue || x.HasPurchasePlan == HasPurchasePlan.Value)
            && (!Status.HasValue || x.Status == Status.Value);
    }
}
