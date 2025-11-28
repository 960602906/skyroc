using Common.Constants;

namespace Application.DTOs;

/// <summary>
///     DTO基类 - 所有DTO的基类
/// </summary>
public abstract class BaseDto
{
    /// <summary>
    ///     主键ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     创建时间
    /// </summary>
    public DateTime? CreatedTime { get; set; }

    /// <summary>
    ///     创建人ID (可选)
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    ///     最后修改时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    ///     最后修改人ID (可选)
    /// </summary>
    public Guid? UpdateBy { get; set; }

    /// <summary>
    ///     启用禁用状态
    /// </summary>
    public Status? Status { get; set; }
}