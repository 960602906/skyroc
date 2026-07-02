using Shared.Constants;

namespace Application.DTOs.Customers;

/// <summary>
///     更新客户子账号 DTO。
/// </summary>
public class UpdateCustomerSubAccountDto : CreateCustomerSubAccountDto, IHasId
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

