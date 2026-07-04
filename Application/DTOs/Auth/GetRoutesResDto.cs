namespace Application.DTOs.Auth;

/// <summary>
///     动态路由与默认首页响应。
/// </summary>
public class GetRoutesResDto
{
    /// <summary>
    ///     当前用户可访问的前端路由树。
    /// </summary>
    public List<RoutesDto> Routes { get; set; } = [];

    /// <summary>
    ///     登录后的默认首页路径。
    /// </summary>
    public string Home { get; set; } = "/home";
}
