namespace Application.DTOs.Customers;

/// <summary>
///     客户标签 DTO。
/// </summary>
public class CustomerTagDto : BaseDto, ITreeNodeDto<CustomerTagDto>
{
    /// <summary>
    ///     标签名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     标签编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     父级标签 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     排序值。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     子标签。
    /// </summary>
    public List<CustomerTagDto>? Children { get; set; }
}

/// <summary>
///     创建客户标签 DTO。
/// </summary>
public class CreateCustomerTagDto : CreateNamedCodeDto
{
    /// <summary>
    ///     父级标签 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     排序值。
    /// </summary>
    public int Sort { get; set; }
}

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
