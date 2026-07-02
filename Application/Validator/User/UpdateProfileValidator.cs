using Application.DTOs.User;
using FluentValidation;

namespace Application.Validator.User;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("性别值不正确");

        RuleFor(x => x.NickName)
            .NotEmpty().WithMessage("昵称不能为空")
            .Length(2, 50).WithMessage("昵称长度必须在2-50之间");

        RuleFor(x => x.Phone)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");
    }
}
