using Application.DTOs.Delivery;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 配送异常登记请求校验器。
/// </summary>
public class CreateDeliveryExceptionValidator : AbstractValidator<CreateDeliveryExceptionDto>
{
    /// <summary>
    /// 配置任务主键和异常描述校验规则。
    /// </summary>
    public CreateDeliveryExceptionValidator()
    {
        RuleFor(x => x.DeliveryTaskId).NotEqual(Guid.Empty).WithMessage("配送任务 ID 不能为空");
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("异常描述不能为空")
            .MaximumLength(1000).WithMessage("异常描述不能超过 1000 个字符");
    }
}
