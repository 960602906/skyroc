using Domain.Entities.Purchases;

namespace Domain.Interfaces;

/// <summary>
/// 采购单仓储接口，负责聚合读取采购单、商品明细和采购计划占用关系。
/// </summary>
public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    /// <summary>
    /// 检查采购单编号是否已被其他采购单占用。
    /// </summary>
    /// <param name="purchaseNo">待校验的采购单业务编号。</param>
    /// <param name="excludeId">编辑场景需要排除的采购单主键。</param>
    /// <returns>存在同号采购单时返回 <c>true</c>。</returns>
    Task<bool> ExistsPurchaseNoAsync(string purchaseNo, Guid? excludeId = null);
}
