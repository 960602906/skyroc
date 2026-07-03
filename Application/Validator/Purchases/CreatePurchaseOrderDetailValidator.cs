using Application.DTOs.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 手工采购单商品行请求校验器。
/// </summary>
public class CreatePurchaseOrderDetailValidator : AbstractValidator<CreatePurchaseOrderDetailDto>
{
    /// <summary>
    /// 配置商品、单位、数量、价格和备注约束。
    /// </summary>
    public CreatePurchaseOrderDetailValidator()
    {
        RuleFor(x => x.GoodsId).NotEmpty();
        RuleFor(x => x.PurchaseUnitId).NotEmpty();
        RuleFor(x => x.RequiredQuantity).GreaterThan(0m).When(x => x.RequiredQuantity.HasValue);
        RuleFor(x => x.PurchaseQuantity).GreaterThan(0m);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
