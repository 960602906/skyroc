using Application.DTOs.User;
using FluentValidation;

namespace Application.Validator.User;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 50).WithMessage("用户名长度必须在3-50之间")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("用户名只能包含字母、数字、下划线和连字符");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码长度不能少于6位");
        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("性别不能为空")
            .IsInEnum().WithMessage("性别值不正确");
        RuleFor(x => x.NickName)
            .NotEmpty().WithMessage("昵称不能为空")
            .Length(2, 50).WithMessage("昵称长度必须在2-50之间");
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");
    }
}