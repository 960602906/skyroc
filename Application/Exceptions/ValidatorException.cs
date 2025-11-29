using FluentValidation.Results;

namespace Application.Exceptions;

/// <summary>
///     验证异常
/// </summary>
public class ValidationException() : Exception("发生一个或多个验证错误。")
{
    public new readonly string? Message;

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        var failureGroups = new Dictionary<string, List<string>>();
        var errorMessages = new List<string>();
        foreach (var failure in failures)
        {
            if (!failureGroups.TryGetValue(failure.PropertyName, out var value))
            {
                value = [];
                failureGroups[failure.PropertyName] = value;
            }

            value.Add(failure.ErrorMessage);
        }

        Errors = new Dictionary<string, string[]>();
        foreach (var group in failureGroups)
        {
            Errors.Add(group.Key, group.Value.ToArray());
            errorMessages.AddRange(group.Value);
        }

        // ✅ 用逗号拼接所有错误信息
        Message = string.Join(",", errorMessages);
    }

    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();
}