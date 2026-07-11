namespace Application.Services.System;

/// <summary>统一审计字段的空值回退、首尾空白清理和数据库长度限制。</summary>
internal static class AuditTextSanitizer
{
    /// <summary>规范化必填文本，空值使用指定安全摘要。</summary>
    public static string Required(string? value, int maxLength, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }

    /// <summary>规范化可空文本，空白值返回空，其他值按上限裁剪。</summary>
    public static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }
}
