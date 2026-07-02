using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
///     带名称和编码的基础资料校验基类。
/// </summary>
public abstract class NamedCodeValidator<T> : AbstractValidator<T>
    where T : INamedCodeInput
{
    protected NamedCodeValidator(string displayName)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage($"{displayName}名称不能为空")
            .MaximumLength(150).WithMessage($"{displayName}名称不能超过150个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage($"{displayName}编码不能为空")
            .MaximumLength(50).WithMessage($"{displayName}编码不能超过50个字符")
            .Matches(@"^[A-Za-z0-9_\-]+$").WithMessage($"{displayName}编码只能包含字母、数字、下划线和中横线");
    }

    protected void RuleForId()
    {
        RuleFor(x => ((IUpdateInput)x).Id)
            .NotEmpty().WithMessage("id 必须填写");
    }
}

