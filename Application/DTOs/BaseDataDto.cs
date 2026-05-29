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

/// <summary>
///     带主键的基础资料更新模型。
/// </summary>
public interface IUpdateInput : INamedCodeInput, IHasId
{
}

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
