using Application.DTOs.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 采购单商品行计划占用请求校验器。
/// </summary>
public class PurchaseOrderPlanAllocationValidator : AbstractValidator<PurchaseOrderPlanAllocationDto>
{
    /// <summary>
    /// 配置计划明细主键和占用数量约束。
    /// </summary>
    public PurchaseOrderPlanAllocationValidator()
    {
        RuleFor(x => x.PurchasePlanDetailId).NotEmpty();
        RuleFor(x => x.AllocatedQuantity).GreaterThan(0m);
    }
}
