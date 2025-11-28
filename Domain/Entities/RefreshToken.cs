namespace Domain.Entities;

/// <summary>
///     刷新令牌实体
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    ///     用户 ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     刷新令牌值
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    ///     过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    ///     是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    ///     撤销时间
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    // 导航属性
    public User User { get; set; } = null!;
}