namespace Shared.Common;

/// <summary>
///     天眼查开放平台配置。
/// </summary>
public class TianyanchaOptions
{
    public const string SectionName = "Tianyancha";

    /// <summary>
    ///     是否启用天眼查补全。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     天眼查开放平台根地址。
    /// </summary>
    public string BaseUrl { get; set; } = "http://open.api.tianyancha.com";

    /// <summary>
    ///     天眼查授权 Token。
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    ///     企业搜索接口路径。
    /// </summary>
    public string SearchPath { get; set; } = "/services/open/search/2.0";

    /// <summary>
    ///     企业基本信息接口路径。
    /// </summary>
    public string BaseInfoPath { get; set; } = "/services/open/ic/baseinfoV2/2.0";

    /// <summary>
    ///     请求超时时间，单位秒。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}
