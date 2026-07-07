using Application.DTOs.Finance;
using Application.QueryParameters.Finance;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 客户结款应用服务，负责待结账单查询、结款凭证创建、作废和账单余额回写。
/// </summary>
public interface ICustomerSettlementService
{
    /// <summary>按条件分页查询客户账单，支持待结余额筛选。</summary>
    Task<PagedResult<CustomerBillDto>> GetBillsAsync(CustomerBillQueryParameters parameters);

    /// <summary>按条件分页查询客户结款凭证。</summary>
    Task<PagedResult<CustomerSettlementDto>> GetPagedAsync(CustomerSettlementQueryParameters parameters);

    /// <summary>读取客户结款凭证明细。</summary>
    Task<CustomerSettlementDto> GetByIdAsync(Guid id);

    /// <summary>创建客户结款凭证并原子回写所选客户账单的已结金额和状态。</summary>
    Task<CustomerSettlementDto> CreateAsync(CreateCustomerSettlementDto dto);

    /// <summary>作废客户结款凭证并原子回滚对应账单的已结金额和状态。</summary>
    Task<CustomerSettlementDto> VoidAsync(Guid id, VoidCustomerSettlementDto dto);
}
