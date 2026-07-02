using Shared.Constants;

namespace Application.DTOs;

/// <summary>
///     树形 DTO 节点。
/// </summary>
public interface ITreeNodeDto<TNode>
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    ///     父级 ID。
    /// </summary>
    Guid? ParentId { get; set; }

    /// <summary>
    ///     排序值。
    /// </summary>
    int Sort { get; set; }

    /// <summary>
    ///     子节点集合。
    /// </summary>
    List<TNode>? Children { get; set; }
}

