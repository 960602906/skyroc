using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validator.Auth;

public class LoginValidator : AbstractValidator<LoginReqDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("用户名必填")
            .Length(3, 50)
            .WithMessage("用户名长度必须在3-50个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$")
            .WithMessage("用户名只能包含字母、数字和下划线");
        ;
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("密码必填")
            .Length(6, 100)
            .WithMessage("密码长度必须在6-100个字符之间");
        ;
    }
}