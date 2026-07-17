using Application.DTOs.Purchases;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 采购单应用服务，提供查询、维护、计划生成和执行状态流转能力。
/// </summary>
public interface IPurchaseOrderService
{
    /// <summary>
    /// 按采购单编号、责任方、商品、预计到货时间和执行状态分页查询。
    /// </summary>
    /// <param name="parameters">采购单分页与筛选参数。</param>
    /// <returns>按创建时间倒序排列的采购单分页结果。</returns>
    Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(PurchaseOrderQueryParameters parameters);

    /// <summary>
    /// 读取采购单详情，包含商品明细和采购计划来源。
    /// </summary>
    /// <param name="id">采购单主键。</param>
    /// <returns>采购单完整详情。</returns>
    Task<PurchaseOrderDto> GetByIdAsync(Guid id);

    /// <summary>
    /// 手工创建一张采购单草稿并保存商品、单位、责任方和价格快照。
    /// </summary>
    /// <param name="dto">手工采购单创建请求。</param>
    /// <returns>创建后的采购单详情。</returns>
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto);

    /// <summary>
    /// 编辑采购单草稿的责任方、到货安排、商品行及计划数量占用。
    /// </summary>
    /// <param name="dto">采购单完整替换请求。</param>
    /// <returns>更新后的采购单详情。</returns>
    Task<PurchaseOrderDto> UpdateAsync(UpdatePurchaseOrderDto dto);

    /// <summary>
    /// 删除采购单草稿；来源计划数量占用会在同一事务中释放。
    /// </summary>
    /// <param name="id">待删除采购单主键。</param>
    /// <returns>删除成功返回 <c>true</c>。</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// 从采购计划的全部剩余数量生成采购单草稿，并按采购模式、供应商和采购员分组。
    /// </summary>
    /// <param name="dto">来源采购计划集合、预计到货时间和统一备注。</param>
    /// <returns>本次生成的采购单详情集合。</returns>
    Task<List<PurchaseOrderDto>> GenerateFromPlansAsync(GeneratePurchaseOrdersFromPlansDto dto);

    /// <summary>
    /// 将采购单草稿标记为已完成，使其可被后续采购入库流程引用。
    /// </summary>
    /// <param name="id">采购单主键。</param>
    /// <returns>完成后的采购单详情。</returns>
    Task<PurchaseOrderDto> CompleteAsync(Guid id);

    /// <summary>
    /// 取消采购单草稿并释放其采购计划数量占用，取消结果保留供追溯。
    /// </summary>
    /// <param name="id">采购单主键。</param>
    /// <returns>取消后的采购单详情。</returns>
    Task<PurchaseOrderDto> CancelAsync(Guid id);
}
