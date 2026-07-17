using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 业务单据编号生成器，按种类统一生成「前缀 + UTC 时间戳 + Guid 后缀」并查重重试。
/// </summary>
public interface IDocumentNoGenerator
{
    /// <summary>
    /// 生成指定种类的唯一业务编号。
    /// </summary>
    /// <param name="kind">单据编号种类，决定前缀、后缀长度或最大长度，以及失败提示。</param>
    /// <param name="existsCheck">返回 <c>true</c> 表示编号已被占用，生成器将重试。</param>
    /// <returns>未占用的业务编号。</returns>
    /// <exception cref="Exceptions.BusinessException">连续重试仍冲突时抛出。</exception>
    Task<string> NextAsync(DocumentNoKind kind, Func<string, Task<bool>> existsCheck);
}
