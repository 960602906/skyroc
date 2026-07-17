using Application.DTOs.Finance;
using Application.QueryParameters.Finance;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 供应商结算应用服务，负责待结单据查询、结算单创建、作废和单据余额回写。
/// </summary>
public interface ISupplierSettlementService
{
    /// <summary>按条件分页查询供应商待结单据，支持待结余额筛选。</summary>
    Task<PagedResult<SupplierBillDto>> GetBillsAsync(SupplierBillQueryParameters parameters);

    /// <summary>按条件分页查询供应商结算单。</summary>
    Task<PagedResult<SupplierSettlementDto>> GetPagedAsync(SupplierSettlementQueryParameters parameters);

    /// <summary>读取供应商结算单明细。</summary>
    Task<SupplierSettlementDto> GetByIdAsync(Guid id);

    /// <summary>创建供应商结算单并原子回写所选待结单据的已结金额和状态。</summary>
    Task<SupplierSettlementDto> CreateAsync(CreateSupplierSettlementDto dto);

    /// <summary>作废供应商结算单并原子回滚对应待结单据的已结金额和状态。</summary>
    Task<SupplierSettlementDto> VoidAsync(Guid id, VoidSupplierSettlementDto dto);
}
