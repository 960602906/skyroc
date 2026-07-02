using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class CreateGoodsTypeValidator : NamedCodeValidator<CreateGoodsTypeDto>
{
    public CreateGoodsTypeValidator() : base("商品分类")
    {
        RuleFor(x => x.DefaultTaxRate)
            .InclusiveBetween(0m, 1m)
            .When(x => x.DefaultTaxRate.HasValue)
            .WithMessage("默认税率必须在 0 到 1 之间");
    }
}

