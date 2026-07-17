using Application.DTOs.Purchases;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 采购计划应用服务，提供查询、生成、分配、合并和拆分能力。
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

    /// <summary>
    /// 为一批未发布采购计划分配或清除供应商，并刷新供应商名称快照。
    /// </summary>
    /// <param name="dto">采购计划主键及目标供应商。</param>
    /// <returns>更新后的采购计划详情集合。</returns>
    Task<List<PurchasePlanDto>> AssignSupplierAsync(AssignPurchasePlanSupplierDto dto);

    /// <summary>
    /// 为一批未发布采购计划分配或清除采购员，并刷新采购员名称快照。
    /// </summary>
    /// <param name="dto">采购计划主键及目标采购员。</param>
    /// <returns>更新后的采购计划详情集合。</returns>
    Task<List<PurchasePlanDto>> AssignPurchaserAsync(AssignPurchasePlanPurchaserDto dto);

    /// <summary>
    /// 将采购模式、供应商和采购员一致的未发布计划合并为一张新计划。
    /// </summary>
    /// <param name="dto">待合并计划集合及新计划备注。</param>
    /// <returns>合并后的采购计划详情；计划交期取来源计划最早交期。</returns>
    Task<PurchasePlanDto> MergeAsync(MergePurchasePlansDto dto);

    /// <summary>
    /// 查询采购计划中可用于按订单拆分的来源订单及需求数量。
    /// </summary>
    /// <param name="planId">采购计划主键。</param>
    /// <returns>按来源订单聚合的可拆分摘要。</returns>
    Task<List<SplittablePurchasePlanOrderDto>> GetSplittableOrdersAsync(Guid planId);

    /// <summary>
    /// 将指定来源订单的需求及其计划数量从原计划拆入一张新计划。
    /// </summary>
    /// <param name="dto">原计划、来源订单集合及新计划备注。</param>
    /// <returns>拆分产生的新采购计划详情。</returns>
    Task<PurchasePlanDto> SplitByOrdersAsync(SplitPurchasePlanByOrdersDto dto);

    /// <summary>
    /// 按商品明细指定数量拆分采购计划，并按比例分配来源订单需求数量。
    /// </summary>
    /// <param name="dto">原计划及各明细拆出数量。</param>
    /// <returns>拆分产生的新采购计划详情。</returns>
    Task<PurchasePlanDto> SplitByQuantityAsync(SplitPurchasePlanByQuantityDto dto);
}
