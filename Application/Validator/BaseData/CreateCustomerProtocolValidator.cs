using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class CreateCustomerProtocolValidator : NamedCodeValidator<CreateCustomerProtocolDto>
{
    public CreateCustomerProtocolValidator() : base("客户协议价")
    {
        RuleFor(x => x.EffectiveStart).NotEmpty().WithMessage("协议生效开始时间不能为空");
        RuleFor(x => x.EffectiveEnd)
            .GreaterThanOrEqualTo(x => x.EffectiveStart)
            .When(x => x.EffectiveEnd.HasValue)
            .WithMessage("协议结束时间不能早于开始时间");
    }
}

