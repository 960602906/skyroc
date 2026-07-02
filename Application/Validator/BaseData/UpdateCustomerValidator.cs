using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class UpdateCustomerValidator : NamedCodeValidator<UpdateCustomerDto>
{
    public UpdateCustomerValidator() : base("客户")
    {
        RuleForId();
        RuleFor(x => x.TaxpayerIdentificationNumber).MaximumLength(32);
        RuleFor(x => x.UnifiedSocialCreditCode).MaximumLength(32);
    }
}

