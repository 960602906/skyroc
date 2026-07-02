using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class CreateCustomerProtocolGoodsValidator : AbstractValidator<CreateCustomerProtocolGoodsDto>
{
    public CreateCustomerProtocolGoodsValidator()
    {
        RuleFor(x => x.CustomerProtocolId).NotEmpty().WithMessage("客户协议价不能为空");
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.GoodsUnitId).NotEmpty().WithMessage("协议价单位不能为空");
        RuleFor(x => x.ProtocolPrice).GreaterThanOrEqualTo(0).WithMessage("协议单价不能小于0");
        RuleFor(x => x.MinOrderQuantity).GreaterThanOrEqualTo(0).When(x => x.MinOrderQuantity.HasValue);
    }
}

