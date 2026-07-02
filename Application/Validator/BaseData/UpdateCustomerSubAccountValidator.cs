using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

public class UpdateCustomerSubAccountValidator : AbstractValidator<UpdateCustomerSubAccountDto>
{
    public UpdateCustomerSubAccountValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("id 必须填写");
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("所属公司不能为空");
        RuleFor(x => x.Username).NotEmpty().WithMessage("登录账号不能为空").MaximumLength(50);
        RuleFor(x => x.NickName).NotEmpty().WithMessage("昵称不能为空").MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("邮箱格式不正确");
    }
}

