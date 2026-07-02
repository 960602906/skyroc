using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Pricing;

/// <summary>
///     更新客户协议价 DTO。
/// </summary>
public class UpdateCustomerProtocolDto : CreateCustomerProtocolDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

