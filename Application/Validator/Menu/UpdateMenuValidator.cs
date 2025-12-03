using Application.DTOs.Menu;
using FluentValidation;

namespace Application.Validator.Menu;

public class UpdateMenuValidator : AbstractValidator<UpdateMenuDto>
{
    public UpdateMenuValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("菜单id必填");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("菜单名称不能为空")
            .Length(2, 50).WithMessage("菜单名称长度必须在2-50之间");

        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("路由路径不能为空")
            .Length(1, 255).WithMessage("路由路径长度必须在1-255之间");


        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("路由路径不能为空")
            .Length(1, 100).WithMessage("菜单标题长度必须在1-100之间");
    }
}