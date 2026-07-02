using Shared.Constants;

namespace Application.DTOs;

/// <summary>
///     带名称和编码的基础资料输入模型。
/// </summary>
public interface INamedCodeInput
{
    /// <summary>
    ///     名称。
    /// </summary>
    string? Name { get; set; }

    /// <summary>
    ///     编码。
    /// </summary>
    string? Code { get; set; }
}

