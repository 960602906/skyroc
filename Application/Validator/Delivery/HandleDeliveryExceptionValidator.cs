using Application.DTOs.Delivery;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 配送异常处理请求校验器。
/// </summary>
public class HandleDeliveryExceptionValidator : AbstractValidator<HandleDeliveryExceptionDto>
{
    /// <summary>
    /// 配置异常处理说明必填和长度限制。
    /// </summary>
    public HandleDeliveryExceptionValidator()
    {
        RuleFor(x => x.HandleRemark)
            .NotEmpty().WithMessage("异常处理说明不能为空")
            .MaximumLength(1000).WithMessage("异常处理说明不能超过 1000 个字符");
    }
}
