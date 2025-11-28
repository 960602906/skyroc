using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validator.Auth;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenReqDto>
{
    public RefreshTokenValidator()
    {
        RuleFor(r => r.RefreshToken)
            .NotEmpty()
            .WithMessage("token 必填写")
            .Length(50)
            .WithMessage("token 最少为50字符以上");
    }
}