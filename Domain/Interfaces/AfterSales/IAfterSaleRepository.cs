using System.Linq.Expressions;
using Domain.Entities.AfterSales;
using Domain.ReadModels.AfterSales;

namespace Domain.Interfaces;

/// <summary>
/// 售后单聚合仓储，提供详情读取、并发锁定、分页和来源数量占用查询。
/// </summary>
public interface IAfterSaleRepository : IRepository<AfterSale>
{
    /// <summary>
    /// 按业务条件读取轻量售后分页，只返回列表展示与操作判断所需字段。
    /// </summary>
    /// <param name="predicate">售后业务筛选表达式。</param>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页记录数。</param>
    /// <returns>按创建时间和主键稳定倒序排列的列表数据及总记录数。</returns>
    Task<(IReadOnlyList<AfterSaleListItemReadModel> Data, int Total)> GetListPageAsync(
        Expression<Func<AfterSale, bool>>? predicate,
        int pageNumber,
        int pageSize);

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

    /// <summary>
    /// 读取指定销售订单下已完成的售后单，用于订单签收生成账单时补齐已生效财务调整。
    /// </summary>
    /// <param name="saleOrderId">来源销售订单主键。</param>
    /// <returns>包含售后商品明细的已完成售后单集合。</returns>
    Task<List<AfterSale>> GetCompletedBySaleOrderIdAsync(Guid saleOrderId);
}
