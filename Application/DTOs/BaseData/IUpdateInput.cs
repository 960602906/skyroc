using Shared.Constants;

namespace Application.DTOs;

/// <summary>
///     带主键的基础资料更新模型。
/// </summary>
public interface IUpdateInput : INamedCodeInput, IHasId
{
}

