using Application.DTOs.Delivery;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 配送任务签收请求校验器。
/// </summary>
public class SignDeliveryTaskValidator : AbstractValidator<SignDeliveryTaskDto>
{
    /// <summary>
    /// 配置签收人、交付说明、出库行唯一性和逐行验收规则。
    /// </summary>
    public SignDeliveryTaskValidator()
    {
        RuleFor(x => x.SignerName)
            .NotEmpty().WithMessage("签收人不能为空")
            .MaximumLength(100).WithMessage("签收人不能超过 100 个字符");
        RuleFor(x => x.Remark).MaximumLength(500).WithMessage("签收说明不能超过 500 个字符");
        RuleFor(x => x.Details)
            .NotNull().WithMessage("验收明细不能为空")
            .NotEmpty().WithMessage("验收明细不能为空")
            .Must(details => details is null || details.Select(x => x.StockOutDetailId).Distinct().Count() == details.Count)
            .WithMessage("销售出库明细不能重复");
        RuleForEach(x => x.Details).SetValidator(new SignDeliveryCheckDetailValidator());
    }
}
