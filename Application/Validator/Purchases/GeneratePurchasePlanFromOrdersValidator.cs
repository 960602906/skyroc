using Application.DTOs.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 从已审核销售订单生成采购计划的请求校验器。
/// </summary>
public class GeneratePurchasePlanFromOrdersValidator : AbstractValidator<GeneratePurchasePlanFromOrdersDto>
{
    /// <summary>
    /// 配置来源订单集合和备注的校验规则。
    /// </summary>
    public GeneratePurchasePlanFromOrdersValidator()
    {
        RuleFor(x => x.OrderIds).NotEmpty().WithMessage("请至少选择一个销售订单");
        RuleForEach(x => x.OrderIds).NotEmpty().WithMessage("销售订单主键不能为空");
        RuleFor(x => x.OrderIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .When(x => x.OrderIds.Count > 0)
            .WithMessage("销售订单不能重复");
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
