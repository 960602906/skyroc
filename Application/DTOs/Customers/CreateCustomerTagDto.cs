namespace Application.DTOs.Customers;

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

