using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 入库应用服务，提供采购入库、其他入库和销售退货入库的维护、查询及审核、反审核能力。
/// </summary>
public interface IStockInService
{
    /// <summary>
    /// 按入库类型分页查询入库单及商品明细。
    /// </summary>
    /// <param name="orderType">入库业务类型：采购、其他或销售退货。</param>
    /// <param name="parameters">入库单分页与筛选参数。</param>
    /// <returns>按入库时间倒序排列的入库单分页结果。</returns>
    Task<PagedResult<StockInOrderDto>> GetPagedAsync(
        Domain.Entities.Storage.StockInOrderType orderType,
        StockInOrderQueryParameters parameters);

    /// <summary>
    /// 按入库类型读取入库单详情，包含商品明细快照。
    /// </summary>
    /// <param name="orderType">入库业务类型。</param>
    /// <param name="id">入库单主键。</param>
    /// <returns>入库单完整详情。</returns>
    Task<StockInOrderDto> GetByIdAsync(
        Domain.Entities.Storage.StockInOrderType orderType,
        Guid id);

    /// <summary>
    /// 创建采购入库草稿并保存仓库、供应商、采购员和商品单位、批次、成本快照。
    /// </summary>
    /// <param name="dto">采购入库创建请求。</param>
    /// <returns>创建后的采购入库单详情。</returns>
    Task<StockInOrderDto> CreatePurchaseAsync(CreatePurchaseStockInDto dto);

    /// <summary>
    /// 编辑采购入库草稿的主单字段与全部商品行。
    /// </summary>
    /// <param name="dto">采购入库整单替换请求。</param>
    /// <returns>更新后的采购入库单详情。</returns>
    Task<StockInOrderDto> UpdatePurchaseAsync(UpdatePurchaseStockInDto dto);

    /// <summary>
    /// 创建其他入库草稿，由授权人员手工增加库存。
    /// </summary>
    /// <param name="dto">其他入库创建请求。</param>
    /// <returns>创建后的其他入库单详情。</returns>
    Task<StockInOrderDto> CreateOtherAsync(CreateOtherStockInDto dto);

    /// <summary>
    /// 编辑其他入库草稿的主单字段与全部商品行。
    /// </summary>
    /// <param name="dto">其他入库整单替换请求。</param>
    /// <returns>更新后的其他入库单详情。</returns>
    Task<StockInOrderDto> UpdateOtherAsync(UpdateOtherStockInDto dto);

    /// <summary>
    /// 创建销售退货入库草稿；关联已完成取货任务时校验来源快照并按任务集合幂等返回。
    /// </summary>
    /// <param name="dto">销售退货入库创建请求。</param>
    /// <returns>创建后的销售退货入库单详情。</returns>
    Task<StockInOrderDto> CreateSalesReturnAsync(CreateSalesReturnStockInDto dto);

    /// <summary>
    /// 编辑销售退货入库草稿的主单字段与全部商品行。
    /// </summary>
    /// <param name="dto">销售退货入库整单替换请求。</param>
    /// <returns>更新后的销售退货入库单详情。</returns>
    Task<StockInOrderDto> UpdateSalesReturnAsync(UpdateSalesReturnStockInDto dto);

    /// <summary>
    /// 删除指定类型的入库单草稿；仅草稿或待审核状态可删除。
    /// </summary>
    /// <param name="orderType">入库业务类型。</param>
    /// <param name="id">入库单主键。</param>
    /// <returns>删除成功返回 <c>true</c>。</returns>
    Task<bool> DeleteAsync(
        Domain.Entities.Storage.StockInOrderType orderType,
        Guid id);

    /// <summary>
    /// 审核入库单，原子创建或更新库存批次并只追加正向流水以增加库存。
    /// </summary>
    /// <param name="orderType">入库业务类型。</param>
    /// <param name="id">入库单主键。</param>
    /// <param name="remark">审核说明，写入生成流水备注。</param>
    /// <returns>审核后的入库单详情。</returns>
    Task<StockInOrderDto> AuditAsync(
        Domain.Entities.Storage.StockInOrderType orderType,
        Guid id,
        string? remark);

    /// <summary>
    /// 反审核已审核入库单，按原正向流水生成反向流水回滚批次数量。
    /// </summary>
    /// <param name="orderType">入库业务类型。</param>
    /// <param name="id">入库单主键。</param>
    /// <param name="remark">反审核原因，写入生成反向流水备注。</param>
    /// <returns>反审核后的入库单详情。</returns>
    Task<StockInOrderDto> ReverseAuditAsync(
        Domain.Entities.Storage.StockInOrderType orderType,
        Guid id,
        string? remark);

    /// <summary>
    /// 根据入库单号查询入库单详情，包含商品明细快照。
    /// </summary>
    /// <param name="orderType">入库业务类型。</param>
    /// <param name="inNo">入库单号。</param>
    /// <returns>入库单完整详情。</returns>
    /// <exception cref="Application.Exceptions.BusinessException">入库单号不存在或类型不匹配时抛出。</exception>
    Task<StockInOrderDto> GetByInNoAsync(
        Domain.Entities.Storage.StockInOrderType orderType,
        string inNo);
}
