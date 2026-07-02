namespace Application.DTOs.Purchases;

/// <summary>
///     更新供应商 DTO。
/// </summary>
public class UpdateSupplierDto : CreateSupplierDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

