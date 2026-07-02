using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class UpdatePurchaserValidator : NamedCodeValidator<UpdatePurchaserDto>
{
    public UpdatePurchaserValidator() : base("采购员")
    {
        RuleForId();
    }
}

