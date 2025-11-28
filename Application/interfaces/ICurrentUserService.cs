namespace Application.interfaces;

/// <summary>
///     获取当前登录用户信息
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    ///     用户id
    /// </summary>
    /// <returns></returns>
    Guid? GetUserId();

    /// <summary>
    ///     用户名
    /// </summary>
    /// <returns></returns>
    string? GetUserName();

    /// <summary>
    ///     邮箱
    /// </summary>
    /// <returns></returns>
    string? GetEmail();

    /// <summary>
    ///     角色
    /// </summary>
    /// <returns></returns>
    string? GetRole();

    /// <summary>
    ///     是否存在
    /// </summary>
    /// <param name="claimType"></param>
    /// <param name="claimValue"></param>
    /// <returns></returns>
    bool HasClaim(string claimType, string claimValue);
}