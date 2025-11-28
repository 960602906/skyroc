namespace Application.Exceptions;

/// <summary>
///     资源未找到异常
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"实体 '{name}' ({key}) 未找到。")
    {
    }
}