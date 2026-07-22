namespace Shared.Options;

/// <summary>单个模型 Provider 的安全配置；只保存密钥环境变量名称。</summary>
public sealed class AiProviderOptions
{
    /// <summary>匹配已注册 <c>IAiModelProvider.AdapterName</c> 的名称。</summary>
    public string Adapter { get; set; } = string.Empty;
    /// <summary>模型服务的绝对 HTTP/HTTPS 基础地址。</summary>
    public string BaseUrl { get; set; } = string.Empty;
    /// <summary>部署环境中启用的模型标识。</summary>
    public string Model { get; set; } = string.Empty;
    /// <summary>保存 API Key 的环境变量名称；不得填写 Key 值。</summary>
    public string ApiKeyEnvironmentVariable { get; set; } = string.Empty;
}
