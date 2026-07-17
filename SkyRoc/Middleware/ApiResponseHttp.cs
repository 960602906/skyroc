using Shared.Constants;

namespace SkyRoc.Middleware;

/// <summary>
///     约定：业务接口受理后 HTTP 状态码固定为 200，业务结果码写在响应体 <c>code</c>；
///     本类型在 <see cref="HttpContext.Items"/> 中同步记录业务码，供审计等中间件读取。
/// </summary>
internal static class ApiResponseHttp
{
    /// <summary>在 Items 中存放当前响应业务码的键。</summary>
    public const string BusinessCodeItemKey = "SkyRoc.ApiResponse.Code";

    /// <summary>将业务码写入当前请求上下文，供后续中间件读取。</summary>
    public static void MarkBusinessCode(HttpContext context, ResponseCode code) =>
        context.Items[BusinessCodeItemKey] = code;

    /// <summary>读取当前请求已标记的业务码；尚未写入时返回 null。</summary>
    public static ResponseCode? GetBusinessCode(HttpContext context) =>
        context.Items.TryGetValue(BusinessCodeItemKey, out var value) && value is ResponseCode code
            ? code
            : null;
}
