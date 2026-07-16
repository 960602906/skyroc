using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Shared.Common;

namespace Infrastructure.Storage;

/// <summary>
/// 通过 AWSSDK.S3 对接 RustFS 的对象存储实现，强制 Path-Style 寻址。
/// </summary>
public sealed class RustFsObjectStorage : IObjectStorage, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketEnsured;
    private bool _disposed;

    /// <summary>
    /// 使用已绑定的 RustFS 选项创建 S3 客户端。
    /// </summary>
    public RustFsObjectStorage(IOptions<RustFsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.Endpoint))
            throw new InvalidOperationException("必须配置 RustFS:Endpoint");
        if (string.IsNullOrWhiteSpace(value.BucketName))
            throw new InvalidOperationException("必须配置 RustFS:BucketName");
        if (string.IsNullOrWhiteSpace(value.AccessKey) || string.IsNullOrWhiteSpace(value.SecretKey))
            throw new InvalidOperationException("必须通过环境变量配置 RustFS:AccessKey 与 RustFS:SecretKey");

        _bucketName = value.BucketName.Trim();
        var serviceUrl = NormalizeEndpoint(value.Endpoint, value.UseSsl);
        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = value.ForcePathStyle,
            AuthenticationRegion = string.IsNullOrWhiteSpace(value.Region) ? "us-east-1" : value.Region.Trim()
        };
        _client = new AmazonS3Client(new BasicAWSCredentials(value.AccessKey, value.SecretKey), config);
    }

    /// <inheritdoc />
    public async Task PutAsync(string key, Stream content, string contentType, long length, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);
        await EnsureBucketAsync(cancellationToken).ConfigureAwait(false);
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            AutoResetStreamPosition = false
        };
        request.Headers.ContentLength = length;
        await _client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await EnsureBucketAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var response = await _client.GetObjectAsync(_bucketName, key, cancellationToken).ConfigureAwait(false);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound
                                                   || string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException("对象不存在", key, exception);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await EnsureBucketAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _client.DeleteObjectAsync(_bucketName, key, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound
                                                   || string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            // 补偿删除时对象可能已不存在，视为成功。
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await EnsureBucketAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _client.GetObjectMetadataAsync(_bucketName, key, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound
                                                   || string.Equals(exception.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase)
                                                   || string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _client.Dispose();
        _bucketLock.Dispose();
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketEnsured) return;
        await _bucketLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_bucketEnsured) return;
            try
            {
                await _client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _bucketName,
                    UseClientRegion = true
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonS3Exception exception) when (
                string.Equals(exception.ErrorCode, "BucketAlreadyOwnedByYou", StringComparison.OrdinalIgnoreCase)
                || string.Equals(exception.ErrorCode, "BucketAlreadyExists", StringComparison.OrdinalIgnoreCase)
                || exception.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // Bucket 已存在。
            }

            _bucketEnsured = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    private static string NormalizeEndpoint(string endpoint, bool useSsl)
    {
        var trimmed = endpoint.Trim().TrimEnd('/');
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var scheme = useSsl ? "https" : "http";
        return $"{scheme}://{trimmed}";
    }
}
