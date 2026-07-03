using Application.DTOs.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 手工新增采购计划商品明细校验器。
/// </summary>
public class CreatePurchasePlanDetailValidator : AbstractValidator<CreatePurchasePlanDetailDto>
{
    /// <summary>
    /// 配置采购计划商品、单位和数量的校验规则。
    /// </summary>
    public CreatePurchasePlanDetailValidator()
    {
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.PurchaseUnitId).NotEmpty().WithMessage("采购单位不能为空");
        RuleFor(x => x.PlannedQuantity).GreaterThan(0).WithMessage("计划采购数量必须大于零");
        RuleFor(x => x.RequiredQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => x.RequiredQuantity.HasValue)
            .WithMessage("需求数量不能为负数");
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
