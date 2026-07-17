using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Common;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Common;

/// <summary>
///     集成测试辅助：业务接口 HTTP 固定 200，断言响应体业务码。
/// </summary>
public static class ApiHttpAssert
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>断言 HTTP 200，且 body.code 等于期望业务码。</summary>
    public static async Task AssertBusinessCodeAsync(
        HttpResponseMessage response,
        ResponseCode expectedCode)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await response.Content.LoadIntoBufferAsync();
        var payload = await ReadApiResponseAsync(response);
        Assert.Equal(expectedCode, payload.Code);
    }

    /// <summary>读取响应体中的业务码；要求 HTTP 200。</summary>
    public static async Task<ResponseCode> ReadBusinessCodeAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await response.Content.LoadIntoBufferAsync();
        var payload = await ReadApiResponseAsync(response);
        return payload.Code;
    }

    private static async Task<ApiResponse<object?>> ReadApiResponseAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<ApiResponse<object?>>(stream, JsonOptions);
        Assert.NotNull(payload);
        return payload!;
    }
}
