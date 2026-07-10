using Domain.Entities.Traceability;
using Domain.ReadModels.Traceability;

namespace Domain.Interfaces;

/// <summary>
/// 商品溯源记录仓储接口，提供二维码详情读取、报告引用保护和销售出库来源投影。
/// </summary>
public interface ITraceRecordRepository : IRepository<TraceRecord>
{
    /// <summary>读取二维码展示所需的溯源记录和关联检测报告聚合。</summary>
    /// <param name="id">溯源记录主键。</param>
    /// <returns>包含报告、商品和附件的溯源记录；不存在时返回 <c>null</c>。</returns>
    Task<TraceRecord?> GetDetailByIdAsync(Guid id);

    /// <summary>按二维码承载的溯源编号读取完整详情。</summary>
    /// <param name="traceNo">溯源业务唯一编号。</param>
    /// <returns>包含检测报告聚合的溯源记录；不存在时返回 <c>null</c>。</returns>
    Task<TraceRecord?> GetDetailByTraceNoAsync(string traceNo);

    /// <summary>读取指定销售订单已经生成的溯源记录。</summary>
    /// <param name="saleOrderId">销售订单主键。</param>
    /// <returns>按订单商品明细稳定排序的溯源记录。</returns>
    Task<IReadOnlyList<TraceRecord>> GetBySaleOrderIdAsync(Guid saleOrderId);

    /// <summary>在当前事务内锁定销售订单，串行化同一订单的溯源记录生成。</summary>
    /// <param name="saleOrderId">销售订单主键。</param>
    /// <returns>订单存在时返回 <c>true</c>。</returns>
    Task<bool> LockSaleOrderAsync(Guid saleOrderId);

    /// <summary>投影指定销售订单已审核销售出库的批次来源与最新检测报告。</summary>
    /// <param name="saleOrderId">销售订单主键。</param>
    /// <returns>每条已审核销售出库商品行的溯源来源投影。</returns>
    Task<IReadOnlyList<TraceGenerationSource>> GetGenerationSourcesAsync(Guid saleOrderId);

    /// <summary>检查检测报告是否已被任何溯源记录引用。</summary>
    /// <param name="inspectionReportId">检测报告主键。</param>
    /// <returns>存在溯源引用时返回 <c>true</c>。</returns>
    Task<bool> ExistsByInspectionReportIdAsync(Guid inspectionReportId);
}
