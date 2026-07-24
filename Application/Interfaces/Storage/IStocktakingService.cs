using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 库存盘点应用服务接口，提供盘点快照创建、查询和一次性差异调整审核。
/// </summary>
public interface IStocktakingService
{
    /// <summary>
    /// 分页查询库存盘点单及其批次差异明细。
    /// </summary>
    /// <param name="parameters">盘点分页和筛选参数。</param>
    /// <returns>盘点单分页结果。</returns>
    Task<PagedResult<StocktakingOrderDto>> GetPagedAsync(StocktakingQueryParameters parameters);

    /// <summary>
    /// 查询指定库存盘点单完整详情。
    /// </summary>
    /// <param name="id">盘点单主键。</param>
    /// <returns>包含账实数量和批次差异的盘点详情。</returns>
    Task<StocktakingOrderDto> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据盘点单号查询盘点详情，包含账实数量和批次差异。
    /// </summary>
    /// <param name="stocktakingNo">盘点单号。</param>
    /// <returns>包含账实数量和批次差异的盘点详情。</returns>
    /// <exception cref="Application.Exceptions.BusinessException">盘点单号不存在时抛出。</exception>
    Task<StocktakingOrderDto> GetByStocktakingNoAsync(string stocktakingNo);

    /// <summary>
    /// 创建库存盘点草稿，以当前批次余额为账面快照并计算实盘差异。
    /// </summary>
    /// <param name="dto">仓库、实盘批次数量和差异说明。</param>
    /// <returns>已固化账面数量、成本和差异的盘点详情。</returns>
    Task<StocktakingOrderDto> CreateAsync(CreateStocktakingDto dto);

    /// <summary>
    /// 审核库存盘点并按批次差异一次性调整余额、追加库存流水。
    /// </summary>
    /// <param name="id">待审核盘点单主键。</param>
    /// <param name="remark">写入库存调整流水的审核说明。</param>
    /// <returns>已审核且标记调整完成的盘点详情。</returns>
    Task<StocktakingOrderDto> AuditAsync(Guid id, string? remark);
}
