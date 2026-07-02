namespace Application.DTOs.Storage;

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

