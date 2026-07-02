namespace Application.DTOs.Customers;

/// <summary>
///     更新公司 DTO。
/// </summary>
public class UpdateCompanyDto : CreateCompanyDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

