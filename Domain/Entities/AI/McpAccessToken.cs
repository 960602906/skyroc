namespace Domain.Entities.AI;

/// <summary>
/// 用户个人 MCP 访问令牌元数据，只保存可识别前缀和 HMAC-SHA256 哈希，不保存令牌原文。
/// </summary>
public class McpAccessToken : BaseEntity
{
    /// <summary>
    /// 令牌所属系统用户主键。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户为令牌设置的用途名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 用于列表识别和令牌定位的非敏感随机前缀。
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// 使用服务端独立密钥计算的 HMAC-SHA256 十六进制哈希。
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// 以空格分隔并按稳定顺序保存的授权范围集合。
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// 令牌失效时间（UTC）。
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 令牌被用户撤销的时间（UTC）；未撤销时为空。
    /// </summary>
    public DateTime? RevokedTime { get; set; }

    /// <summary>
    /// 令牌最近一次通过鉴权的时间（UTC），允许按最短间隔延迟更新。
    /// </summary>
    public DateTime? LastUsedTime { get; set; }

    /// <summary>
    /// 令牌所属系统用户。
    /// </summary>
    public virtual User User { get; set; } = null!;
}
