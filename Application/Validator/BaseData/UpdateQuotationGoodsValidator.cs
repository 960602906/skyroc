using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class UpdateQuotationGoodsValidator : AbstractValidator<UpdateQuotationGoodsDto>
{
    public UpdateQuotationGoodsValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("id 必须填写");
        RuleFor(x => x.QuotationId).NotEmpty().WithMessage("报价单不能为空");
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.GoodsUnitId).NotEmpty().WithMessage("报价单位不能为空");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("销售单价不能小于0");
        RuleFor(x => x.MinOrderQuantity).GreaterThanOrEqualTo(0).When(x => x.MinOrderQuantity.HasValue);
    }
}

