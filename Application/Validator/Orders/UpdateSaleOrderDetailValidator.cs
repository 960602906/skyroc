using Application.DTOs.Orders;
using FluentValidation;

namespace Application.Validator;

public class UpdateSaleOrderDetailValidator : AbstractValidator<UpdateSaleOrderDetailDto>
{
    public UpdateSaleOrderDetailValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty)
            .When(x => x.Id.HasValue)
            .WithMessage("明细 ID 不能是空值");
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.GoodsUnitId).NotEmpty().WithMessage("下单单位不能为空");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("下单数量必须大于0");
        RuleFor(x => x.FixedPrice).GreaterThanOrEqualTo(0).WithMessage("固定单价不能小于0");
        RuleFor(x => x.FixedGoodsUnitId).NotEmpty().WithMessage("单价单位不能为空");
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.InnerRemark).MaximumLength(500);
    }
}
