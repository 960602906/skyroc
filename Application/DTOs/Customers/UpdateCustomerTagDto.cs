namespace Application.DTOs.Customers;

/// <summary>
///     更新客户标签 DTO。
/// </summary>
public class UpdateCustomerTagDto : CreateCustomerTagDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

