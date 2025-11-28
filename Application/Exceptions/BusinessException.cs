namespace Application.Exceptions;

/// <summary>
///     业务异常
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }

    public BusinessException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public BusinessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string? ErrorCode { get; set; }
}