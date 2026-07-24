using Application.DTOs.Storage;
using Application.QueryParameters;
using Domain.Entities.Storage;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 出库应用服务，提供销售、采购退货和其他出库的维护、查询及审核、反审核能力。
/// </summary>
public interface IStockOutService
{
    /// <summary>
    /// 按出库类型分页查询出库单及商品批次明细。
    /// </summary>
    /// <param name="orderType">出库业务类型：销售、采购退货或其他。</param>
    /// <param name="parameters">出库单分页与筛选参数。</param>
    /// <returns>按出库时间倒序排列的出库单分页结果。</returns>
    Task<PagedResult<StockOutOrderDto>> GetPagedAsync(
        StockOutOrderType orderType,
        StockOutOrderQueryParameters parameters);

    /// <summary>
    /// 按出库类型读取出库单详情，包含商品、单位和扣减批次快照。
    /// </summary>
    /// <param name="orderType">出库业务类型。</param>
    /// <param name="id">出库单主键。</param>
    /// <returns>出库单完整详情。</returns>
    Task<StockOutOrderDto> GetByIdAsync(StockOutOrderType orderType, Guid id);

    /// <summary>
    /// 创建销售出库草稿，可关联已审核销售订单或作为手工销售出库。
    /// </summary>
    /// <param name="dto">销售出库创建请求。</param>
    /// <returns>创建后的销售出库单详情。</returns>
    Task<StockOutOrderDto> CreateSaleAsync(CreateSaleStockOutDto dto);

    /// <summary>
    /// 编辑销售出库草稿的来源、业务方和全部商品批次行。
    /// </summary>
    /// <param name="dto">销售出库整单替换请求。</param>
    /// <returns>更新后的销售出库单详情。</returns>
    Task<StockOutOrderDto> UpdateSaleAsync(UpdateSaleStockOutDto dto);

    /// <summary>
    /// 创建采购退货出库草稿，将指定批次商品退还供应商。
    /// </summary>
    /// <param name="dto">采购退货出库创建请求。</param>
    /// <returns>创建后的采购退货出库单详情。</returns>
    Task<StockOutOrderDto> CreatePurchaseReturnAsync(CreatePurchaseReturnStockOutDto dto);

    /// <summary>
    /// 编辑采购退货出库草稿的供应商、仓库和全部商品批次行。
    /// </summary>
    /// <param name="dto">采购退货出库整单替换请求。</param>
    /// <returns>更新后的采购退货出库单详情。</returns>
    Task<StockOutOrderDto> UpdatePurchaseReturnAsync(UpdatePurchaseReturnStockOutDto dto);

    /// <summary>
    /// 创建其他出库草稿，由授权人员手工扣减指定批次库存。
    /// </summary>
    /// <param name="dto">其他出库创建请求。</param>
    /// <returns>创建后的其他出库单详情。</returns>
    Task<StockOutOrderDto> CreateOtherAsync(CreateOtherStockOutDto dto);

    /// <summary>
    /// 编辑其他出库草稿的仓库、部门和全部商品批次行。
    /// </summary>
    /// <param name="dto">其他出库整单替换请求。</param>
    /// <returns>更新后的其他出库单详情。</returns>
    Task<StockOutOrderDto> UpdateOtherAsync(UpdateOtherStockOutDto dto);

    /// <summary>
    /// 删除指定类型的出库单草稿；已审核或已反审核单据不得删除。
    /// </summary>
    /// <param name="orderType">出库业务类型。</param>
    /// <param name="id">出库单主键。</param>
    /// <returns>删除成功返回 <c>true</c>。</returns>
    Task<bool> DeleteAsync(StockOutOrderType orderType, Guid id);

    /// <summary>
    /// 审核出库单，锁定并校验批次可用库存后原子扣减数量并追加负向流水。
    /// </summary>
    /// <param name="orderType">出库业务类型。</param>
    /// <param name="id">出库单主键。</param>
    /// <param name="remark">审核说明，写入生成流水备注。</param>
    /// <returns>审核后的出库单详情。</returns>
    Task<StockOutOrderDto> AuditAsync(StockOutOrderType orderType, Guid id, string? remark);

    /// <summary>
    /// 反审核已审核出库单，按原负向流水追加反向正流水并恢复批次数量。
    /// </summary>
    /// <param name="orderType">出库业务类型。</param>
    /// <param name="id">出库单主键。</param>
    /// <param name="remark">反审核原因，写入生成流水备注。</param>
    /// <returns>反审核后的出库单详情。</returns>
    Task<StockOutOrderDto> ReverseAuditAsync(StockOutOrderType orderType, Guid id, string? remark);

    /// <summary>
    /// 根据出库单号查询出库单详情，包含商品、单位和扣减批次快照。
    /// </summary>
    /// <param name="orderType">出库业务类型。</param>
    /// <param name="outNo">出库单号。</param>
    /// <returns>出库单完整详情。</returns>
    /// <exception cref="Application.Exceptions.BusinessException">出库单号不存在或类型不匹配时抛出。</exception>
    Task<StockOutOrderDto> GetByOutNoAsync(StockOutOrderType orderType, string outNo);
}
