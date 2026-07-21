using Application.DTOs;
using Application.DTOs.Orders;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 销售订单应用服务。
/// </summary>
public interface ISaleOrderService
{
    /// <summary>
    /// 按业务条件分页查询销售订单。
    /// </summary>
    Task<PagedResult<SaleOrderDto>> GetPagedAsync(SaleOrderQueryParameters parameters);

    /// <summary>
    /// 读取销售订单详情、商品明细和审核轨迹。
    /// </summary>
    Task<SaleOrderDto> GetByIdAsync(Guid id);

    /// <summary>
    /// 创建销售订单并记录首次提交审核轨迹。
    /// </summary>
    Task<SaleOrderDto> CreateAsync(CreateSaleOrderDto dto);

    /// <summary>
    /// 更新待审核或已驳回订单及其商品明细。
    /// </summary>
    Task<SaleOrderDto> UpdateAsync(UpdateSaleOrderDto dto);

    /// <summary>
    /// 删除允许删除的销售订单及其拥有记录。
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// 审核通过待审核销售订单。
    /// </summary>
    Task<SaleOrderDto> ApproveAsync(Guid id, string? remark);

    /// <summary>
    /// 驳回待审核销售订单并记录原因。
    /// </summary>
    Task<SaleOrderDto> RejectAsync(Guid id, string? remark);

    /// <summary>
    /// 将已驳回销售订单重新提交审核。
    /// </summary>
    Task<SaleOrderDto> ResubmitAsync(Guid id, string? remark);

    /// <summary>
    ///     按订单号限量搜索销售订单选择项，不执行总数统计。
    /// </summary>
    Task<SelectionOptionSearchResultDto> SearchSelectionOptionsAsync(SelectionOptionSearchQueryParameters parameters);

    /// <summary>
    ///     按已选主键集合恢复订单号和客户名称。
    /// </summary>
    Task<List<SelectionOptionDto>> ResolveSelectionOptionsAsync(IReadOnlyCollection<Guid> ids);
}
