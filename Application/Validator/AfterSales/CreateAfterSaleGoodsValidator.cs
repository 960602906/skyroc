using Application.DTOs.AfterSales;
using FluentValidation;

namespace Application.Validator.AfterSales;

/// <summary>
/// 售后商品行请求校验器。
/// </summary>
public class CreateAfterSaleGoodsValidator : AbstractValidator<CreateAfterSaleGoodsDto>
{
    /// <summary>
    /// 配置来源订单行、手工商品、数量、单价、枚举及备注约束。
    /// </summary>
    public CreateAfterSaleGoodsValidator()
    {
        RuleFor(x => x.SaleOrderDetailId).NotEmpty().When(x => x.SaleOrderDetailId.HasValue);
        RuleFor(x => x.GoodsId).NotEmpty().When(x => x.GoodsId.HasValue);
        RuleFor(x => x.GoodsUnitId).NotEmpty().When(x => x.GoodsUnitId.HasValue);
        RuleFor(x => x.GoodsId).NotNull().When(x => !x.SaleOrderDetailId.HasValue)
            .WithMessage("手工售后商品必须填写商品 ID");
        RuleFor(x => x.GoodsUnitId).NotNull().When(x => !x.SaleOrderDetailId.HasValue)
            .WithMessage("手工售后商品必须填写商品单位 ID");
        RuleFor(x => x.UnitPrice).NotNull().When(x => !x.SaleOrderDetailId.HasValue)
            .WithMessage("手工售后商品必须填写核算单价");
        RuleFor(x => x.ActualRefundQuantity).GreaterThan(0m);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m).When(x => x.UnitPrice.HasValue);
        RuleFor(x => x.AfterSaleType).IsInEnum();
        RuleFor(x => x.ReasonType).IsInEnum();
        RuleFor(x => x.HandleType).IsInEnum();
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
