using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Domain.Entities.AI;

/// <summary>
/// 生成通用操作草稿的规范化 JSON 和绑定用户、operationId、参数的不可变哈希。
/// </summary>
public static class AiActionDraftIntegrity
{
    /// <summary>
    /// 将业务参数转为属性按序、无额外空白且数字格式稳定的 JSON。
    /// </summary>
    public static string CanonicalizeArguments(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
            throw new ArgumentException("通用操作参数 JSON 不能为空。", nameof(argumentsJson));

        using var document = JsonDocument.Parse(argumentsJson);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }))
        {
            WriteCanonicalElement(writer, document.RootElement);
            writer.Flush();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// 计算绑定草稿所属用户、operationId 与规范化参数的 SHA-256 十六进制哈希。
    /// </summary>
    public static string ComputeHash(Guid userId, string operationId, string canonicalArgumentsJson)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("草稿所属用户不能为空。", nameof(userId));
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentException("operationId 不能为空。", nameof(operationId));
        if (string.IsNullOrWhiteSpace(canonicalArgumentsJson))
            throw new ArgumentException("规范化参数 JSON 不能为空。", nameof(canonicalArgumentsJson));

        var source = string.Create(
            CultureInfo.InvariantCulture,
            $"{userId:D}\n{operationId.Trim()}\n{canonicalArgumentsJson}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
    }

    /// <summary>
    /// 使用固定时间比较确认请求是否仍与原草稿的用户、operationId 和参数完全一致。
    /// </summary>
    public static bool Matches(
        string storedHash,
        Guid currentUserId,
        string operationId,
        string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || storedHash.Length != 64)
            return false;

        try
        {
            var canonicalArguments = CanonicalizeArguments(argumentsJson);
            var currentHash = ComputeHash(currentUserId, operationId, canonicalArguments);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(storedHash),
                Convert.FromHexString(currentHash));
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void WriteCanonicalElement(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                var properties = element.EnumerateObject()
                    .OrderBy(item => item.Name, StringComparer.Ordinal)
                    .ToArray();
                for (var index = 1; index < properties.Length; index++)
                {
                    if (string.Equals(properties[index - 1].Name, properties[index].Name, StringComparison.Ordinal))
                        throw new JsonException($"通用操作参数包含重复属性 '{properties[index].Name}'。");
                }

                foreach (var property in properties)
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalElement(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteCanonicalElement(writer, item);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                WriteCanonicalNumber(writer, element);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new JsonException("通用操作参数包含不支持的 JSON 值。");
        }
    }

    private static void WriteCanonicalNumber(Utf8JsonWriter writer, JsonElement element)
    {
        if (element.TryGetInt64(out var integer))
        {
            writer.WriteNumberValue(integer);
            return;
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            writer.WriteRawValue(decimalValue.ToString("G29", CultureInfo.InvariantCulture));
            return;
        }

        var doubleValue = element.GetDouble();
        if (!double.IsFinite(doubleValue))
            throw new JsonException("通用操作参数数字必须是有限值。");
        writer.WriteRawValue(doubleValue.ToString("R", CultureInfo.InvariantCulture));
    }
}
