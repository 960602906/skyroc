namespace Shared.Common;

/// <summary>
///     开发环境默认种子配置（仅管理员账户）。
/// </summary>
public class DevSeedOptions
{
    public const string SectionName = "DevSeed";

    /// <summary>
    ///     是否启用开发种子账号
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     管理员用户名
    /// </summary>
    public string AdminUsername { get; set; } = "admin";

    /// <summary>
    ///     管理员初始密码
    /// </summary>
    public string AdminPassword { get; set; } = string.Empty;
}
