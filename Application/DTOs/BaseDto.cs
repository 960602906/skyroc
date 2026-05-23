using Application.Serialization;
using Shared.Constants;
using System.Text.Json.Serialization;

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
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? CreateTime { get; set; }

    /// <summary>
    ///     创建人ID (可选)
    /// </summary>
    public Guid? CreateBy { get; set; }

    /// <summary>
    ///     创建人名称 (可选)
    /// </summary>
    public string? CreateName { get; set; }

    /// <summary>
    ///     最后修改时间
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    ///     最后修改人ID (可选)
    /// </summary>
    public Guid? UpdateBy { get; set; }

    /// <summary>
    ///     最后修改人名称 (可选)
    /// </summary>
    public string? UpdateName { get; set; }

    /// <summary>
    ///     启用禁用状态
    /// </summary>
    public Status? Status { get; set; }
}
