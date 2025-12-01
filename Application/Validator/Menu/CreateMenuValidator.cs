using Application.DTOs.Menu;
using FluentValidation;

namespace Application.Validator.Menu;

public class CreateMenuValidator : AbstractValidator<CreateMenuDto>
{
    public CreateMenuValidator()
    {
        RuleFor(x => x.MenuName)
            .NotEmpty().WithMessage("菜单名称不能为空")
            .Length(2, 50).WithMessage("菜单名称长度必须在2-50之间");

        RuleFor(x => x.RoutePath)
            .NotEmpty().WithMessage("路由路径不能为空")
            .Length(1, 255).WithMessage("路由路径长度必须在1-255之间");

     

        RuleFor(x => x.MenuName)
            .NotEmpty().WithMessage("菜单标题不能为空")
            .Length(1, 100).WithMessage("菜单标题长度必须在1-100之间");

        // 它是布尔值
        RuleFor(x => x.Constant)
            .NotNull().WithMessage("是否常驻不能为空");
    }
}