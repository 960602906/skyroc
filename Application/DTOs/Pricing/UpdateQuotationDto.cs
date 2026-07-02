using Application.DTOs.Goods;
using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Pricing;

/// <summary>
///     更新报价单 DTO。
/// </summary>
public class UpdateQuotationDto : CreateQuotationDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

