namespace Application.Serialization;

/// <summary>
/// 统一日期时间格式与 UTC 规范化（查询绑定 / 表达式比较共用）。
/// API JSON 使用 System.Text.Json 默认 ISO 8601 行为；前端请求须传带 Z 的 UTC。
/// </summary>
public static class DateTimeJsonFormats
{
    /// <summary>
    /// query/form 绑定可解析的时间字符串格式。
    /// </summary>
    public static readonly string[] Supported =
    [
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
        "O"
    ];

    /// <summary>
    /// 将解析后的时间规范为 UTC，满足 Npgsql 对 timestamp with time zone 的写入要求。
    /// 无时区信息的业务日期（如 yyyy-MM-dd）按 UTC 午夜处理。
    /// </summary>
    public static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// 可空时间规范为 UTC；null 保持 null。
    /// </summary>
    public static DateTime? NormalizeToUtc(DateTime? value)
    {
        return value.HasValue ? NormalizeToUtc(value.Value) : null;
    }

    /// <summary>
    /// 查询起始时间（含）：规范为 UTC。
    /// </summary>
    public static DateTime? AsUtcQueryStart(DateTime? value)
    {
        return NormalizeToUtc(value);
    }

    /// <summary>
    /// 查询截止时间（含）：规范为 UTC。
    /// 若为日期-only（时分秒毫秒均为 0），扩展到当天 23:59:59.9999999，
    /// 以便前端传 yyyy-MM-dd 时能覆盖整天。
    /// </summary>
    public static DateTime? AsUtcQueryEndInclusive(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        var utc = NormalizeToUtc(value.Value);
        if (utc is { Hour: 0, Minute: 0, Second: 0, Millisecond: 0 })
            return utc.AddDays(1).AddTicks(-1);

        return utc;
    }
}
