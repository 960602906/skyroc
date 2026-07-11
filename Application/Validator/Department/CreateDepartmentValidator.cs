using Application.DTOs.Department;
using FluentValidation;

namespace Application.Validator.Department;

public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("部门名称不能为空")
            .MaximumLength(64).WithMessage("名称不能超过64个字符");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("部门代码不能为空")
            .MaximumLength(64).WithMessage("部门代码长度不能超过64个字符")
            .Matches(@"^[A-Za-z0-9_]+$").WithMessage("部门代码只能包含字母、数字和下划线");
        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("联系电话长度不能超过20个字符")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
        RuleFor(x => x.Email)
            .MaximumLength(100).WithMessage("邮箱长度不能超过100个字符");

        RuleFor(x => x.Remark)
            .MaximumLength(500).WithMessage("备注长度不能超过500个字符");
    }
}