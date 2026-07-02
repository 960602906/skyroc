using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class CreateQuotationValidator : NamedCodeValidator<CreateQuotationDto>
{
    public CreateQuotationValidator() : base("报价单")
    {
        RuleFor(x => x.EffectiveEnd)
            .GreaterThanOrEqualTo(x => x.EffectiveStart)
            .When(x => x.EffectiveStart.HasValue && x.EffectiveEnd.HasValue)
            .WithMessage("生效结束时间不能早于开始时间");
    }
}

