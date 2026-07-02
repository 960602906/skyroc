using Application.DTOs.User;
using FluentValidation;

namespace Application.Validator.User;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("旧密码不能为空");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密码不能为空")
            .Length(6, 100).WithMessage("新密码长度必须在6-100个字符之间")
            .NotEqual(x => x.OldPassword).WithMessage("新密码不能与旧密码相同");
    }
}
