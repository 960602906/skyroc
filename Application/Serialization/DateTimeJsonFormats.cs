namespace Application.Serialization;

internal static class DateTimeJsonFormats
{
    public const string Default = "yyyy-MM-dd HH:mm:ss";

    public static readonly string[] Supported =
    [
        Default,
        "yyyy-MM-dd",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
        "O"
    ];

    /// <summary>
    ///     将解析后的时间规范为 UTC，满足 Npgsql 对 timestamp with time zone 的写入要求。
    ///     无时区信息的业务日期（如 yyyy-MM-dd）按 UTC 午夜处理。
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
}
