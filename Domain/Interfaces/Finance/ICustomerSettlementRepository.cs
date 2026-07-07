using Domain.Entities.Finance;

namespace Domain.Interfaces;

/// <summary>
/// 客户结款凭证仓储接口，提供凭证明细读取、行级锁和编号唯一性校验能力。
/// </summary>
public interface ICustomerSettlementRepository : IRepository<CustomerSettlement>
{
    /// <summary>
    /// 在当前数据库事务内锁定客户结款凭证及明细，供作废操作串行回滚账单余额。
    /// </summary>
    /// <param name="id">客户结款凭证主键。</param>
    /// <returns>包含明细的客户结款凭证；不存在时返回 <c>null</c>。</returns>
    Task<CustomerSettlement?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 检查客户结款凭证编号是否已存在，避免自动编号冲突。
    /// </summary>
    /// <param name="settlementNo">待检查的结款凭证编号。</param>
    /// <returns>编号已被占用时返回 <c>true</c>。</returns>
    Task<bool> ExistsSettlementNoAsync(string settlementNo);
}
