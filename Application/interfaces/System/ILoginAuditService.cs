namespace Application.interfaces.System;

/// <summary>定义认证服务写入成功或失败登录审计记录的用例。</summary>
public interface ILoginAuditService
{
    /// <summary>记录一次登录验证结果；实现不得持久化请求密码或任何访问令牌。</summary>
    /// <param name="username">请求中提交的登录名。</param>
    /// <param name="userId">已匹配用户主键；用户不存在时为空。</param>
    /// <param name="isSuccess">登录是否验证成功。</param>
    /// <param name="failureReason">失败原因安全摘要；成功时为空。</param>
    Task RecordAsync(string username, Guid? userId, bool isSuccess, string? failureReason);
}
