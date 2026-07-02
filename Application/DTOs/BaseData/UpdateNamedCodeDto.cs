using Shared.Constants;

namespace Application.DTOs;

/// <summary>
///     基础资料更新 DTO 基类。
/// </summary>
public abstract class UpdateNamedCodeDto : CreateNamedCodeDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

