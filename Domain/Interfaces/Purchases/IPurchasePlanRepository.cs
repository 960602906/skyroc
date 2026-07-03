using Domain.Entities.Purchases;

namespace Domain.Interfaces;

/// <summary>
/// 采购计划仓储接口，负责聚合读取计划、明细和订单来源关系。
/// </summary>
public interface IPurchasePlanRepository : IRepository<PurchasePlan>
{
    /// <summary>
    /// 检查采购计划编号是否已被其他计划占用。
    /// </summary>
    /// <param name="planNo">待校验的采购计划编号。</param>
    /// <param name="excludeId">需要排除的采购计划主键，编辑场景传入自身。</param>
    /// <returns>存在返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    Task<bool> ExistsPlanNoAsync(string planNo, Guid? excludeId = null);
}
