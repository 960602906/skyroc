using Application.DTOs.Role;
using FluentValidation;

namespace Application.Validator.Role;

public class UpdateRoleValidator : AbstractValidator<UpdateRoleDto>
{
    public UpdateRoleValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("角色id必填");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("角色名称不能为空")
            .Length(2, 50).WithMessage("角色名称长度必须在2-50之间")
            .Matches(@"^[a-zA-Z0-9_\-\u4e00-\u9fa5]+$").WithMessage("角色名称只能包含字母、数字、下划线、连字符和中文");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("角色编码不能为空")
            .Length(1, 255).WithMessage("角色编码长度必须在1-255之间");
    }
}