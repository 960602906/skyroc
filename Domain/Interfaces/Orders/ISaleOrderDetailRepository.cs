using Domain.Entities.Orders;

namespace Domain.Interfaces;

/// <summary>
/// 销售订单明细仓储接口。
/// </summary>
public interface ISaleOrderDetailRepository : IRepository<SaleOrderDetail>
{
    /// <summary>
    /// 读取指定销售订单的全部商品明细。
    /// </summary>
    Task<List<SaleOrderDetail>> GetBySaleOrderIdAsync(Guid saleOrderId);

    /// <summary>
    /// 批量读取指定主键的销售订单商品明细。
    /// </summary>
    Task<List<SaleOrderDetail>> GetByIdsAsync(IEnumerable<Guid> ids);
}
