using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class UpdateGoodsValidator : NamedCodeValidator<UpdateGoodsDto>
{
    public UpdateGoodsValidator() : base("商品")
    {
        RuleForId();
        RuleFor(x => x.GoodsTypeId).NotEmpty().WithMessage("商品分类不能为空");
        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0m, 1m)
            .When(x => x.TaxRate.HasValue)
            .WithMessage("商品税率必须在 0 到 1 之间");
    }
}

