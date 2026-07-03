using Domain.Entities.Orders;

namespace Domain.Interfaces;

/// <summary>
/// 销售订单明细仓储接口。
/// </summary>
public interface ISaleOrderDetailRepository : IRepository<SaleOrderDetail>
{
    Task<List<SaleOrderDetail>> GetBySaleOrderIdAsync(Guid saleOrderId);

    Task<List<SaleOrderDetail>> GetByIdsAsync(IEnumerable<Guid> ids);
}
