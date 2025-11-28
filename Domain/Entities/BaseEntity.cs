
using Common.Constants;

namespace Domain.Entities;

/// <summary>
///     基础实体类 - 所有实体都继承此类
///     包含通用的审计字段和元数据
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    ///     主键ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    ///     创建人ID (可选)
    /// </summary>
    public Guid? CreateBy { get; set; }
    
    /// <summary>
    ///   创建人名称 (可选)
    /// </summary>
    public string? CreateName { get; set; }
    
    /// <summary>
    ///     最后修改时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    ///     最后修改人ID (可选)
    /// </summary>
    public Guid? UpdateBy { get; set; }
    
    /// <summary>
    ///   最后修改人名称 (可选)
    /// </summary>
    public string? UpdateName { get; set; }
    
    /// <summary>
    ///     启用禁用状态
    /// </summary>
    public Status Status { get; set; } = Status.Enable;
}