namespace Application.DTOs.Purchases;

/// <summary>
///     更新采购规则 DTO。
/// </summary>
public class UpdatePurchaseRuleDto : CreatePurchaseRuleDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

