using Application.DTOs.AfterSales;
using FluentValidation;

namespace Application.Validator.AfterSales;

/// <summary>
/// 更新待提交售后单请求校验器。
/// </summary>
public class UpdateAfterSaleValidator : AbstractValidator<UpdateAfterSaleDto>
{
    /// <summary>
    /// 配置主键、联系信息及替换商品行集合约束。
    /// </summary>
    public UpdateAfterSaleValidator(IValidator<CreateAfterSaleGoodsDto> goodsValidator)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ContactName).MaximumLength(100);
        RuleFor(x => x.ContactPhone).MaximumLength(30);
        RuleFor(x => x.PickupAddress).MaximumLength(500);
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Goods).NotNull().NotEmpty();
        RuleForEach(x => x.Goods).SetValidator(goodsValidator);
    }
}
