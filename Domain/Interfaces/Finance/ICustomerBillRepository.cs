using Domain.Entities.Finance;

namespace Domain.Interfaces;

/// <summary>
/// 客户账单仓储接口，提供按来源订单读取、锁定和编号唯一性校验能力。
/// </summary>
public interface ICustomerBillRepository : IRepository<CustomerBill>
{
    /// <summary>
    /// 根据来源销售订单读取客户账单及明细。
    /// </summary>
    /// <param name="saleOrderId">来源销售订单主键。</param>
    /// <returns>包含明细的客户账单；尚未生成时返回 <c>null</c>。</returns>
    Task<CustomerBill?> GetBySaleOrderIdAsync(Guid saleOrderId);

    /// <summary>
    /// 在当前数据库事务内锁定来源销售订单对应账单，供签收和售后并发同步应收时串行更新。
    /// </summary>
    /// <param name="saleOrderId">来源销售订单主键。</param>
    /// <returns>被锁定的客户账单；尚未生成时返回 <c>null</c>。</returns>
    Task<CustomerBill?> GetBySaleOrderIdForUpdateAsync(Guid saleOrderId);

    /// <summary>
    /// 检查客户账单编号是否已存在，避免自动编号冲突。
    /// </summary>
    /// <param name="billNo">待检查的账单编号。</param>
    /// <returns>编号已被占用时返回 <c>true</c>。</returns>
    Task<bool> ExistsBillNoAsync(string billNo);

    /// <summary>
    /// 显式追加客户账单明细，供已存在账单增补订单或售后来源行时标记为新增。
    /// </summary>
    /// <param name="detail">待追加的客户账单明细。</param>
    Task AddDetailAsync(CustomerBillDetail detail);
}
