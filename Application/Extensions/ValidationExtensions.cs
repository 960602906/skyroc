using Application.Exceptions;
using FluentValidation;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Extensions;

/// <summary>
/// FluentValidation 校验扩展：失败时抛出业务 <see cref="ValidationException"/>。
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// 校验实例；无效时抛出 <see cref="ValidationException"/>（与编排服务原私有助手行为一致）。
    /// 命名为 ValidateOrThrowAsync，避免与 FluentValidation 的 ValidateAsync 实例方法冲突。
    /// </summary>
    public static async Task ValidateOrThrowAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
    }
}
