using Application.DTOs.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 手工新增采购计划请求校验器。
/// </summary>
public class CreatePurchasePlanValidator : AbstractValidator<CreatePurchasePlanDto>
{
    /// <summary>
    /// 配置采购计划主单及明细的校验规则。
    /// </summary>
    public CreatePurchasePlanValidator()
    {
        RuleFor(x => x.PlanDate).NotEmpty().WithMessage("计划交期不能为空");
        RuleFor(x => x.PurchasePattern).IsInEnum().WithMessage("采购模式不合法");
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty().WithMessage("采购计划至少需要一条商品明细");
        RuleForEach(x => x.Details).SetValidator(new CreatePurchasePlanDetailValidator());
    }
}
