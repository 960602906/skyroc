using Domain.Entities.Finance;

namespace Domain.Interfaces;

/// <summary>
/// 供应商结算单仓储接口，提供结算明细读取、行级锁和编号唯一性校验能力。
/// </summary>
public interface ISupplierSettlementRepository : IRepository<SupplierSettlement>
{
    /// <summary>
    /// 在当前数据库事务内锁定供应商结算单及明细，供作废操作串行回滚待结单据余额。
    /// </summary>
    /// <param name="id">供应商结算单主键。</param>
    /// <returns>包含明细的供应商结算单；不存在时返回 <c>null</c>。</returns>
    Task<SupplierSettlement?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 检查供应商结算单编号是否已存在，避免自动编号冲突。
    /// </summary>
    /// <param name="settlementNo">待检查的结算单编号。</param>
    /// <returns>编号已被占用时返回 <c>true</c>。</returns>
    Task<bool> ExistsSettlementNoAsync(string settlementNo);

    /// <summary>
    /// 检查指定供应商待结单据是否已被任何结算单明细引用（含已作废结算单），
    /// 供来源出入库单反审核前判断是否会破坏结算历史外键。
    /// </summary>
    /// <param name="supplierBillId">供应商待结单据主键。</param>
    /// <returns>存在结算明细引用时返回 <c>true</c>。</returns>
    Task<bool> ExistsDetailByBillIdAsync(Guid supplierBillId);
}
