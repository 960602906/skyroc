namespace Application.DTOs.Purchases;

/// <summary>
///     供应商 DTO。
/// </summary>
public class SupplierDto : BaseDto
{
    /// <summary>
    ///     供应商名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     供应商编码。
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
    ///     开户行。
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    ///     银行账号。
    /// </summary>
    public string? BankAccount { get; set; }

    /// <summary>
    ///     税号。
    /// </summary>
    public string? TaxNo { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
///     创建供应商 DTO。
/// </summary>
public class CreateSupplierDto : CreateNamedCodeDto
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

    /// <summary>
    ///     开户行。
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    ///     银行账号。
    /// </summary>
    public string? BankAccount { get; set; }

    /// <summary>
    ///     税号。
    /// </summary>
    public string? TaxNo { get; set; }
}

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
