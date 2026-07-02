namespace Application.interfaces;

/// <summary>
///     企业工商信息提供者。
/// </summary>
public interface ICompanyInfoProvider
{
    /// <summary>
    ///     按企业名称或关键字查询工商信息。
    /// </summary>
    Task<CompanyBusinessInfo?> GetCompanyInfoAsync(string keyword, CancellationToken cancellationToken = default);
}

