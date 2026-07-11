using Application.DTOs.MenuButton;
using Application.interfaces;
using FluentValidation;

namespace Application.Validator.MenuButton;

public class UpdateMenuButtonValidator : AbstractValidator<UpdateMenuButtonDto>
{
    public UpdateMenuButtonValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("按钮编码不能为空")
            .Length(1, 100).WithMessage("按钮编码长度必须在1-100之间");
    }
}