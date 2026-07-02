using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class CreateGoodsUnitValidator : AbstractValidator<CreateGoodsUnitDto>
{
    public CreateGoodsUnitValidator()
    {
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.Name).NotEmpty().WithMessage("单位名称不能为空").MaximumLength(50);
        RuleFor(x => x.ConversionRate).GreaterThan(0).WithMessage("换算比例必须大于0");
    }
}

