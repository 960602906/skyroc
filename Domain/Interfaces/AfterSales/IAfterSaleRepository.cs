using Domain.Entities.AfterSales;

namespace Domain.Interfaces;

/// <summary>
/// 售后单聚合仓储，提供详情读取、并发锁定、分页和来源数量占用查询。
/// </summary>
public interface IAfterSaleRepository : IRepository<AfterSale>
{
    /// <summary>
    /// 在当前数据库事务内锁定并读取售后聚合，防止审核、反审核、编辑和删除并发交错。
    /// </summary>
    /// <param name="id">待锁定的售后单主键。</param>
    /// <returns>包含商品、审核轨迹和取货任务的售后单；不存在时返回 <c>null</c>。</returns>
    Task<AfterSale?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 检查售后单号是否已被占用。
    /// </summary>
    /// <param name="afterSaleNo">待检查的规范化售后单号。</param>
    Task<bool> ExistsAfterSaleNoAsync(string afterSaleNo);

    /// <summary>
    /// 汇总指定订单商品行已被售后申请占用的基础单位数量。
    /// </summary>
    /// <param name="saleOrderDetailIds">来源销售订单商品行主键集合。</param>
    /// <param name="excludeAfterSaleId">编辑时排除的当前售后单主键。</param>
    Task<IReadOnlyDictionary<Guid, decimal>> GetReservedBaseQuantitiesAsync(
        IEnumerable<Guid> saleOrderDetailIds,
        Guid? excludeAfterSaleId = null);
}
