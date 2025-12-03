using Application.DTOs.MenuButton;
using FluentValidation;

namespace Application.Validator.MenuButton;

public class CreateMenuButtonValidator:AbstractValidator<CreateMenuButtonDto>
{
    public CreateMenuButtonValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("按钮编码不能为空")
            .Length(1, 100).WithMessage("按钮编码长度必须在1-100之间");
    }
}