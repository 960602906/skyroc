using Application.DTOs.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 从采购计划生成采购单请求校验器。
/// </summary>
public class GeneratePurchaseOrdersFromPlansValidator : AbstractValidator<GeneratePurchaseOrdersFromPlansDto>
{
    /// <summary>
    /// 配置来源计划集合及统一备注约束。
    /// </summary>
    public GeneratePurchaseOrdersFromPlansValidator()
    {
        RuleFor(x => x.PlanIds).NotEmpty();
        RuleForEach(x => x.PlanIds).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
