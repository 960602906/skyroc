using Domain.Entities.Orders;
using Domain.ReadModels.BaseData;

namespace Domain.Interfaces;

/// <summary>
/// 销售订单仓储接口。
/// </summary>
public interface ISaleOrderRepository : IRepository<SaleOrder>
{
    /// <summary>
    /// 在当前数据库事务内锁定并读取销售订单聚合，供审核状态流转、销售出库等需要串行校验的场景使用。
    /// </summary>
    /// <param name="id">待锁定的销售订单主键。</param>
    /// <returns>包含商品明细的销售订单；不存在时返回 <c>null</c>。</returns>
    Task<SaleOrder?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 检查销售订单编号是否已被其他订单使用。
    /// </summary>
    Task<bool> ExistsOrderNoAsync(string orderNo, Guid? excludeId = null);

    /// <summary>
    /// 批量只读销售订单主单及商品明细快照，用于订单打印等场景。
    /// </summary>
    /// <param name="ids">待读取的销售订单主键集合。</param>
    /// <returns>存在的销售订单聚合集合。</returns>
    Task<List<SaleOrder>> GetByIdsAsync(IEnumerable<Guid> ids);

    /// <summary>原子标记指定销售订单已完成正式打印。</summary>
    /// <param name="ids">待标记的销售订单主键集合。</param>
    /// <param name="updatedBy">确认打印的操作人主键。</param>
    /// <param name="updateName">确认打印的操作人名称快照。</param>
    /// <returns>实际标记成功的订单数量。</returns>
    Task<int> MarkPrintedAsync(IReadOnlyCollection<Guid> ids, Guid? updatedBy, string? updateName);

    /// <summary>
    ///     按订单号限量搜索轻量选择项；空关键词时按最近创建顺序返回。
    /// </summary>
    /// <param name="keyword">订单号关键词。</param>
    /// <param name="take">数据库读取数量，调用方可多取一条判断是否还有结果。</param>
    Task<List<SelectionOption>> SearchSelectionOptionsAsync(string? keyword, int take);

    /// <summary>
    ///     按主键集合解析销售订单的订单号和客户名称。
    /// </summary>
    /// <param name="ids">已去重的销售订单主键集合。</param>
    Task<List<SelectionOption>> ResolveSelectionOptionsAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>
    /// 根据销售订单号查询销售订单详情（含商品明细）。
    /// </summary>
    /// <param name="orderNo">销售订单号。</param>
    /// <returns>销售订单聚合；不存在时返回 <c>null</c>。</returns>
    Task<SaleOrder?> GetByOrderNoAsync(string orderNo);
}
