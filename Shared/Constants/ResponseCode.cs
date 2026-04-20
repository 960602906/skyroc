namespace Shared.Constants;

/// <summary>
///     API 响应状态码
/// </summary>
public enum ResponseCode
{
    // ========================================
    // 成功 (200)
    // ========================================

    /// <summary>
    ///     操作成功
    /// </summary>
    Success = 200,

    // ========================================
    // 客户端错误 (400-499)
    // ========================================

    /// <summary>
    ///     请求参数错误
    /// </summary>
    BadRequest = 400,

    /// <summary>
    ///     未授权（未登录）
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    ///     认证失败
    /// </summary>
    AuthenticationFailed = 402,

    /// <summary>
    ///     无权限访问
    /// </summary>
    Forbidden = 403,

    /// <summary>
    ///     资源不存在
    /// </summary>
    NotFound = 404,

    /// <summary>
    ///     数据已存在
    /// </summary>
    Conflict = 409,

    /// <summary>
    ///     参数验证失败
    /// </summary>
    ValidationError = 422,

    // ========================================
    // 服务器错误 (500-599)
    // ========================================

    /// <summary>
    ///     服务器内部错误
    /// </summary>
    InternalError = 500,

    /// <summary>
    ///     业务逻辑错误
    /// </summary>
    BusinessError = 501,

    /// <summary>
    ///     数据库操作失败
    /// </summary>
    DatabaseError = 502
}