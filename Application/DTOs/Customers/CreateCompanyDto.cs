namespace Application.DTOs.Customers;

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

