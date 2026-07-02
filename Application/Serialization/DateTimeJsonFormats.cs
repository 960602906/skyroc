namespace Application.Serialization;

internal static class DateTimeJsonFormats
{
    public const string Default = "yyyy-MM-dd HH:mm:ss";

    public static readonly string[] Supported =
    [
        Default,
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
        "O"
    ];
}
