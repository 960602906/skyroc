namespace Shared.Common;

/// <summary>
/// RustFS（S3 兼容）对象存储连接配置；AccessKey/SecretKey 应通过环境变量注入，勿写入仓库明文。
/// </summary>
public class RustFsOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "RustFS";

    /// <summary>
    /// S3 API 端点，含协议，例如 <c>http://127.0.0.1:9000</c>。
    /// </summary>
    public string Endpoint { get; set; } = "http://127.0.0.1:9000";

    /// <summary>
    /// Access Key；生产环境通过 <c>RustFS__AccessKey</c> 注入。
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret Key；生产环境通过 <c>RustFS__SecretKey</c> 注入。
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 业务文件所在 Bucket 名称。
    /// </summary>
    public string BucketName { get; set; } = "skyroc";

    /// <summary>
    /// 签名用 Region；RustFS 通常不校验，默认 <c>us-east-1</c>。
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// 是否强制 Path-Style 寻址；对接 RustFS 时必须为 <c>true</c>。
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// 端点是否使用 HTTPS；当前内网部署可为 <c>false</c>。
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// 为 <c>true</c> 时使用进程内内存对象存储，供自动化测试使用，不连接真实 RustFS。
    /// </summary>
    public bool UseInMemory { get; set; }
}
