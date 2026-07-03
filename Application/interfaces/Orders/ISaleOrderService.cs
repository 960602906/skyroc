using Application.DTOs.Orders;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 销售订单应用服务。
/// </summary>
public interface ISaleOrderService
{
    Task<PagedResult<SaleOrderDto>> GetPagedAsync(SaleOrderQueryParameters parameters);

    Task<SaleOrderDto> GetByIdAsync(Guid id);

    Task<SaleOrderDto> CreateAsync(CreateSaleOrderDto dto);

    Task<SaleOrderDto> UpdateAsync(UpdateSaleOrderDto dto);

    Task<bool> DeleteAsync(Guid id);
}
