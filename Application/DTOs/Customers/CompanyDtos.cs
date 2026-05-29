namespace Application.DTOs.Customers;

/// <summary>
///     公司 DTO。
/// </summary>
public class CompanyDto : BaseDto
{
    /// <summary>
    ///     公司名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     公司编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     联系人。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    ///     联系电话。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    ///     地址。
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
///     创建公司 DTO。
/// </summary>
public class CreateCompanyDto : CreateNamedCodeDto
{
    /// <summary>
    ///     联系人。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    ///     联系电话。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    ///     地址。
    /// </summary>
    public string? Address { get; set; }
}

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
