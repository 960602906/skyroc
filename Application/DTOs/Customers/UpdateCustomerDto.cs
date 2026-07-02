using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Customers;

/// <summary>
///     更新客户 DTO。
/// </summary>
public class UpdateCustomerDto : CreateCustomerDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

