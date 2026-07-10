using Domain.Entities.Traceability;

namespace Domain.Interfaces;

/// <summary>
/// 检测报告仓储接口，读取包含商品和附件的报告聚合，并支持删除前的并发锁定。
/// </summary>
public interface IInspectionReportRepository : IRepository<InspectionReport>
{
    /// <summary>读取包含商品明细和附件的检测报告。</summary>
    /// <param name="id">检测报告主键。</param>
    /// <returns>完整报告聚合；不存在时返回 <c>null</c>。</returns>
    Task<InspectionReport?> GetDetailByIdAsync(Guid id);

    /// <summary>在当前事务内锁定并读取检测报告聚合。</summary>
    /// <param name="id">检测报告主键。</param>
    /// <returns>已锁定的报告聚合；不存在时返回 <c>null</c>。</returns>
    Task<InspectionReport?> GetDetailByIdForUpdateAsync(Guid id);

    /// <summary>检查检测报告编号是否已占用。</summary>
    /// <param name="inspectionNo">待检查的报告编号。</param>
    /// <param name="excludeId">编辑时需要排除的报告主键。</param>
    /// <returns>编号已存在时返回 <c>true</c>。</returns>
    Task<bool> ExistsInspectionNoAsync(string inspectionNo, Guid? excludeId = null);

    /// <summary>在当前事务内按稳定顺序锁定指定检测报告，避免生成溯源与编辑报告交错。</summary>
    /// <param name="ids">检测报告主键集合。</param>
    /// <returns>已锁定的报告主键集合。</returns>
    Task<IReadOnlyList<Guid>> LockByIdsAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>读取已锁定报告当前仍关联的来源采购入库商品明细主键。</summary>
    /// <param name="ids">已锁定的检测报告主键集合。</param>
    /// <returns>报告主键与来源入库商品明细主键的关系集合。</returns>
    Task<IReadOnlyList<(Guid InspectionReportId, Guid StockInDetailId)>> GetLockedGoodsSourceIdsAsync(
        IReadOnlyCollection<Guid> ids);
}
