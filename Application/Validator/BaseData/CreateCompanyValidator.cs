using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class CreateCompanyValidator : NamedCodeValidator<CreateCompanyDto>
{
    public CreateCompanyValidator() : base("公司")
    {
    }
}

