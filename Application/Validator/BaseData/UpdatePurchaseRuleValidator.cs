using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class UpdatePurchaseRuleValidator : NamedCodeValidator<UpdatePurchaseRuleDto>
{
    public UpdatePurchaseRuleValidator() : base("采购规则")
    {
        RuleForId();
        RuleFor(x => x.PurchasePattern).InclusiveBetween(1, 2).WithMessage("采购模式只能是 1 或 2");
    }
}

