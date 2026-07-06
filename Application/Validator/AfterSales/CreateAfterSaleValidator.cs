using Application.DTOs.AfterSales;
using FluentValidation;

namespace Application.Validator.AfterSales;

/// <summary>
/// 创建售后单请求校验器。
/// </summary>
public class CreateAfterSaleValidator : AbstractValidator<CreateAfterSaleDto>
{
    /// <summary>
    /// 配置来源订单或客户、联系信息及商品行集合约束。
    /// </summary>
    public CreateAfterSaleValidator(IValidator<CreateAfterSaleGoodsDto> goodsValidator)
    {
        RuleFor(x => x.SaleOrderId).NotEmpty().When(x => x.SaleOrderId.HasValue);
        RuleFor(x => x.CustomerId).NotEmpty().When(x => x.CustomerId.HasValue);
        RuleFor(x => x.CustomerId).NotNull().When(x => !x.SaleOrderId.HasValue)
            .WithMessage("无来源订单时必须填写客户 ID");
        RuleFor(x => x.Source).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ContactName).MaximumLength(100);
        RuleFor(x => x.ContactPhone).MaximumLength(30);
        RuleFor(x => x.PickupAddress).MaximumLength(500);
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Goods).NotNull().NotEmpty();
        RuleForEach(x => x.Goods).SetValidator(goodsValidator);
    }
}
