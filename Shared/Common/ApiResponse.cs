using Shared.Constants;

namespace Shared.Common;

/// <summary>
///     API 统一响应模型
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    ///     响应消息
    /// </summary>
    public string? Msg { get; set; }

    /// <summary>
    ///     响应数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    ///     响应代码
    /// </summary>
    public ResponseCode Code { get; set; }


    // ========================================
    // 静态快捷方法
    // ========================================

    /// <summary>
    ///     成功响应
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Code = ResponseCode.Success,
            Msg = message,
            Data = data
        };
    }

    /// <summary>
    ///     成功响应（无数据）
    /// </summary>
    public static ApiResponse<T> Ok(string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Code = ResponseCode.Success,
            Msg = message,
            Data = default
        };
    }

    /// <summary>
    ///     失败响应
    /// </summary>
    public static ApiResponse<T> Fail(string message)
    {
        return new ApiResponse<T>
        {
            Code = ResponseCode.BusinessError,
            Msg = message,
            Data = default
        };
    }

    /// <summary>
    ///     失败响应
    /// </summary>
    public static ApiResponse<T> Fail(string message, T data)
    {
        return new ApiResponse<T>
        {
            Code = ResponseCode.BusinessError,
            Msg = message,
            Data = data
        };
    }

    /// <summary>
    ///     参数错误
    /// </summary>
    public static ApiResponse<T> BadRequest(string message = "请求参数错误")
    {
        return new ApiResponse<T>
        {
            Code = ResponseCode.BadRequest,
            Msg = message,
            Data = default
        };
    }

    /// <summary>
    ///     未授权
    /// </summary>
    public static ApiResponse<T> Unauthorized(string message = "未授权访问")
    {
        return new ApiResponse<T>
        {
            Code = ResponseCode.Unauthorized,
            Msg = message,
            Data = default
        };
    }


    /// <summary>
    ///     无权限
    /// </summary>
    public static ApiResponse<T> Forbidden(string message = "无权限访问")
    {
        return new ApiResponse<T>
        {
            Code = ResponseCode.Forbidden,
            Msg = message,
            Data = default
        };
    }

    /// <summary>
    ///     资源不存在
    /// </summary>
    public static ApiResponse<T> NotFound(string message = "资源不存在")
    {
        return new ApiResponse<T>
        {
            Code = ResponseCode.NotFound,
            Msg = message,
            Data = default
        };
    }

    /// <summary>
    ///     服务器错误
    /// </summary>
    public static ApiResponse<T> Error(string message = "服务器内部错误")
    {
        return new ApiResponse<T>
        {
            Code = ResponseCode.InternalError,
            Msg = message,
            Data = default
        };
    }
}