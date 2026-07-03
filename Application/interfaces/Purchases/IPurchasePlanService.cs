using Application.DTOs.Purchases;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 采购计划应用服务，提供查询、手工新增和从已审核订单生成计划的能力。
/// </summary>
public interface IPurchasePlanService
{
    /// <summary>
    /// 按业务条件分页查询采购计划，按计划交期倒序返回。
    /// </summary>
    /// <param name="parameters">采购计划分页与筛选参数。</param>
    /// <returns>分页后的采购计划集合。</returns>
    Task<PagedResult<PurchasePlanDto>> GetPagedAsync(PurchasePlanQueryParameters parameters);

    /// <summary>
    /// 读取采购计划详情，包含商品明细与来源订单关系。
    /// </summary>
    /// <param name="id">采购计划主键。</param>
    /// <returns>采购计划详情。</returns>
    Task<PurchasePlanDto> GetByIdAsync(Guid id);

    /// <summary>
    /// 手工新增一张采购计划及其商品明细。
    /// </summary>
    /// <param name="dto">采购计划创建请求。</param>
    /// <returns>创建后的采购计划详情。</returns>
    Task<PurchasePlanDto> CreateAsync(CreatePurchasePlanDto dto);

    /// <summary>
    /// 从已审核通过且未生成过计划的销售订单批量生成采购计划，
    /// 每个订单生成一张计划，按商品聚合明细并回写订单的采购计划标记。
    /// </summary>
    /// <param name="dto">来源销售订单集合及备注。</param>
    /// <returns>本次生成的采购计划详情集合。</returns>
    Task<List<PurchasePlanDto>> GenerateFromOrdersAsync(GeneratePurchasePlanFromOrdersDto dto);
}
