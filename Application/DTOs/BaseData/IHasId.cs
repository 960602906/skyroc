using Shared.Constants;

namespace Application.DTOs;

/// <summary>
///     带主键的输入模型。
/// </summary>
public interface IHasId
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    Guid Id { get; set; }
}

