using Application.DTOs.AfterSales;
using FluentValidation;

namespace Application.Validator.AfterSales;

/// <summary>
/// 取货任务司机分配请求校验器。
/// </summary>
public class AssignPickupTaskValidator : AbstractValidator<AssignPickupTaskDto>
{
    /// <summary>配置司机主键和调度备注长度约束。</summary>
    public AssignPickupTaskValidator()
    {
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
