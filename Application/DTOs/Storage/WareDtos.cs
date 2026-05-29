namespace Application.DTOs.Storage;

/// <summary>
///     仓库 DTO。
/// </summary>
public class WareDto : BaseDto
{
    /// <summary>
    ///     仓库名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     仓库编码。
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
    ///     仓库地址。
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    ///     排序值。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
///     创建仓库 DTO。
/// </summary>
public class CreateWareDto : CreateNamedCodeDto
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
    ///     仓库地址。
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    ///     排序值。
    /// </summary>
    public int Sort { get; set; }
}

/// <summary>
///     更新仓库 DTO。
/// </summary>
public class UpdateWareDto : CreateWareDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}
