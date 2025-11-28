using Application.DTOs.Menu;
using FluentValidation;

namespace Application.Validator.Menu;

public class CreateMenuValidator : AbstractValidator<CreateMenuDto>
{
    public CreateMenuValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("菜单名称不能为空")
            .Length(2, 50).WithMessage("菜单名称长度必须在2-50之间");

        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("路由路径不能为空")
            .Length(1, 255).WithMessage("路由路径长度必须在1-255之间");

        RuleFor(x => x.Component)
            .NotEmpty().WithMessage("组件路径不能为空")
            .Length(1, 255).WithMessage("组件路径长度必须在1-255之间");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("菜单标题不能为空")
            .Length(1, 100).WithMessage("菜单标题长度必须在1-100之间");

        // 它是布尔值
        RuleFor(x => x.Constant)
            .NotNull().WithMessage("是否常驻不能为空");
    }
}