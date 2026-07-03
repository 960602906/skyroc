using Application.DTOs.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 采购单编辑商品行请求校验器。
/// </summary>
public class UpdatePurchaseOrderDetailValidator : AbstractValidator<UpdatePurchaseOrderDetailDto>
{
    /// <summary>
    /// 配置商品、单位、数量、价格及计划占用约束。
    /// </summary>
    public UpdatePurchaseOrderDetailValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty).When(x => x.Id.HasValue);
        RuleFor(x => x.GoodsId).NotEmpty();
        RuleFor(x => x.PurchaseUnitId).NotEmpty();
        RuleFor(x => x.RequiredQuantity).GreaterThan(0m).When(x => x.RequiredQuantity.HasValue);
        RuleFor(x => x.PurchaseQuantity).GreaterThan(0m);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleForEach(x => x.PlanAllocations).SetValidator(new PurchaseOrderPlanAllocationValidator());
    }
}
