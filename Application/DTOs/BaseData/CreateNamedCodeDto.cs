using Shared.Constants;

namespace Application.DTOs;

/// <summary>
///     基础资料创建 DTO 基类。
/// </summary>
public abstract class CreateNamedCodeDto : INamedCodeInput
{
    /// <summary>
    ///     名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }
}

